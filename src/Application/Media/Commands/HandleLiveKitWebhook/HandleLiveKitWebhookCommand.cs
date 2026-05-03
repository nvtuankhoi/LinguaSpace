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

    public HandleLiveKitWebhookCommandHandler(
        ISfuService sfuService,
        IApplicationDbContext context)
    {
        _sfuService = sfuService;
        _context = context;
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
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
