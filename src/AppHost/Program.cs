using LinguaSpace.Shared;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aca-env");

var databaseServer = builder
    .AddAzurePostgresFlexibleServer(Services.DatabaseServer)
    .WithPasswordAuthentication()
    .RunAsContainer(container => 
        container.WithLifetime(ContainerLifetime.Persistent))
    .AddDatabase(Services.Database);

var redis = builder
    .AddRedis(Services.Cache)
    .WithLifetime(ContainerLifetime.Persistent);

// LiveKit SFU — local dev only (not deployed to ACA)
// Uses a fixed dev key/secret matching appsettings.json LiveKit:ApiKey / ApiSecret
// Clients connect on ws://localhost:7880 (HTTP/WS API) and localhost:7881 (RTC/TCP)
// UDP 50000-60000 is used for media — map locally or use --dev flag for single-node
var liveKit = builder
    .AddContainer(Services.LiveKit, "livekit/livekit-server", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(port: 7880, targetPort: 7880, name: "http")
    .WithEndpoint(port: 7881, targetPort: 7881, name: "rtc-tcp")
    .WithArgs("--dev");   // --dev enables no-auth TURN, generates key=devkey secret=secret

var web = builder.AddProject<Projects.Web>(Services.WebApi)
    .WithReference(databaseServer)
    .WaitFor(databaseServer)
    .WithReference(redis)
    .WaitFor(redis)
    .WaitFor(liveKit)
    .WithExternalHttpEndpoints()
    .WithAspNetCoreEnvironment()
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar API Reference";
        url.Url = "/scalar";
    });


builder.Build().Run();
