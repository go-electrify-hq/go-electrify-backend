using System.Collections.Concurrent;
using System.Net.Http.Headers;
using GoElectrify.DockConsole.Contracts;
using GoElectrify.DockConsole.Internal;
using Microsoft.Extensions.Options;

namespace GoElectrify.DockConsole.Infrastructure
{
    public sealed class SimulationManager
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IOptions<DockConsoleOptions> _opt;
        private readonly ConcurrentDictionary<int, CancellationTokenSource> _runs = new(); // sessionId -> CTS

        public SimulationManager(IHttpClientFactory httpFactory, IOptions<DockConsoleOptions> opt)
        {
            _httpFactory = httpFactory;
            _opt = opt;
        }

        public bool IsRunning(int sessionId) => _runs.ContainsKey(sessionId);
        public void Stop(int sessionId) { if (_runs.TryRemove(sessionId, out var cts)) cts.Cancel(); }

        /// <summary>
        /// Overload MỚI: dùng toàn bộ số liệu từ /charging-sessions/start, KHÔNG gọi thêm GET nào.
        /// </summary>
        public Task StartAsync(
            int sessionId, int dockId, string driverJwt, int initialSoc,
            double vehicleBatteryKwh, int vehicleMaxPowerKw,
            double chargerPowerKw, int connectorMaxPowerKw, int targetSoc,
            CancellationToken ct)
        {
            Stop(sessionId); // hủy nếu đang chạy phiên cũ
            var hasSecret = _opt.Value.Docks.TryGetValue(dockId.ToString(), out var dockSecret1) && !string.IsNullOrWhiteSpace(dockSecret1);
            Console.WriteLine($"[SIM] start? session={sessionId} dock={dockId} secret={(hasSecret ? "OK" : "MISSING")}");

            if (!hasSecret) return Task.CompletedTask; // giữ logic cũ

            if (!_opt.Value.Docks.TryGetValue(dockId.ToString(), out var dockSecret) || string.IsNullOrWhiteSpace(dockSecret))
                return Task.CompletedTask; // không có secret -> không giả lập

            // clamp inputs
            var batteryKwh = Math.Max(0.1, vehicleBatteryKwh);
            var capByVehicle = vehicleMaxPowerKw > 0 ? vehicleMaxPowerKw : double.PositiveInfinity;
            var capByConnector = connectorMaxPowerKw > 0 ? connectorMaxPowerKw : double.PositiveInfinity;

            // công suất trần thực tế
            var capKw = Math.Min(chargerPowerKw, Math.Min(capByVehicle, capByConnector));
            if (double.IsInfinity(capKw) || capKw <= 0) capKw = chargerPowerKw; // fallback

            // heuristic DC nếu cap lớn (vì ConnectorType của bạn không có IsDc)
            var isDcHeuristic = capKw >= 40.0;

            // vòng giả lập
            var http = _httpFactory.CreateClient("api");
            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (!_runs.TryAdd(sessionId, cts)) { cts.Cancel(); return Task.CompletedTask; }

            _ = Task.Run(async () =>
            {
                try
                {
                    await SimLoopAsync(sessionId, dockId, dockSecret, driverJwt, initialSoc,
                        batteryKwh, capKw, isDcHeuristic, targetSoc, http, cts.Token);
                }
                catch { /* ignore */ }
                finally { _runs.TryRemove(sessionId, out _); }
            }, cts.Token);

            return Task.CompletedTask;
        }
        private static (double V, double A) ComputeVI(double pKw, double soc, double capKw, bool isDc)
        {
            if (pKw <= 0.1) return (0, 0);

            if (isDc)
            {
                // chọn class theo cap
                var hvClass = capKw >= 150.0;

                if (hvClass)
                {
                    // 800V-class: khoảng 550–800V, tăng theo SOC
                    var v = 550.0 + (soc / 100.0) * 250.0; // 550 → 800 V
                    var i = (pKw * 1000.0) / v;
                    return (v, i);
                }
                else
                {
                    // 400V-class: khoảng 350–420V
                    var v = 350.0 + (soc / 100.0) * 70.0; // 350 → 420 V
                    var i = (pKw * 1000.0) / v;
                    return (v, i);
                }
            }
            else
            {
                // AC
                if (capKw <= 7.4)
                {
                    // 1-phase 230V
                    const double v = 230.0;
                    const double pf = 0.98;
                    var i = (pKw * 1000.0) / (v * pf);
                    return (v, i);
                }
                else
                {
                    // 3-phase 400V
                    const double vll = 400.0;     // line-line
                    const double pf = 0.95;
                    var i = (pKw * 1000.0) / (Math.Sqrt(3.0) * vll * pf);
                    return (vll, i);
                }
            }
        }


        private static async Task SimLoopAsync(
            int sessionId, int dockId, string dockSecret, string driverJwt, int initialSoc,
            double batteryKwh, double capKw, bool isDc, int targetSoc,
            HttpClient http, CancellationToken ct)
        {

            // ==== trạng thái mô phỏng ====
            double soc = Math.Clamp(initialSoc, 0, 100);

            // TÍCH PHÂN Ở ĐỘ PHÂN GIẢI WH (tránh trôi số)
            double energyWh = 0.0;

            // mốc thời gian thực để tính dt
            var lastAt = DateTime.UtcNow;

            // noise nho nhỏ cho realistic
            var rng = new Random();

            // Nhịp gửi (vẫn nhịp đều), nhưng dt tính theo thực tế
            const int hz = 2;
            var period = TimeSpan.FromMilliseconds(1000.0 / hz);

            // Gợi ý: dùng usable factor nếu BatteryCapacityKwh là gross
            const double usableFactor = 1.0; // tạm =1.0; nếu muốn: 0.94

            var usableBatteryKwh = Math.Max(0.1, batteryKwh * usableFactor);

            while (!ct.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var dtSec = (now - lastAt).TotalSeconds;
                lastAt = now;

                // chặn outlier
                if (dtSec <= 0 || dtSec > 2.0)
                {
                    await Task.Delay(period, ct);
                    continue;
                }

                // 1) Công suất đặt theo SOC + noise
                var baseP = isDc ? DcTaper(capKw, soc) : AcProfile(capKw, soc);
                var noise = baseP * (rng.NextDouble() * 0.06 - 0.03); // ±3%
                var pSet = Math.Max(0.5, baseP + noise);              // kW

                // 2) Năng lượng thực nạp vào (Wh) & SOC
                var deltaWh = pSet * dtSec * (1000.0 / 3600.0); // kW * s -> Wh
                energyWh += deltaWh;

                var deltaKwh = deltaWh / 1000.0;
                soc = Math.Min(100.0, soc + (deltaKwh / usableBatteryKwh) * 100.0);

                var (voltageV, currentA) = ComputeVI(pSet, soc, capKw, isDc);

                // 4) Gửi tick – chỉ làm tròn khi GỬI
                var body = new
                {
                    DockId = dockId,
                    SecretKey = dockSecret,
                    SampleAt = now.ToString("o"),
                    PowerKw = Math.Round(pSet, 2),
                    SessionEnergyKwh = Math.Round(energyWh / 1000.0, 4),
                    SocPercent = (int)Math.Round(soc),
                    State = "CHARGING",
                    Voltage = (decimal?)Math.Round((decimal)voltageV, 1), // có giá trị!
                    Current = (decimal?)Math.Round((decimal)currentA, 1), // có giá trị!
                    ErrorCode = (string?)null
                };

                using var resp = await http.PostAsJsonAsync("/api/v1/docks/log", body, JsonCfg.Pascal, ct);
                if (!resp.IsSuccessStatusCode) break;

                if (soc >= targetSoc)
                {
                    try
                    {
                        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/charging-sessions/{sessionId}/stop")
                        { Content = JsonContent.Create(new { Reason = "SIM_TARGET_SOC" }, options: JsonCfg.Pascal) };
                        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", driverJwt);
                        await http.SendAsync(msg, ct);
                    }
                    catch { /* ignore */ }
                    break;
                }

                await Task.Delay(period, ct);
            }
        }

        // AC: phẳng tới ~90%, sau đó taper nhẹ
        private static double AcProfile(double capKw, double soc)
        {
            if (soc < 90) return capKw;
            if (soc < 99) return Math.Max(2.0, capKw * (1 - (soc - 90) / 12.0));
            return 1.0;
        }

        // DC: ramp-up -> plateau -> taper -> kết thúc
        private static double DcTaper(double capKw, double soc)
        {
            if (soc < 10) return Math.Max(0.2 * capKw, capKw * (0.5 + soc / 20.0));
            if (soc < 60) return capKw;
            if (soc < 80) return Math.Max(0.4 * capKw, capKw * (1.0 - (soc - 60) / 50.0));
            if (soc < 95) return Math.Max(20.0, capKw * (0.4 - (soc - 80) / 80.0));
            return 8.0;
        }
    }
}
