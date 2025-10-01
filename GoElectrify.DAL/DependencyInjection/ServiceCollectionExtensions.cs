using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Services;
using GoElectrify.DAL.Infra;
using GoElectrify.DAL.Persistence;
using GoElectrify.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using Resend;


namespace GoElectrify.DAL.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDal(this IServiceCollection services, IConfiguration cfg)
        {
            // SQL Server
            services.AddDbContext<AppDbContext>(opt =>
                opt.UseSqlServer(cfg.GetConnectionString("SqlServer")));

            // Redis (Upstash cũng dùng ConnectionMultiplexer.Connect với rediss://)
            var redisUrl = cfg.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisUrl))
            {
                var uri = new Uri(redisUrl);

                var opts = new ConfigurationOptions
                {
                    AbortOnConnectFail = false,
                    Ssl = uri.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase),
                    SslHost = uri.Host,                    // quan trọng cho SNI/TLS
                    ConnectRetry = 3,
                    ConnectTimeout = 10000,                // 10s
                    KeepAlive = 60,
                    ReconnectRetryPolicy = new ExponentialRetry(5000)
                };

                opts.EndPoints.Add(uri.Host, uri.Port > 0 ? uri.Port : 6379);

                // user/password từ URL
                if (!string.IsNullOrEmpty(uri.UserInfo))
                {
                    var parts = uri.UserInfo.Split(':', 2);
                    if (parts.Length == 2) { opts.User = parts[0]; opts.Password = parts[1]; }
                    else { opts.Password = parts[0]; } // nhiều URL không có user => password-only
                }

                services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(opts));
            }
            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IWalletRepository, WalletRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IExternalLoginRepository, ExternalLoginRepository>();
            services.AddScoped<ITopupIntentRepository, TopupIntentRepository>();
            services.AddScoped<IVehicleModelRepository, VehicleModelRepository>();
            services.AddScoped<IVehicleModelService, VehicleModelService>();
            services.AddScoped<IConnectorTypeRepository, ConnectorTypeRepository>();
            services.AddScoped<IStationRepository, StationRepository>();
            services.AddScoped<IStationStaffRepository, StationStaffRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();



            services.AddScoped<IStationRepository, StationRepository>();
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<IChargerRepository, ChargerRepository>();


            // Infra services
            services.AddSingleton<IRedisCache, RedisCache>();


            // ======================
            // Email Sender — Resend ONLY (SDK chính thức)
            // ======================
            services.AddHttpClient<ResendClient>();
            // Lấy "Resend:ApiToken" từ appsettings + user-secrets (dev) / ENV (prod)
            services.Configure<ResendClientOptions>(cfg.GetSection("Resend"));
            services.PostConfigure<ResendClientOptions>(o =>
            {
                if (string.IsNullOrWhiteSpace(o.ApiToken))
                    throw new InvalidOperationException("Missing Resend:ApiToken. Set via user-secrets (dev) or ENV/secret store (prod).");
            });
            services.AddTransient<IResend, ResendClient>();

            // From (sender) cấu hình nhẹ
            services.Configure<EmailSenderOptions>(cfg.GetSection("Email"));
            services.PostConfigure<EmailSenderOptions>(o =>
            {
                if (string.IsNullOrWhiteSpace(o.From))
                    throw new InvalidOperationException("Missing Email:From (verified sender in Resend).");
            });

            // IEmailSender -> ResendEmailSender (duy nhất)
            services.AddTransient<IEmailSender, ResendEmailSender>();

            return services;
        }
    }
}
