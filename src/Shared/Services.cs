namespace LinguaSpace.Shared;

public static class Services
{
    /// <summary>
    /// The name of the Web Frontend service.
    /// This service is responsible for hosting the frontend application.
    /// </summary>
    public const string WebFrontend = "webfrontend";

    /// <summary>
    /// The name of the Web API service.
    /// This service is responsible for hosting the Web API application.
    /// </summary>
    public const string WebApi = "webapi";

    /// <summary>
    /// The name of the Database Server service.
    /// This service is responsible for hosting the database server (e.g., PostgreSQL, SQL Server, or SQLite).
    /// </summary>
    public const string DatabaseServer = "dbserver";

    /// <summary>
    /// The name of the Database.
    /// This is the name of the database that will be created and used by the application.
    /// </summary>
    public const string Database = "LinguaSpaceDb";

    /// <summary>
    /// The name of the Redis cache service.
    /// Used as the SignalR backplane and distributed cache.
    /// </summary>
    public const string Cache = "cache";

    /// <summary>
    /// The name of the LiveKit SFU service used for voice/video.
    /// Running as a local dev container via Aspire.
    /// </summary>
    public const string LiveKit = "livekit";
}
