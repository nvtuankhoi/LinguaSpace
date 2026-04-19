using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Rooms.Commands.SendMessage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LinguaSpace.Infrastructure.Hubs;

/// <summary>
/// SignalR Hub for real-time room communication.
/// Clients call server methods (JoinRoomGroup, LeaveRoomGroup, SendMessage).
/// Server broadcasts to groups (ReceiveMessage, UserJoinedRoom, UserLeftRoom).
/// </summary>
[Authorize]
public class RoomHub : Hub
{
    private readonly ISender _sender;
    private readonly IUser _user;

    public RoomHub(ISender sender, IUser user)
    {
        _sender = sender;
        _user = user;
    }

    // ─── Client → Server ────────────────────────────────────────────────────

    /// <summary>
    /// Called by client after navigating to a room page.
    /// Adds this connection to the SignalR group so it receives room broadcasts.
    /// Note: Actual room membership (DB) is managed via HTTP JoinRoomCommand.
    /// </summary>
    public async Task JoinRoomGroup(int roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroupName(roomId));
    }

    /// <summary>
    /// Called by client when leaving a room page.
    /// </summary>
    public async Task LeaveRoomGroup(int roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroupName(roomId));
    }

    /// <summary>
    /// Client sends a message → persisted via MediatR → broadcast to all room members.
    /// This keeps message persistence in the Application layer (not in Hub logic).
    /// </summary>
    public async Task SendMessage(int roomId, string content)
    {
        SendMessageCommand command = new(roomId, content);
        int messageId = await _sender.Send(command);

        // Broadcast to all connections in this room group (including sender)
        await Clients.Group(RoomGroupName(roomId)).SendAsync("ReceiveMessage", new
        {
            MessageId = messageId,
            SenderId = _user.Id,
            Content = content,
            SentAt = DateTimeOffset.UtcNow,
        });
    }

    // ─── Server → Clients (called from outside Hub, e.g. from EventHandlers) ──
    // These are not Hub methods — callers use IHubContext<RoomHub>.

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Note: We don't automatically leave DB room membership on disconnect.
        // Presence (IsOnline) is tracked separately via PresenceHub.
        // If user wants to truly leave a room, they call HTTP POST /api/rooms/{id}/leave.
        await base.OnDisconnectedAsync(exception);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static string RoomGroupName(int roomId) => $"room-{roomId}";
}
