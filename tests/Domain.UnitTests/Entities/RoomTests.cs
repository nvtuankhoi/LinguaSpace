using LinguaSpace.Domain.Entities;
using LinguaSpace.Domain.Enums;

namespace LinguaSpace.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for Room and RoomParticipant entities.
///
/// Tests entity property contracts and defaults.
/// Business rules (capacity checks, host enforcement) are tested in functional tests
/// since they require the full command handler + database.
/// </summary>
public class RoomTests
{
    // ─── Room ─────────────────────────────────────────────────────────────────

    [Test]
    public void NewRoom_HasExpectedDefaults()
    {
        Room room = new();

        room.Title.ShouldBe(string.Empty);
        room.Description.ShouldBeNull();
        room.LanguageCode.ShouldBe(string.Empty);
        room.MaxParticipants.ShouldBe(20);
        room.Status.ShouldBe(RoomStatus.Active);
        room.RoomType.ShouldBe(RoomType.TextOnly);
        room.LiveKitRoomName.ShouldBeNull();
        room.HostId.ShouldBe(string.Empty);
        room.Participants.ShouldBeEmpty();
        room.Messages.ShouldBeEmpty();
    }

    [Test]
    public void SetTitle_Persists()
    {
        Room room = new() { Title = "English Conversation" };

        room.Title.ShouldBe("English Conversation");
    }

    [Test]
    public void SetLanguageCode_Persists()
    {
        Room room = new() { LanguageCode = "fr" };

        room.LanguageCode.ShouldBe("fr");
    }

    [Test]
    public void SetStatusToClosed_Persists()
    {
        Room room = new();

        room.Status = RoomStatus.Closed;

        room.Status.ShouldBe(RoomStatus.Closed);
    }

    [Test]
    public void SetRoomType_VoiceOnly_Persists()
    {
        Room room = new() { RoomType = RoomType.VoiceOnly };

        room.RoomType.ShouldBe(RoomType.VoiceOnly);
    }

    [Test]
    public void SetMaxParticipants_Custom_Persists()
    {
        Room room = new() { MaxParticipants = 5 };

        room.MaxParticipants.ShouldBe(5);
    }

    [Test]
    public void ParticipantsCollection_CanAddParticipant()
    {
        Room room = new() { Title = "Test", LanguageCode = "en" };

        room.Participants.Add(new RoomParticipant
        {
            UserId = "user-1",
            Role = ParticipantRole.Host,
        });

        room.Participants.Count.ShouldBe(1);
        room.Participants.First().Role.ShouldBe(ParticipantRole.Host);
    }

    // ─── RoomParticipant ──────────────────────────────────────────────────────

    [Test]
    public void NewRoomParticipant_HasExpectedDefaults()
    {
        RoomParticipant participant = new();

        participant.UserId.ShouldBe(string.Empty);
        participant.Role.ShouldBe(ParticipantRole.Speaker);
        participant.IsMuted.ShouldBeFalse();
    }

    [Test]
    public void SetIsMuted_True_Persists()
    {
        RoomParticipant participant = new();

        participant.IsMuted = true;

        participant.IsMuted.ShouldBeTrue();
    }

    [Test]
    public void SetIsMuted_ToggleFalse_Persists()
    {
        RoomParticipant participant = new() { IsMuted = true };

        participant.IsMuted = false;

        participant.IsMuted.ShouldBeFalse();
    }

    [Test]
    public void SetRole_Host_Persists()
    {
        RoomParticipant participant = new() { Role = ParticipantRole.Host };

        participant.Role.ShouldBe(ParticipantRole.Host);
    }
}
