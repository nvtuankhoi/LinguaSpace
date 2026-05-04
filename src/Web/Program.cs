using LinguaSpace.Infrastructure.Data;
using LinguaSpace.Infrastructure.Hubs;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// CORS must be before UseAuthentication/UseAuthorization
app.UseCors("LinguaSpacePolicy");

app.UseFileServer();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseExceptionHandler(options => { });

// Authentication/Authorization middleware must be in this exact order
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.Map("/", () => Results.Redirect("/scalar"));

app.MapDefaultEndpoints();
app.MapEndpoints(typeof(Program).Assembly);

// SignalR Hubs
// Client connects via: new HubConnectionBuilder().withUrl("/hubs/room?access_token=<jwt>")
app.MapHub<RoomHub>("/hubs/room");
app.MapHub<PresenceHub>("/hubs/presence");

app.Run();
