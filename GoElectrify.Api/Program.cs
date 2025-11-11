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
using GoElectrify.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using GoElectrify.BLL.Services.Realtime;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("FrontEndProd", p =>
    {
        p.WithOrigins(cfg.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials(); // cần cho cookie cross-site
    });
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
builder.Services.AddScoped<IRefundService, RefundService>();
// JWT auth
var jwtOpts = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
             ?? throw new InvalidOperationException("Missing Jwt options");
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddCookie("External", o =>
    {
        o.Cookie.Name = "ge_ext";
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        o.Cookie.SameSite = SameSiteMode.None;   // cross-site
        o.Cookie.HttpOnly = true;
        o.Cookie.Path = "/";
        o.Cookie.Domain = cfg["Auth:CookieDomain"]; // .go-electrify.com
        o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        o.SlidingExpiration = false;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
    {
        o.TokenValidationParameters = new()
        {
            ValidIssuer = jwtOpts.Issuer,
            ValidAudience = jwtOpts.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.Secret)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        o.IncludeErrorDetails = true;
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
    })
    .AddGoogle("Google", o =>
    {
        o.SignInScheme = "External";
        o.ClientId = cfg["Authentication:Google:ClientId"]!;
        o.ClientSecret = cfg["Authentication:Google:ClientSecret"]!;
        o.CallbackPath = cfg["Authentication:Google:CallbackPath"] ?? "/signin-google";
        o.Scope.Add("openid");
        o.Scope.Add("email");
        o.Scope.Add("profile");
        o.SaveTokens = false;
        o.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
        {
            OnRemoteFailure = ctx =>
            {
                ctx.HttpContext.RequestServices
                   .GetRequiredService<ILoggerFactory>()
                   .CreateLogger("GoogleOAuth")
                   .LogError(ctx.Failure, "Google OAuth remote failure");

                ctx.HandleResponse();
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
app.UseHttpsRedirection();
app.UseCors("FrontEndProd");
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
});
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();
app.MapGet("/healthz", () => "ok");

app.Run();
