using System.Text;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Infrastructure.Auth;
using LinguaSpace.Infrastructure.Cache;
using LinguaSpace.Infrastructure.Data;
using LinguaSpace.Infrastructure.Data.Interceptors;
using LinguaSpace.Infrastructure.Email;
using LinguaSpace.Infrastructure.Identity;
using LinguaSpace.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        string? connectionString = builder.Configuration.GetConnectionString(Services.Database);
        Guard.Against.Null(connectionString, message: $"Connection string '{Services.Database}' not found.");

        // ─── EF Core ─────────────────────────────────────────────────────────
        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        builder.EnrichNpgsqlDbContext<ApplicationDbContext>();

        builder.Services.AddScoped<IApplicationDbContext>(
            provider => provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        // ─── Identity (without API endpoints — we use custom JWT) ────────────
        // AddIdentityCore registers UserManager, RoleManager, etc.
        // We intentionally do NOT call .AddApiEndpoints() — that registers
        // the Identity Bearer token endpoints (/login, /register) which conflict
        // with our custom JWT endpoints.
        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                // Align with RegisterCommandValidator: 8+ chars, no other constraints enforced by Identity.
                // FluentValidation is the single source of truth for password rules and gives clearer errors.
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // ─── JWT Authentication ───────────────────────────────────────────────
        // We read key/issuer/audience from appsettings.json "Jwt" section.
        // TokenValidationParameters must EXACTLY match JwtTokenService config.
        string jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key 'Jwt:Key' is not configured.");
        string jwtIssuer = builder.Configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT issuer 'Jwt:Issuer' is not configured.");
        string jwtAudience = builder.Configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT audience 'Jwt:Audience' is not configured.");

        builder.Services
            .AddAuthentication(options =>
            {
                // Set JWT as the default scheme for both authentication and challenge.
                // Without this, ASP.NET might fall back to cookie auth.
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    // ClockSkew = TimeSpan.Zero means tokens expire exactly at their exp claim.
                    // Default is 5 minutes — this can cause confusing "token still valid" bugs.
                    ClockSkew = TimeSpan.Zero,
                };

                // SignalR sends JWT in query string because WebSocket protocol
                // doesn't support Authorization headers.
                // This block reads the token from ?access_token= for Hub connections.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        string? accessToken = context.Request.Query["access_token"];
                        PathString path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorizationBuilder();

        // ─── Redis ────────────────────────────────────────────────────────────
        // When running via Aspire AppHost, the connection string is injected from the Redis container.
        // When running standalone, falls back to "localhost:6379,abortConnect=false" from appsettings.json.
        // abortConnect=false means startup won't throw even if Redis is unreachable at launch.
        string redisConnectionString =
            builder.Configuration.GetConnectionString(Services.Cache)
            ?? "localhost:6379,abortConnect=false";

        // IConnectionMultiplexer is thread-safe and designed to be reused (Singleton).
        // ConnectionMultiplexer.Connect is the StackExchange.Redis connection factory.
        builder.Services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(redisConnectionString));

        // ─── SignalR + Redis Backplane ────────────────────────────────────────
        // Without a backplane, SignalR can only broadcast to clients connected to
        // the SAME server instance. Redis pub/sub syncs messages across all instances.
        // For local dev with 1 instance, backplane is a no-op (but no harm).
        builder.Services
            .AddSignalR()
            .AddStackExchangeRedis(redisConnectionString);

        // ─── Application Services ─────────────────────────────────────────────
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();

        // Singleton because JwtTokenService holds SymmetricSecurityKey (stateless, thread-safe).
        builder.Services.AddSingleton<ITokenService, JwtTokenService>();

        // Singleton because IConnectionMultiplexer is Singleton (StackExchange.Redis best practice).
        builder.Services.AddSingleton<ICacheService, RedisCacheService>();

        // Transient: email and storage services are stateless, per-request is fine.
        builder.Services.AddTransient<IEmailService, ConsoleEmailService>();
        builder.Services.AddTransient<IStorageService, LocalStorageService>();

        // ─── LiveKit SFU ──────────────────────────────────────────────────────
        builder.Services.AddTransient<ISfuService, LinguaSpace.Infrastructure.Media.LiveKitService>();

        // ─── SignalR Notification + Presence ─────────────────────────────────
        builder.Services.AddTransient<INotificationService, LinguaSpace.Infrastructure.Notifications.SignalRNotificationService>();

        // ─── Firebase Cloud Messaging (FCM) ───────────────────────────────────
        builder.Services.AddTransient<IPushNotificationService, LinguaSpace.Infrastructure.PushNotifications.FcmPushNotificationService>();

        // ─── Google OAuth token validation ────────────────────────────────────
        builder.Services.AddTransient<IGoogleTokenValidator, LinguaSpace.Infrastructure.Auth.GoogleTokenValidator>();
    }
}
