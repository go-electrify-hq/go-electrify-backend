using System.Net.Http.Headers;
using System.Text.Json;
using GoElectrify.DockConsole.Contracts;
using GoElectrify.DockConsole.Infrastructure;
using GoElectrify.DockConsole.Internal;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Options
builder.Services.Configure<DockConsoleOptions>(builder.Configuration.GetSection("DockConsole"));

// HTTP client tới API gốc
builder.Services.AddHttpClient("api", (sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<DockConsoleOptions>>().Value;
    http.BaseAddress = new Uri(opt.ApiBase.TrimEnd('/'));
    http.Timeout = TimeSpan.FromSeconds(15);
    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// Services
builder.Services.AddSingleton<SimulationManager>();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

var serveUi = builder.Configuration.GetValue<bool>("DockConsole:ServeUi", true);

if (serveUi)
{
    app.UseDefaultFiles(); // tìm wwwroot/index.html
    app.UseStaticFiles();
}

// luôn có health & API proxy
app.MapGet("/healthz", () => "ok");

// =========== Minimal proxy endpoints ===========

// 1) Config cho FE
app.MapGet("/api/config", (IOptions<DockConsoleOptions> opt) =>
{
    var docks = opt.Value.Docks.Keys
        .Select(k => int.TryParse(k, out var id) ? id : (int?)null)
        .Where(id => id.HasValue)
        .Select(id => new { DockId = id!.Value, ChannelId = $"ge:dock:{id!.Value}" });

    return Results.Ok(new { apiBase = opt.Value.ApiBase, docks });
});

// 2) Ably token/channel (proxy)
app.MapPost("/api/dock/connect/{dockId:int}", async (
    int dockId,
    IHttpClientFactory httpFactory,
    IOptions<DockConsoleOptions> opt,
    CancellationToken ct) =>
{
    if (!opt.Value.Docks.TryGetValue(dockId.ToString(), out var secret) || string.IsNullOrWhiteSpace(secret))
        return Results.BadRequest(new { error = "DockId not configured or secret missing." });

    var http = httpFactory.CreateClient("api");
    var payload = new { DockId = dockId, SecretKey = secret };

    using var resp = await http.PostAsJsonAsync("/api/v1/docks/connect", payload, JsonCfg.Pascal, ct);
    var txt = await resp.Content.ReadAsStringAsync(ct);
    var contentType = resp.Content.Headers.ContentType?.MediaType ?? "application/json";
    return Results.Text(txt, contentType, statusCode: (int)resp.StatusCode);

});

// 3) Gửi tick log (proxy)
app.MapPost("/api/dock/log/{dockId:int}", async (
    int dockId,
    HttpRequest request,
    IHttpClientFactory httpFactory,
    IOptions<DockConsoleOptions> opt,
    CancellationToken ct) =>
{
    if (!opt.Value.Docks.TryGetValue(dockId.ToString(), out var secret) || string.IsNullOrWhiteSpace(secret))
        return Results.BadRequest(new { error = "DockId not configured or secret missing." });

    var body = await request.ReadFromJsonAsync<PartialLog>(cancellationToken: ct);
    if (body is null) return Results.BadRequest(new { error = "Invalid JSON" });

    var apiBody = new
    {
        DockId = dockId,
        SecretKey = secret,
        SampleAt = body.SampleAt ?? DateTime.UtcNow.ToString("o"),
        Voltage = body.Voltage,
        Current = body.Current,
        PowerKw = body.PowerKw,
        SessionEnergyKwh = body.SessionEnergyKwh,
        SocPercent = body.SocPercent,
        State = body.State ?? "CHARGING",
        ErrorCode = body.ErrorCode
    };

    var http = httpFactory.CreateClient("api");
    using var resp = await http.PostAsJsonAsync("/api/v1/docks/log", apiBody, JsonCfg.Pascal, ct);
    var txt = await resp.Content.ReadAsStringAsync(ct);
    var contentType = resp.Content.Headers.ContentType?.MediaType ?? "application/json";
    return Results.Text(txt, contentType, statusCode: (int)resp.StatusCode);

});

// 4) Driver START (proxy) + kick-off simulator
app.MapPost("/api/driver/start", async (
    HttpRequest req,
    IHttpClientFactory httpFactory,
    SimulationManager sim,
    CancellationToken ct) =>
{
    var body = await req.ReadFromJsonAsync<StartSessionInput>(cancellationToken: ct);
    if (body is null) return Results.BadRequest(new { error = "Invalid JSON" });

    var token = HttpHelpers.ExtractBearer(req);
    if (string.IsNullOrWhiteSpace(token)) return Results.BadRequest(new { error = "Missing access token" });

    var http = httpFactory.CreateClient("api");

    var apiBody = new
    {
        BookingId = body.BookingId,
        BookingCode = body.BookingCode,
        ChargerId = body.ChargerId,
        VehicleModelId = body.VehicleModelId,
        InitialSoc = body.InitialSoc
    };

    using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/charging-sessions/start")
    { Content = JsonContent.Create(apiBody, options: JsonCfg.Pascal) };
    msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    using var resp = await http.SendAsync(msg, ct);
    var txt = await resp.Content.ReadAsStringAsync(ct);
    var contentType = resp.Content.Headers.ContentType?.MediaType ?? "application/json";
    if (!resp.IsSuccessStatusCode) return Results.Text(txt, contentType, statusCode: (int)resp.StatusCode);

    using var doc = JsonDocument.Parse(txt);
    var root = doc.RootElement.TryGetProperty("data", out var d) ? d : doc.RootElement;

    // ID bắt buộc
    int sessionId = HttpHelpers.ReadInt(root, "Id") ?? HttpHelpers.ReadInt(root, "id") ?? 0;
    int chargerId = HttpHelpers.ReadInt(root, "ChargerId") ?? HttpHelpers.ReadInt(root, "chargerId") ?? (body.ChargerId ?? 0);
    int vmId = body.VehicleModelId ?? HttpHelpers.ReadInt(root, "VehicleModelId") ?? 0;

    // SOC ban đầu
    int initialSoc = HttpHelpers.ReadInt(root, "SocStart") ?? HttpHelpers.ReadInt(root, "socStart") ?? body.InitialSoc;

    // ====== SỐ LIỆU MÔ PHỎNG LẤY TỪ DTO TRẢ VỀ ======
    // Vehicle model
    var vehicleBatteryKwh = (double)(HttpHelpers.ReadDecimal(root, "VehicleBatteryCapacityKwh") ?? 60m);
    var vehicleMaxPowerKw = HttpHelpers.ReadInt(root, "VehicleMaxPowerKw") ?? 0;

    // Charger & Connector
    var chargerPowerKw = (double)(HttpHelpers.ReadDecimal(root, "ChargerPowerKw") ?? 7.2m);
    var connectorMaxPowerKw = HttpHelpers.ReadInt(root, "ConnectorMaxPowerKw") ?? 0;

    // Target SOC (nếu không có -> mặc định 100)
    var targetSoc = HttpHelpers.ReadInt(root, "TargetSoc") ?? 100;
    Console.WriteLine($"[DriverStart] session={sessionId} charger={chargerId} initSOC={initialSoc}% " +
                  $"batt={vehicleBatteryKwh}kWh vehMax={vehicleMaxPowerKw}kW connMax={connectorMaxPowerKw}kW charger={chargerPowerKw}kW target={targetSoc}%");

    // Kick-off simulator: KHÔNG gọi thêm GET nào
    _ = sim.StartAsync(
        sessionId: sessionId,
        dockId: chargerId,
        driverJwt: token!,
        initialSoc: initialSoc,
        vehicleBatteryKwh: vehicleBatteryKwh,
        vehicleMaxPowerKw: vehicleMaxPowerKw,
        chargerPowerKw: chargerPowerKw,
        connectorMaxPowerKw: connectorMaxPowerKw,
        targetSoc: targetSoc,
        ct: ct);

    return Results.Text(txt, contentType);
});

// 5) Driver STOP (proxy) + stop simulator
app.MapPost("/api/driver/stop/{sessionId:int}", async (
    int sessionId,
    HttpRequest req,
    IHttpClientFactory httpFactory,
    SimulationManager sim,
    CancellationToken ct) =>
{
    var token = HttpHelpers.ExtractBearer(req);
    if (string.IsNullOrWhiteSpace(token)) return Results.BadRequest(new { error = "Missing access token" });

    sim.Stop(sessionId);

    var http = httpFactory.CreateClient("api");
    using var msg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/charging-sessions/{sessionId}/stop")
    { Content = JsonContent.Create(new { Reason = "DRIVER" }, options: JsonCfg.Pascal) };
    msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    using var resp = await http.SendAsync(msg, ct);
    var txt = await resp.Content.ReadAsStringAsync(ct);
    var contentType = resp.Content.Headers.ContentType?.MediaType ?? "application/json";
    return Results.Text(txt, contentType, statusCode: (int)resp.StatusCode);

});

app.Run();
