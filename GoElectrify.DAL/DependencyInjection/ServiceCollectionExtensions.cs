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

            // Infra services
            services.AddSingleton<IRedisCache, RedisCache>();
            services.AddScoped<IEmailSender, ConsoleEmailSender>();

            // JWT
            services.Configure<JwtOptions>(cfg.GetSection("Jwt"));
            services.AddScoped<ITokenService, JwtTokenService>();

            return services;
        }
    }
}
