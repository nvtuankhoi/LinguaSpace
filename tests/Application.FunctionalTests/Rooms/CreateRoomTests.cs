using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Rooms.Commands.CreateRoom;
using LinguaSpace.Application.Rooms.DTOs;
using LinguaSpace.Application.Rooms.Queries.GetRooms;
using LinguaSpace.Domain.Entities;
using LinguaSpace.Domain.Enums;
using LinguaSpace.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace LinguaSpace.Application.FunctionalTests.Rooms;

/// <summary>
/// Functional tests for CreateRoomCommand and GetRoomsQuery.
///
/// Setup: RegisterAndSetCurrentUserAsync creates a real user + UserProfile in DB,
/// then sets IUser.Id mock so subsequent commands run as that user.
/// </summary>
public class CreateRoomTests : TestBase
{
    [Test]
    public async Task ShouldCreateRoomAndReturnId()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        int roomId = await TestApp.SendAsync(new CreateRoomCommand(
            Title: "English Chat",
            Description: "Practice English",
            LanguageCode: "en",
            MaxParticipants: 10,
            RoomType: RoomType.TextOnly));

        roomId.ShouldBeGreaterThan(0);

        Room? room = await TestApp.FindAsync<Room>(roomId);
        room.ShouldNotBeNull();
        room.Title.ShouldBe("English Chat");
        room.Status.ShouldBe(RoomStatus.Active);
    }

    [Test]
    public async Task HostShouldBeAutoAddedAsParticipant()
    {
        string userId = await TestApp.RegisterAndSetCurrentUserAsync();

        int roomId = await TestApp.SendAsync(new CreateRoomCommand(
            Title: "French Practice",
            Description: null,
            LanguageCode: "fr",
            MaxParticipants: 5,
            RoomType: RoomType.TextOnly));

        using IServiceScope scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        RoomParticipant? participant = await context.RoomParticipants
            .FirstOrDefaultAsync(p => p.RoomId == roomId && p.UserId == userId);

        participant.ShouldNotBeNull();
        participant.Role.ShouldBe(ParticipantRole.Host);
    }

    [Test]
    public async Task ShouldThrowForbiddenWhenNotAuthenticated()
    {
        // Don't call RegisterAndSetCurrentUserAsync — IUser.Id is null
        await Should.ThrowAsync<ForbiddenAccessException>(
            () => TestApp.SendAsync(new CreateRoomCommand(
                Title: "Test", Description: null, LanguageCode: "en",
                MaxParticipants: 5, RoomType: RoomType.TextOnly)));
    }

    [Test]
    public async Task ShouldThrowValidationExceptionForEmptyTitle()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        ValidationException ex = await Should.ThrowAsync<ValidationException>(
            () => TestApp.SendAsync(new CreateRoomCommand(
                Title: string.Empty, Description: null, LanguageCode: "en",
                MaxParticipants: 5, RoomType: RoomType.TextOnly)));

        ex.Errors.ShouldContainKey("Title");
    }

    [Test]
    public async Task GetRoomsShouldReturnCreatedRoom()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        await TestApp.SendAsync(new CreateRoomCommand(
            Title: "Visible Room",
            Description: null,
            LanguageCode: "en",
            MaxParticipants: 10,
            RoomType: RoomType.TextOnly));

        PaginatedResult<RoomSummaryDto> rooms = await TestApp.SendAsync(
            new GetRoomsQuery(LanguageCode: null, RoomType: null, Q: null));

        rooms.Items.Count.ShouldBe(1);
        rooms.Items[0].Title.ShouldBe("Visible Room");
    }

    [Test]
    public async Task GetRoomsShouldFilterByLanguage()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        await TestApp.SendAsync(new CreateRoomCommand(
            "English Room", null, "en", 10, RoomType.TextOnly));

        await TestApp.SendAsync(new CreateRoomCommand(
            "French Room", null, "fr", 10, RoomType.TextOnly));

        PaginatedResult<RoomSummaryDto> englishRooms = await TestApp.SendAsync(
            new GetRoomsQuery(LanguageCode: "en", RoomType: null, Q: null));

        englishRooms.Items.Count.ShouldBe(1);
        englishRooms.Items[0].Title.ShouldBe("English Room");
    }
}
