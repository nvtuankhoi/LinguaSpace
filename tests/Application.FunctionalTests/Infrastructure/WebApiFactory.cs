using LinguaSpace.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LinguaSpace.Application.FunctionalTests.Infrastructure;

public class WebApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide all required configuration before AddInfrastructureServices() runs.
        // UseSetting overrides appsettings values and runs BEFORE service registration.
        builder.UseSetting("ConnectionStrings:LinguaSpaceDb", connectionString);

        // Redis: abortConnect=false prevents StackExchange.Redis from throwing on startup
        // when Redis is not available. Connections are lazy and will fail silently in tests.
        builder.UseSetting("ConnectionStrings:cache", "localhost:6379,abortConnect=false");

        // JWT: must match the values used by JwtTokenService (DI reads these at startup)
        builder.UseSetting("Jwt:Key", "test-secret-key-must-be-at-least-32chars!!!");
        builder.UseSetting("Jwt:Issuer", "LinguaSpace");
        builder.UseSetting("Jwt:Audience", "LinguaSpaceApi");

        builder.ConfigureTestServices(services =>
        {
            // Replace IUser with a mock driven by TestApp state (userId, roles).
            services
                .RemoveAll<IUser>()
                .AddTransient(provider =>
                {
                    Mock<IUser> mock = new();
                    mock.SetupGet(x => x.Roles).Returns(TestApp.GetRoles());
                    mock.SetupGet(x => x.Id).Returns(TestApp.GetUserId());
                    return mock.Object;
                });

            // Replace ICacheService with a no-op in-memory implementation.
            // Tests don't need caching behaviour, and Redis is not available in TestAppHost.
            services.RemoveAll<ICacheService>();
            services.AddSingleton<ICacheService>(new NullCacheService());
        });
    }
}
