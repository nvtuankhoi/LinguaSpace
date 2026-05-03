using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using LinguaSpace.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LinguaSpace.Infrastructure.PushNotifications;

/// <summary>
/// Firebase Cloud Messaging (FCM) implementation of <see cref="IPushNotificationService"/>.
/// Reads device FCM tokens from <see cref="IApplicationDbContext.UserDevices"/>.
/// Configuration: Firebase service account JSON path via "Firebase:CredentialPath",
/// or relies on Application Default Credentials if not set.
/// </summary>
public class FcmPushNotificationService : IPushNotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly FirebaseMessaging _messaging;
    private readonly ILogger<FcmPushNotificationService> _logger;

    public FcmPushNotificationService(
        IApplicationDbContext context,
        IConfiguration configuration,
        ILogger<FcmPushNotificationService> logger)
    {
        _context = context;
        _logger = logger;

        // Initialize Firebase app (singleton-safe: won't throw if already initialized)
        if (FirebaseApp.DefaultInstance is null)
        {
            string? credPath = configuration["Firebase:CredentialPath"];

            GoogleCredential credential = string.IsNullOrEmpty(credPath)
                ? GoogleCredential.GetApplicationDefault()
                : GoogleCredential.FromServiceAccountCredential(
                    ServiceAccountCredential.FromServiceAccountData(
                        System.IO.File.OpenRead(credPath)));

            FirebaseApp.Create(new AppOptions { Credential = credential });
        }

        _messaging = FirebaseMessaging.DefaultInstance;
    }

    public async Task SendAsync(
        string userId,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        IList<string> tokens = await GetTokensAsync(userId, cancellationToken);

        if (tokens.Count == 0)
        {
            return;
        }

        foreach (string token in tokens)
        {
            try
            {
                await _messaging.SendAsync(new FirebaseAdmin.Messaging.Message
                {
                    Token = token,
                    Notification = new FirebaseAdmin.Messaging.Notification { Title = title, Body = body },
                    Data = data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                }, cancellationToken);
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogWarning("FCM send failed for token {Token}: {Message}", token, ex.Message);
            }
        }
    }

    public async Task SendMulticastAsync(
        IEnumerable<string> userIds,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        List<string> allTokens = new();

        foreach (string userId in userIds)
        {
            IList<string> tokens = await GetTokensAsync(userId, cancellationToken);
            allTokens.AddRange(tokens);
        }

        if (allTokens.Count == 0)
        {
            return;
        }

        // FCM multicast: max 500 tokens per batch
        const int batchSize = 500;
        for (int i = 0; i < allTokens.Count; i += batchSize)
        {
            List<string> batch = allTokens.GetRange(i, Math.Min(batchSize, allTokens.Count - i));

            try
            {
                await _messaging.SendEachForMulticastAsync(new MulticastMessage
                {
                    Tokens = batch,
                    Notification = new FirebaseAdmin.Messaging.Notification { Title = title, Body = body },
                    Data = data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("FCM multicast failed for batch: {Message}", ex.Message);
            }
        }
    }

    private async Task<IList<string>> GetTokensAsync(string userId, CancellationToken cancellationToken)
    {
        List<string> tokens = await _context.UserDevices
            .Where(d => d.UserId == userId && d.IsActive && !string.IsNullOrEmpty(d.FcmToken))
            .Select(d => d.FcmToken)
            .ToListAsync<string>(cancellationToken);

        return tokens;
    }
}
