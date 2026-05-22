using Azure.Identity;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Infrastructure.Data;
using LinguaSpace.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddScoped<IUser, CurrentUser>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

        // Customise default API behaviour
        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        builder.Services.AddEndpointsApiExplorer();

        // Serialize enums as strings in all JSON responses (affects Minimal API + OpenAPI)
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddOpenApi(options =>
        {
            options.AddOperationTransformer<ApiExceptionOperationTransformer>();
            options.AddOperationTransformer<BearerSecurityOperationTransformer>();
            // IdentityApiOperationTransformer is removed — we no longer use MapIdentityApi
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        // Rate limiting — two policies:
        //  "api"  : 100 requests / 1 minute per IP (general)
        //  "auth" : 10  requests / 1 minute per IP (sensitive auth endpoints)
        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy("api", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 100,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    }));

            options.AddPolicy("auth", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 10,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        // CORS: AllowAnyOrigin() + AllowCredentials() is INVALID (browser blocks it).
        // Must specify exact origin(s) when credentials (cookies/auth headers) are needed.
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("LinguaSpacePolicy", policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:5173",  // Vite / React dev server
                        "http://localhost:4200"   // Angular dev server (if needed)
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials(); // Required for HttpOnly refresh token cookie
            });
        });
    }

    public static void AddKeyVaultIfConfigured(this IHostApplicationBuilder builder)
    {
        string? keyVaultUri = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential());
        }
    }
}
