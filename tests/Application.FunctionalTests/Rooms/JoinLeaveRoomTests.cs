using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Rooms.Commands.CreateRoom;
using LinguaSpace.Application.Rooms.Commands.JoinRoom;
using LinguaSpace.Application.Rooms.Commands.LeaveRoom;
using LinguaSpace.Domain.Entities;
using LinguaSpace.Domain.Enums;
using LinguaSpace.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using AppNotFoundException = LinguaSpace.Application.Common.Exceptions.NotFoundException;

namespace LinguaSpace.Application.FunctionalTests.Rooms;

/// <summary>
/// Functional tests for JoinRoomCommand and LeaveRoomCommand.
/// </summary>
public class JoinLeaveRoomTests : TestBase
{
    private async Task<(int RoomId, string HostId)> CreateRoomAsync()
    {
        string hostId = await TestApp.RegisterAndSetCurrentUserAsync("host@test.com");

        int roomId = await TestApp.SendAsync(new CreateRoomCommand(
            Title: "Test Room",
            Description: null,
            LanguageCode: "en",
            MaxParticipants: 3,
            RoomType: RoomType.TextOnly));

        return (roomId, hostId);
    }

    // ─── Join ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task ShouldJoinRoomAsParticipant()
    {
        (int roomId, _) = await CreateRoomAsync();

        // Register a second user and join the room
        await TestApp.RegisterAndSetCurrentUserAsync("joiner@test.com");
        await TestApp.SendAsync(new JoinRoomCommand(roomId));

        // Verify participant added to DB
        using IServiceScope scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        int participantCount = await context.RoomParticipants
            .CountAsync(p => p.RoomId == roomId);

        participantCount.ShouldBe(2); // host + joiner
    }

    [Test]
    public async Task JoinRoomShouldBeIdempotent()
    {
        (int roomId, _) = await CreateRoomAsync();

        await TestApp.RegisterAndSetCurrentUserAsync("joiner@test.com");

        // Join twice — should not throw, should not add duplicate
        await TestApp.SendAsync(new JoinRoomCommand(roomId));
        await TestApp.SendAsync(new JoinRoomCommand(roomId));

        using IServiceScope scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        string? joinerId = TestApp.GetUserId();
        int count = await context.RoomParticipants
            .CountAsync(p => p.RoomId == roomId && p.UserId == joinerId);

        count.ShouldBe(1); // no duplicate
    }

    [Test]
    public async Task ShouldThrowWhenRoomIsFull()
    {
        // Room has MaxParticipants=3, host already occupies 1 slot → 2 more can join
        (int roomId, _) = await CreateRoomAsync();

        await TestApp.RegisterAndSetCurrentUserAsync("user2@test.com");
        await TestApp.SendAsync(new JoinRoomCommand(roomId));

        await TestApp.RegisterAndSetCurrentUserAsync("user3@test.com");
        await TestApp.SendAsync(new JoinRoomCommand(roomId));

        // Fourth user should get "Room is full" error
        await TestApp.RegisterAndSetCurrentUserAsync("user4@test.com");

        ValidationException ex = await Should.ThrowAsync<ValidationException>(
            () => TestApp.SendAsync(new JoinRoomCommand(roomId)));

        ex.Errors.Values
            .SelectMany(v => v)
            .ShouldContain(msg => msg.Contains("full"));
    }

    [Test]
    public async Task ShouldThrowNotFoundForNonExistentRoom()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        await Should.ThrowAsync<AppNotFoundException>(
            () => TestApp.SendAsync(new JoinRoomCommand(9999)));
    }

    // ─── Leave ────────────────────────────────────────────────────────────────

    [Test]
    public async Task ParticipantShouldLeaveRoom()
    {
        (int roomId, _) = await CreateRoomAsync();

        string joinerId = await TestApp.RegisterAndSetCurrentUserAsync("leaver@test.com");
        await TestApp.SendAsync(new JoinRoomCommand(roomId));

        await TestApp.SendAsync(new LeaveRoomCommand(roomId));

        using IServiceScope scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        bool stillParticipant = await context.RoomParticipants
            .AnyAsync(p => p.RoomId == roomId && p.UserId == joinerId);

        stillParticipant.ShouldBeFalse();
    }

    [Test]
    public async Task WhenHostLeavesRoomShouldClose()
    {
        // Create room — host is current user
        string hostId = await TestApp.RegisterAndSetCurrentUserAsync("host2@test.com");

        int roomId = await TestApp.SendAsync(new CreateRoomCommand(
            "Host Leave Room", null, "en", 5, RoomType.TextOnly));

        // Host leaves → UserLeftRoomEventHandler should close the room
        await TestApp.SendAsync(new LeaveRoomCommand(roomId));

        Room? room = await TestApp.FindAsync<Room>(roomId);
        room.ShouldNotBeNull();
        room.Status.ShouldBe(RoomStatus.Closed);
    }

    [Test]
    public async Task LeaveRoomShouldBeIdempotent()
    {
        (int roomId, _) = await CreateRoomAsync();

        await TestApp.RegisterAndSetCurrentUserAsync("leaver2@test.com");
        await TestApp.SendAsync(new JoinRoomCommand(roomId));

        // Leave twice — second should be a no-op (idempotent)
        await TestApp.SendAsync(new LeaveRoomCommand(roomId));
        await Should.NotThrowAsync(
            () => TestApp.SendAsync(new LeaveRoomCommand(roomId)));
    }
}
