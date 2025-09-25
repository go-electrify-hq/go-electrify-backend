using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.DAL.Infra;
using GoElectrify.DAL.Persistence;
using GoElectrify.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


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
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(cfg.GetConnectionString("Redis")!));

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IWalletRepository, WalletRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

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
