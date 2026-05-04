using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Media.Commands.HandleLiveKitWebhook;

/// <summary>
/// Processes an incoming LiveKit webhook event.
/// The web layer passes the raw body and auth header; we verify HMAC here and raise domain events.
/// </summary>
public record HandleLiveKitWebhookCommand(
    string RawBody,
    string AuthHeader) : IRequest;

public class HandleLiveKitWebhookCommandHandler : IRequestHandler<HandleLiveKitWebhookCommand>
{
    private readonly ISfuService _sfuService;
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public HandleLiveKitWebhookCommandHandler(
        ISfuService sfuService,
        IApplicationDbContext context,
        INotificationService notificationService)
    {
        _sfuService = sfuService;
        _context = context;
        _notificationService = notificationService;
    }

    public async Task Handle(
        HandleLiveKitWebhookCommand request,
        CancellationToken cancellationToken)
    {
        if (!_sfuService.VerifyWebhookSignature(request.RawBody, request.AuthHeader))
        {
            return; // Silently ignore invalid signatures
        }

        // Parse the LiveKit room name + user identity from the raw JSON body
        // (minimal parsing without pulling protobuf types into Application layer)
        System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(request.RawBody);
        System.Text.Json.JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("event", out System.Text.Json.JsonElement eventEl))
        {
            return;
        }

        string eventName = eventEl.GetString() ?? string.Empty;

        // Extract room name and participant identity
        root.TryGetProperty("room", out System.Text.Json.JsonElement roomEl);
        root.TryGetProperty("participant", out System.Text.Json.JsonElement participantEl);

        string livekitRoomName = roomEl.ValueKind != System.Text.Json.JsonValueKind.Undefined
            ? roomEl.TryGetProperty("name", out System.Text.Json.JsonElement nameEl) ? nameEl.GetString() ?? "" : ""
            : "";

        string participantIdentity = participantEl.ValueKind != System.Text.Json.JsonValueKind.Undefined
            ? participantEl.TryGetProperty("identity", out System.Text.Json.JsonElement identEl) ? identEl.GetString() ?? "" : ""
            : "";

        // active_speakers_changed doesn't require a participant — handle early
        if (eventName == "active_speakers_changed")
        {
            await HandleActiveSpeakersChanged(root, livekitRoomName, cancellationToken);
            return;
        }

        // Resolve the numeric Room ID from LiveKitRoomName (e.g., "room-5" → 5)
        Room? room = await _context.Rooms
            .FirstOrDefaultAsync(
                r => r.LiveKitRoomName == livekitRoomName || ("room-" + r.Id) == livekitRoomName,
                cancellationToken);

        if (room is null || string.IsNullOrEmpty(participantIdentity))
        {
            return;
        }

        switch (eventName)
        {
            case "participant_joined":
                room.AddDomainEvent(new ParticipantJoinedMediaEvent(room.Id, participantIdentity));
                break;

            case "participant_left":
                // Find the active media session to include its ID in the event
                RoomMediaSession? session = await _context.RoomMediaSessions
                    .Where(s => s.RoomId == room.Id && s.UserId == participantIdentity && s.LeftAt == null)
                    .FirstOrDefaultAsync(cancellationToken);

                if (session is not null)
                {
                    room.AddDomainEvent(new ParticipantLeftMediaEvent(room.Id, participantIdentity, session.Id));
                }
                break;

            case "room_finished":
                // All sessions should be closed by the infrastructure
                IList<RoomMediaSession> activeSessions = await _context.RoomMediaSessions
                    .Where(s => s.RoomId == room.Id && s.LeftAt == null)
                    .ToListAsync(cancellationToken);

                foreach (RoomMediaSession s in activeSessions)
                {
                    room.AddDomainEvent(new ParticipantLeftMediaEvent(room.Id, s.UserId, s.Id));
                }
                break;

            case "track_published":
                await HandleTrackPublished(root, room, participantIdentity, cancellationToken);
                break;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleActiveSpeakersChanged(
        System.Text.Json.JsonElement root,
        string livekitRoomName,
        CancellationToken cancellationToken)
    {
        Room? room = await _context.Rooms
            .FirstOrDefaultAsync(
                r => r.LiveKitRoomName == livekitRoomName || ("room-" + r.Id) == livekitRoomName,
                cancellationToken);

        if (room is null)
        {
            return;
        }

        // Parse activeSpeakers array: [{identity: "...", ...}, ...]
        List<string> speakerIdentities = new();
        if (root.TryGetProperty("activeSpeakers", out System.Text.Json.JsonElement speakersEl)
            && speakersEl.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (System.Text.Json.JsonElement speaker in speakersEl.EnumerateArray())
            {
                if (speaker.TryGetProperty("identity", out System.Text.Json.JsonElement identEl))
                {
                    string identity = identEl.GetString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(identity))
                    {
                        speakerIdentities.Add(identity);
                    }
                }
            }
        }

        await _notificationService.NotifyGroupAsync(
            $"room-{room.Id}",
            "ActiveSpeakerChanged",
            new { speakerIds = speakerIdentities },
            cancellationToken);
    }

    private async Task HandleTrackPublished(
        System.Text.Json.JsonElement root,
        Room room,
        string participantIdentity,
        CancellationToken cancellationToken)
    {
        // Check if the published track is a screen share
        bool isScreenShare = false;
        if (root.TryGetProperty("track", out System.Text.Json.JsonElement trackEl)
            && trackEl.TryGetProperty("source", out System.Text.Json.JsonElement sourceEl))
        {
            string source = sourceEl.GetString() ?? string.Empty;
            isScreenShare = source == "screen_share";
        }

        if (!isScreenShare)
        {
            return;
        }

        RoomMediaSession? session = await _context.RoomMediaSessions
            .Where(s => s.RoomId == room.Id && s.UserId == participantIdentity && s.LeftAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (session is not null)
        {
            session.WasScreenSharing = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

