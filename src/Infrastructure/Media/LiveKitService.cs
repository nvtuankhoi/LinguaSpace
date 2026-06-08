using Livekit.Server.Sdk.Dotnet;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LinguaSpace.Infrastructure.Media;

/// <summary>
/// LiveKit SFU implementation of <see cref="ISfuService"/>.
/// Uses the official Livekit.Server.Sdk.Dotnet package.
/// Configuration keys: LiveKit:ApiKey, LiveKit:ApiSecret, LiveKit:Host.
/// </summary>
public class LiveKitService : ISfuService
{
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly string _host;
    private readonly ILogger<LiveKitService> _logger;

    public LiveKitService(IConfiguration configuration, ILogger<LiveKitService> logger)
    {
        _apiKey = configuration["LiveKit:ApiKey"] ?? "devkey";
        _apiSecret = configuration["LiveKit:ApiSecret"] ?? "devsecret";
        _host = configuration["LiveKit:Host"] ?? "http://localhost:7880";
        _logger = logger;
    }

    public Task<string> GenerateTokenAsync(
        string roomName,
        string userId,
        string displayName,
        SfuPermissions permissions,
        CancellationToken cancellationToken = default)
    {
        bool canPublish = permissions.CanPublishAudio || permissions.CanPublishVideo;

        AccessToken token = new AccessToken(_apiKey, _apiSecret)
            .WithIdentity(userId)
            .WithName(displayName)
            .WithGrants(new VideoGrants
            {
                RoomJoin = true,
                Room = roomName,
                CanPublish = canPublish,
                CanSubscribe = permissions.CanSubscribe,
                CanPublishData = permissions.CanPublishData,
            });

        return Task.FromResult(token.ToJwt());
    }

    public async Task<IReadOnlyList<string>> GetRoomParticipantsAsync(
        string roomName,
        CancellationToken cancellationToken = default)
    {
        RoomServiceClient client = CreateRoomClient();
        ListParticipantsResponse response = await client.ListParticipants(new ListParticipantsRequest
        {
            Room = roomName,
        });

        return response.Participants.Select(p => p.Identity).ToList();
    }

    public async Task MuteParticipantAsync(
        string roomName,
        string participantIdentity,
        CancellationToken cancellationToken = default)
    {
        RoomServiceClient client = CreateRoomClient();

        // Get participant's published tracks and mute audio track
        ParticipantInfo participant = await client.GetParticipant(new RoomParticipantIdentity
        {
            Room = roomName,
            Identity = participantIdentity,
        });

        TrackInfo? audioTrack = participant.Tracks.FirstOrDefault(t => t.Source == TrackSource.Microphone);

        if (audioTrack is not null)
        {
            await client.MutePublishedTrack(new MuteRoomTrackRequest
            {
                Room = roomName,
                Identity = participantIdentity,
                TrackSid = audioTrack.Sid,
                Muted = true,
            });
        }
    }

    public async Task RemoveParticipantAsync(
        string roomName,
        string participantIdentity,
        CancellationToken cancellationToken = default)
    {
        RoomServiceClient client = CreateRoomClient();
        await client.RemoveParticipant(new RoomParticipantIdentity
        {
            Room = roomName,
            Identity = participantIdentity,
        });
    }

    public async Task EndRoomAsync(
        string roomName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            RoomServiceClient client = CreateRoomClient();
            await client.DeleteRoom(new DeleteRoomRequest { Room = roomName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end LiveKit room {RoomName}. LiveKit host may be offline.", roomName);
        }
    }

    public bool VerifyWebhookSignature(string rawBody, string authHeader)
    {
        try
        {
            WebhookReceiver receiver = new(_apiKey, _apiSecret);
            receiver.Receive(rawBody, authHeader);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("LiveKit webhook signature verification failed: {Message}", ex.Message);
            return false;
        }
    }

    private RoomServiceClient CreateRoomClient() => new(_host, _apiKey, _apiSecret);
}
