using LinguaSpace.Application.Common.Models;

namespace LinguaSpace.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the LiveKit SFU server.
/// Token generation lives in Application; transport details in Infrastructure.
/// </summary>
public interface ISfuService
{
    /// <summary>Generate a LiveKit access token for a participant.</summary>
    Task<string> GenerateTokenAsync(
        string roomName,
        string userId,
        string displayName,
        SfuPermissions permissions,
        CancellationToken cancellationToken = default);

    /// <summary>Return the list of identity strings currently connected to the room.</summary>
    Task<IReadOnlyList<string>> GetRoomParticipantsAsync(
        string roomName,
        CancellationToken cancellationToken = default);

    /// <summary>Force-mute a participant's microphone via the server API.</summary>
    Task MuteParticipantAsync(
        string roomName,
        string participantIdentity,
        CancellationToken cancellationToken = default);

    /// <summary>Kick a participant from the LiveKit room.</summary>
    Task RemoveParticipantAsync(
        string roomName,
        string participantIdentity,
        CancellationToken cancellationToken = default);

    /// <summary>Terminate the LiveKit room and disconnect all participants.</summary>
    Task EndRoomAsync(
        string roomName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate the HMAC-SHA256 signature on an incoming LiveKit webhook request.
    /// </summary>
    bool VerifyWebhookSignature(string rawBody, string authHeader);
}
