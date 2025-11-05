using System.Text;
using GoElectrify.Api.Realtime;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Services;
using GoElectrify.BLL.Services.Interfaces;
using GoElectrify.DAL.DependencyInjection;
using GoElectrify.DAL.Infra;
using GoElectrify.DAL.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using GoElectrify.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using GoElectrify.BLL.Services.Realtime;
using GoElectrify.DAL.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("DashboardDev", p =>
        p
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
});


builder.Logging.ClearProviders();
// Serilog + Swagger
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "GoElectrify API", Version = "v1" });

    // Khai báo Bearer cho Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập theo định dạng: Bearer {accessToken}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// DAL (DbContext, Redis, Email, Repos)
builder.Services.AddDal(builder.Configuration);

// BLL services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IVehicleModelService, VehicleModelService>();
builder.Services.AddScoped<IConnectorTypeService, ConnectorTypeService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStationStaffService, StationStaffService>();
builder.Services.AddScoped<IStationService, StationService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IChargerService, ChargerService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IAdminIncidentService, AdminIncidentService>();
builder.Services.AddSingleton<IAblyService, AblyService>();
builder.Services.AddScoped<IChargingSessionService, ChargingSessionService>();
builder.Services.AddScoped<ITopupIntentService, TopupIntentService>();
builder.Services.AddHttpClient<IPayOSService, PayOSService>();
builder.Services.AddHostedService<GoElectrify.Api.Hosted.SessionWatchdog>();
builder.Services.AddHostedService<GoElectrify.Api.Hosted.DockHeartbeatWatchdog>();
builder.Services.AddScoped<IWalletAdminService, WalletAdminService>();
builder.Services.AddScoped<IWalletSubscriptionService, WalletSubscriptionService>();
builder.Services.AddScoped<IChargingPaymentService, ChargingPaymentService>();
builder.Services.AddScoped<IInsightsService, InsightsService>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<IAblyTokenCache, AblyTokenCache>();
builder.Services.AddScoped<IRealtimeTokenIssuer, RealtimeTokenIssuer>();
builder.Services.AddScoped<IBookingFeeService, BookingFeeService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<INotificationMailService, NotificationMailService>();
builder.Services.AddScoped<IChargerLogService, ChargerLogService>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IAuthorizationHandler, NoUnpaidSessionsHandler>();

// JWT auth
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
        o.TokenValidationParameters = new()
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        // Log lý do 401 để debug nhanh
        o.IncludeErrorDetails = true; // thêm dòng này
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var auth = ctx.Request.Headers["Authorization"].ToString();
                Console.WriteLine($"[DockJwt] Authorization present={(!string.IsNullOrEmpty(auth))}");
                if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var tok = auth.Substring("Bearer ".Length).Trim();
                    ctx.Token = tok; // đảm bảo handler dùng đúng token từ header
                    try
                    {
                        var jwtTok = new JwtSecurityTokenHandler().ReadJwtToken(tok);
                        Console.WriteLine($"[DockJwt] hdr ok, iss={jwtTok.Issuer}, aud={string.Join(",", jwtTok.Audiences)}");
                    }
                    catch { }
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"[DockJwt] auth failed: {ctx.Exception.GetType().Name} - {ctx.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                // sẽ in rõ: invalid_token / signature invalid / audience invalid / expired ...
                Console.WriteLine($"[DockJwt] challenge: {ctx.Error} - {ctx.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    })
    .AddJwtBearer("DockJwt", o =>
    {
        var issuer = builder.Configuration["DockAuth:Issuer"];
        var audience = builder.Configuration["DockAuth:Audience"];
        var key = builder.Configuration["DockAuth:SigningKey"];

        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var tok = ctx.Token;
                if (!string.IsNullOrEmpty(tok))
                {
                    try
                    {
                        var h = new JwtSecurityTokenHandler();
                        var jwt = h.ReadJwtToken(tok); // KHÔNG validate
                        Console.WriteLine($"[DockJwt] Received token {tok[..10]}...{tok[^10..]}");
                        Console.WriteLine($"[DockJwt] Token iss={jwt.Issuer} aud={string.Join(",", jwt.Audiences)}");
                    }
                    catch { /* ignore */ }
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"Dock JWT failed: {ctx.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                var sid = ctx.Principal?.FindFirst("sessionId")?.Value;
                var did = ctx.Principal?.FindFirst("dockId")?.Value;
                var role = ctx.Principal?.FindFirst("role")?.Value;
                Console.WriteLine($"[DockJwt] OK role={role} sessionId={sid} dockId={did}");
                return Task.CompletedTask;
            }
        };

    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DockSessionWrite", p =>
        p.RequireAssertion(ctx =>
            ctx.User.HasClaim("role", "Dock") || ctx.User.IsInRole("Dock")));

    options.AddPolicy("DockOrStaffOrAdmin", p =>
        p.RequireAssertion(ctx =>
            ctx.User.HasClaim("role", "Dock") || ctx.User.IsInRole("Dock")
         || ctx.User.IsInRole("Staff") || ctx.User.IsInRole("Admin")));
    options.AddPolicy("NoUnpaidSessions", p => p.Requirements.Add(new NoUnpaidSessionsRequirement()));
});

// Đăng ký token service (phát hành access/refresh)
builder.Services.AddScoped<ITokenService, TokenService>();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = null;
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter(allowIntegerValues: false));
    });

var app = builder.Build();

// Auto-migrate (dev) + seed Roles
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "GoElectrify API v1");
    o.RoutePrefix = "swagger";
});

app.MapGet("/", () => Results.Redirect("/swagger", permanent: false));
app.UseRouting();
app.UseCors("DashboardDev");
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();
app.MapGet("/healthz", () => "ok");

app.Run();
