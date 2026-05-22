using System.Text.Json;

namespace LinguaSpace.Application.Notifications.DTOs;

/// <summary>
/// A notification delivered to the current user.
/// The <see cref="Payload"/> field contains type-specific data — see payload shapes below.
/// </summary>
/// <remarks>
/// Payload shapes by <see cref="Type"/>:
/// <list type="table">
///   <listheader><term>Type</term><description>Payload fields</description></listheader>
///   <item><term>FriendRequest</term><description><c>{ requesterId, requesterDisplayName, requestId }</c></description></item>
///   <item><term>FriendAccepted</term><description><c>{ acceptorId, acceptorDisplayName }</c></description></item>
///   <item><term>NewFollower</term><description><c>{ followerId, followerDisplayName }</c></description></item>
///   <item><term>RoomInvite</term><description><c>{ roomId, roomTitle, inviterId, inviterDisplayName }</c></description></item>
///   <item><term>PostLike</term><description><c>{ postId, likerId, likerDisplayName }</c></description></item>
///   <item><term>PostComment</term><description><c>{ postId, commentId, commenterId, commenterDisplayName, commentPreview }</c></description></item>
///   <item><term>CommentLike</term><description><c>{ commentId, postId, likerId, likerDisplayName }</c></description></item>
///   <item><term>DirectMessage</term><description><c>{ senderId, senderDisplayName, conversationId, messagePreview }</c></description></item>
///   <item><term>BadgeEarned</term><description><c>{ badgeId, badgeName, badgeIconUrl }</c></description></item>
///   <item><term>SystemMessage</term><description><c>{ message, actionUrl? }</c></description></item>
/// </list>
/// </remarks>
public record NotificationDto(
    int Id,
    NotificationType Type,
    JsonElement? Payload,
    bool IsRead,
    DateTimeOffset CreatedAt);
