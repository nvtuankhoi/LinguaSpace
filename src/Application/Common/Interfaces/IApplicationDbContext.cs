using LinguaSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinguaSpace.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<UserLanguage> UserLanguages { get; }
    DbSet<UserDevice> UserDevices { get; }
    DbSet<Room> Rooms { get; }
    DbSet<RoomParticipant> RoomParticipants { get; }
    DbSet<RoomMediaSession> RoomMediaSessions { get; }
    DbSet<Message> Messages { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<DirectMessage> DirectMessages { get; }
    DbSet<Post> Posts { get; }
    DbSet<PostMediaItem> PostMediaItems { get; }
    DbSet<PostTag> PostTags { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Reaction> Reactions { get; }
    DbSet<Friendship> Friendships { get; }
    DbSet<UserBlock> UserBlocks { get; }
    DbSet<Follow> Follows { get; }
    DbSet<UserXp> UserXps { get; }
    DbSet<Badge> Badges { get; }
    DbSet<UserBadge> UserBadges { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Report> Reports { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

