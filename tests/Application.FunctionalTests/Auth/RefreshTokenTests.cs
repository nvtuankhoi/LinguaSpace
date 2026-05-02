using LinguaSpace.Application.Auth.Commands.Login;
using LinguaSpace.Application.Auth.Commands.RefreshToken;
using LinguaSpace.Application.Auth.Commands.Register;
using LinguaSpace.Application.Auth.DTOs;

namespace LinguaSpace.Application.FunctionalTests.Auth;

/// <summary>
/// Functional tests for RefreshTokenCommand.
///
/// The refresh token flow:
/// 1. Register → Login → get access token + refresh token
/// 2. Send refresh token → get a new access token + new refresh token (rotation)
/// 3. Old refresh token is invalidated after rotation
/// </summary>
public class RefreshTokenTests : TestBase
{
    private async Task<(string AccessToken, string RefreshToken)> RegisterAndLoginAsync(
        string email = "refresh@test.com",
        string password = "Testing1234!")
    {
        await TestApp.SendAsync(new RegisterCommand(email, password));

        LoginResult login = await TestApp.SendAsync(new LoginCommand(email, password));

        return (login.AccessToken, login.RefreshToken);
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Test]
    public async Task ShouldReturnNewAccessTokenAndRefreshToken()
    {
        (_, string refreshToken) = await RegisterAndLoginAsync();

        TokenResult result = await TestApp.SendAsync(new RefreshTokenCommand(refreshToken));

        result.AccessToken.ShouldNotBeNullOrEmpty();
        result.NewRefreshToken.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task NewAccessTokenShouldBeJwt()
    {
        (_, string refreshToken) = await RegisterAndLoginAsync();

        TokenResult result = await TestApp.SendAsync(new RefreshTokenCommand(refreshToken));

        string[] parts = result.AccessToken.Split('.');
        parts.Length.ShouldBe(3);
    }

    [Test]
    public async Task NewRefreshTokenShouldBeDifferentFromOld()
    {
        (_, string oldRefreshToken) = await RegisterAndLoginAsync();

        TokenResult result = await TestApp.SendAsync(new RefreshTokenCommand(oldRefreshToken));

        result.NewRefreshToken.ShouldNotBe(oldRefreshToken);
    }

    [Test]
    public async Task AccessTokenExpiresInShouldBe900Seconds()
    {
        (_, string refreshToken) = await RegisterAndLoginAsync();

        TokenResult result = await TestApp.SendAsync(new RefreshTokenCommand(refreshToken));

        result.ExpiresIn.ShouldBe(900);
    }

    // ─── Token rotation — old token invalidated ───────────────────────────────

    [Test]
    public async Task UsingOldRefreshTokenAfterRotationShouldThrow()
    {
        (_, string oldRefreshToken) = await RegisterAndLoginAsync();

        // First refresh — rotates the token
        await TestApp.SendAsync(new RefreshTokenCommand(oldRefreshToken));

        // Second attempt with old token should fail
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => TestApp.SendAsync(new RefreshTokenCommand(oldRefreshToken)));
    }

    [Test]
    public async Task NewRefreshTokenShouldBeUsableOnce()
    {
        (_, string firstRefreshToken) = await RegisterAndLoginAsync();

        TokenResult rotated = await TestApp.SendAsync(new RefreshTokenCommand(firstRefreshToken));

        // New token should work once
        TokenResult secondRotated = await TestApp.SendAsync(
            new RefreshTokenCommand(rotated.NewRefreshToken));

        secondRotated.AccessToken.ShouldNotBeNullOrEmpty();
    }

    // ─── Error cases ──────────────────────────────────────────────────────────

    [Test]
    public async Task ShouldThrowOnInvalidToken()
    {
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => TestApp.SendAsync(new RefreshTokenCommand("definitely-not-a-real-token")));
    }

    [Test]
    public async Task ShouldThrowOnEmptyToken()
    {
        await Should.ThrowAsync<Exception>(
            () => TestApp.SendAsync(new RefreshTokenCommand(string.Empty)));
    }
}
