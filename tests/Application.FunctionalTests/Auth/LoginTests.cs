using LinguaSpace.Application.Auth.Commands.Login;
using LinguaSpace.Application.Auth.DTOs;

namespace LinguaSpace.Application.FunctionalTests.Auth;

/// <summary>
/// Functional tests for LoginCommand.
///
/// Login flow:
/// 1. Register user (creates ApplicationUser in DB)
/// 2. Login with same credentials → returns access token + refresh token
/// 3. Wrong password or unknown email → UnauthorizedAccessException
/// </summary>
public class LoginTests : TestBase
{
    [Test]
    public async Task ShouldReturnTokensOnValidCredentials()
    {
        await TestApp.SendAsync(
            new RegisterCommand("login@test.com", "Testing1234!"));

        LoginResult result = await TestApp.SendAsync(
            new LoginCommand("login@test.com", "Testing1234!"));

        result.AccessToken.ShouldNotBeNullOrEmpty();
        result.RefreshToken.ShouldNotBeNullOrEmpty();
        result.Email.ShouldBe("login@test.com");
        result.ExpiresIn.ShouldBe(900); // 15 minutes
    }

    [Test]
    public async Task ShouldThrowOnWrongPassword()
    {
        await TestApp.SendAsync(
            new RegisterCommand("user@test.com", "Testing1234!"));

        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => TestApp.SendAsync(new LoginCommand("user@test.com", "WrongPassword!")));
    }

    [Test]
    public async Task ShouldThrowOnUnknownEmail()
    {
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => TestApp.SendAsync(new LoginCommand("ghost@test.com", "Testing1234!")));
    }

    [Test]
    public async Task AccessTokenShouldBeJwt()
    {
        await TestApp.SendAsync(
            new RegisterCommand("jwt@test.com", "Testing1234!"));

        LoginResult result = await TestApp.SendAsync(
            new LoginCommand("jwt@test.com", "Testing1234!"));

        // JWT has 3 parts separated by '.'
        string[] parts = result.AccessToken.Split('.');
        parts.Length.ShouldBe(3);
    }
}
