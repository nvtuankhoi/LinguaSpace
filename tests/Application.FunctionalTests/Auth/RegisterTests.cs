using LinguaSpace.Application.Auth.Commands.Login;
using LinguaSpace.Application.Auth.Commands.Register;
using LinguaSpace.Application.Auth.DTOs;
using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Domain.Entities;
using LinguaSpace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinguaSpace.Application.FunctionalTests.Auth;

/// <summary>
/// Functional tests for RegisterCommand.
///
/// Key patterns:
/// - Inherits TestBase → DB is reset before each test (via Respawn)
/// - Uses TestApp.SendAsync() to dispatch MediatR through real DI + real DB
/// - Uses TestApp.CountAsync() / FindAsync() to verify DB state
/// - No need to set up IUser mock for Register (command doesn't use IUser.Id)
/// </summary>
public class RegisterTests : TestBase
{
    // ─── Happy path ───────────────────────────────────────────────────────────

    [Test]
    public async Task ShouldRegisterUserAndCreateProfile()
    {
        RegisterResult result = await TestApp.SendAsync(
            new RegisterCommand("newuser@test.com", "Testing1234!"));

        // Returns userId and email
        result.UserId.ShouldNotBeNullOrEmpty();
        result.Email.ShouldBe("newuser@test.com");

        // UserProfile was created by UserRegisteredEventHandler (via domain event)
        int profileCount = await TestApp.CountAsync<UserProfile>();
        profileCount.ShouldBe(1);
    }

    [Test]
    public async Task ShouldReturnUserIdAndEmail()
    {
        RegisterResult result = await TestApp.SendAsync(
            new RegisterCommand("alice@test.com", "Testing1234!"));

        result.UserId.ShouldNotBeNullOrEmpty();
        result.Email.ShouldBe("alice@test.com");
    }

    [Test]
    public async Task UserProfileDisplayNameShouldDefaultToEmailUsername()
    {
        await TestApp.SendAsync(new RegisterCommand("alice@test.com", "Testing1234!"));

        // Check DisplayName — by convention set to email prefix by UserRegisteredEventHandler
        using IServiceScope scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        UserProfile? profile = await context.UserProfiles.FirstOrDefaultAsync();
        profile.ShouldNotBeNull();
        profile.DisplayName.ShouldBe("alice"); // email prefix before @
    }

    // ─── Validation errors ────────────────────────────────────────────────────

    [Test]
    public async Task ShouldThrowValidationExceptionForInvalidEmail()
    {
        ValidationException ex = await Should.ThrowAsync<ValidationException>(
            () => TestApp.SendAsync(new RegisterCommand("not-an-email", "Testing1234!")));

        ex.Errors.ShouldContainKey("Email");
    }

    [Test]
    public async Task ShouldThrowValidationExceptionForShortPassword()
    {
        ValidationException ex = await Should.ThrowAsync<ValidationException>(
            () => TestApp.SendAsync(new RegisterCommand("valid@test.com", "Short")));

        ex.Errors.ShouldContainKey("Password");
    }

    // ─── Duplicate registration ───────────────────────────────────────────────

    [Test]
    public async Task ShouldFailWhenEmailAlreadyRegistered()
    {
        await TestApp.SendAsync(new RegisterCommand("duplicate@test.com", "Testing1234!"));

        // Second registration with same email should fail
        await Should.ThrowAsync<Exception>(
            () => TestApp.SendAsync(new RegisterCommand("duplicate@test.com", "OtherPass1234!")));
    }
}
