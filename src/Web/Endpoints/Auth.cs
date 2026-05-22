using LinguaSpace.Application.Auth.Commands.ChangeEmail;
using LinguaSpace.Application.Auth.Commands.ChangePassword;
using LinguaSpace.Application.Auth.Commands.ForgotPassword;
using LinguaSpace.Application.Auth.Commands.Login;
using LinguaSpace.Application.Auth.Commands.Logout;
using LinguaSpace.Application.Auth.Commands.OAuthLogin;
using LinguaSpace.Application.Auth.Commands.RefreshToken;
using LinguaSpace.Application.Auth.Commands.Register;
using LinguaSpace.Application.Auth.Commands.RegisterDeviceToken;
using LinguaSpace.Application.Auth.Commands.ResendEmailVerification;
using LinguaSpace.Application.Auth.Commands.ResetPassword;
using LinguaSpace.Application.Auth.Commands.RevokeAllSessions;
using LinguaSpace.Application.Auth.Commands.VerifyEmail;
using LinguaSpace.Application.Auth.DTOs;
using LinguaSpace.Application.Auth.Queries.GetCurrentUser;
using LinguaSpace.Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LinguaSpace.Web.Endpoints;

/// <summary>
/// Auth endpoints: Register, Login, Refresh, Logout, Me.
/// Refresh token strategy: stored as HttpOnly cookie (XSS-safe).
/// Access token returned in response body — client stores in memory (not localStorage).
/// </summary>
public class Auth : IEndpointGroup
{
    // Constant for the HttpOnly cookie name used across Login, Refresh, and Logout.
    private const string RefreshTokenCookie = "refresh_token";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost(Register, "register").AllowAnonymous().RequireRateLimiting("auth");
        group.MapPost(Login, "login").AllowAnonymous().RequireRateLimiting("auth");
        group.MapPost(Refresh, "refresh").AllowAnonymous().RequireRateLimiting("auth");
        group.MapPost(Logout, "logout").RequireAuthorization();
        group.MapGet(Me, "me").RequireAuthorization();

        // ─── Email verification ───────────────────────────────────────────────
        group.MapPost(VerifyEmail, "verify-email").RequireAuthorization();
        group.MapPost(ResendVerification, "resend-verification").RequireAuthorization();
        group.MapPost(ChangePassword, "change-password").RequireAuthorization();
        group.MapPost(ChangeEmail, "change-email").RequireAuthorization();
        group.MapDelete(RevokeAllSessions, "sessions").RequireAuthorization();
        group.MapPost(ForgotPassword, "forgot-password").AllowAnonymous().RequireRateLimiting("auth");
        group.MapPost(ResetPassword, "reset-password").AllowAnonymous().RequireRateLimiting("auth");

        // ─── Push notification device token ──────────────────────────────────
        group.MapPost(RegisterDeviceToken, "device-token").RequireAuthorization();

        // ─── OAuth ───────────────────────────────────────────────────────────
        group.MapPost(OAuthGoogle, "oauth/google").AllowAnonymous().RequireRateLimiting("auth");
    }

    // ─── POST /api/Auth/register ─────────────────────────────────────────────

    [EndpointSummary("Register a new user")]
    [EndpointDescription("Creates a new account and UserProfile. Returns userId and email.")]
    [ProducesResponseType(typeof(RegisterResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public static async Task<Created<RegisterResult>> Register(
        [FromBody] RegisterCommand command,
        ISender sender)
    {
        RegisterResult result = await sender.Send(command);

        // 201 Created with Location header pointing to the new user's profile
        return TypedResults.Created($"/api/Users/{result.UserId}", result);
    }

    // ─── POST /api/Auth/login ────────────────────────────────────────────────

    [EndpointSummary("Login")]
    [EndpointDescription("Returns access token in body. Sets HttpOnly refresh_token cookie.")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public static async Task<Ok<AuthResponseDto>> Login(
        [FromBody] LoginCommand command,
        ISender sender,
        HttpResponse response)
    {
        LoginResult result = await sender.Send(command);

        SetRefreshTokenCookie(response, result.RefreshToken);

        // Never expose the raw refresh token in the body — it belongs in the cookie only.
        return TypedResults.Ok(new AuthResponseDto(result.AccessToken, result.ExpiresIn, result.UserId, result.Email));
    }

    // ─── POST /api/Auth/refresh ──────────────────────────────────────────────

    [EndpointSummary("Refresh access token")]
    [EndpointDescription(
        "Reads refresh token from HttpOnly cookie, issues new token pair (rotation). " +
        "Returns new access token in body and sets new refresh_token cookie.")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public static async Task<Ok<TokenResponseDto>> Refresh(
        ISender sender,
        HttpRequest request,
        HttpResponse response)
    {
        // Read refresh token from HttpOnly cookie (never from request body — that would
        // require JavaScript to send it, defeating the purpose of HttpOnly).
        string? rawRefreshToken = request.Cookies[RefreshTokenCookie];

        if (string.IsNullOrEmpty(rawRefreshToken))
        {
            throw new UnauthorizedAccessException("Refresh token cookie is missing.");
        }

        TokenResult result = await sender.Send(new RefreshTokenCommand(rawRefreshToken));

        SetRefreshTokenCookie(response, result.NewRefreshToken);

        return TypedResults.Ok(new TokenResponseDto(result.AccessToken, result.ExpiresIn));
    }

    // ─── POST /api/Auth/logout ───────────────────────────────────────────────

    [EndpointSummary("Logout")]
    [EndpointDescription("Clears the refresh token from the database and removes the HttpOnly cookie.")]
    public static async Task<NoContent> Logout(
        ISender sender,
        HttpResponse response)
    {
        await sender.Send(new LogoutCommand());

        // Delete the cookie by setting it with an expired MaxAge
        response.Cookies.Delete(RefreshTokenCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
        });

        return TypedResults.NoContent();
    }

    // ─── GET /api/Auth/me ────────────────────────────────────────────────────

    [EndpointSummary("Get current user")]
    [EndpointDescription("Returns the authenticated user's profile from the JWT claims.")]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public static async Task<Ok<CurrentUserDto>> Me(ISender sender)
    {
        CurrentUserDto dto = await sender.Send(new GetCurrentUserQuery());
        return TypedResults.Ok(dto);
    }

    // ─── POST /api/Auth/verify-email ─────────────────────────────────────────

    [EndpointSummary("Verify email address")]
    [EndpointDescription("Confirms the user's email using the token sent at registration.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public static async Task<NoContent> VerifyEmail(
        [FromBody] VerifyEmailCommand command,
        ISender sender)
    {
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Resend email verification")]
    [EndpointDescription("Generates a fresh email verification token for the authenticated user.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<NoContent> ResendVerification(ISender sender)
    {
        await sender.Send(new ResendEmailVerificationCommand());
        return TypedResults.NoContent();
    }

    [EndpointSummary("Change password")]
    [EndpointDescription("Changes the authenticated user's password.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public static async Task<NoContent> ChangePassword(
        [FromBody] ChangePasswordCommand command,
        ISender sender)
    {
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Change email")]
    [EndpointDescription("Changes the authenticated user's email address.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public static async Task<NoContent> ChangeEmail(
        [FromBody] ChangeEmailCommand command,
        ISender sender)
    {
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Revoke all sessions")]
    [EndpointDescription("Revokes the authenticated user's active refresh token sessions.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<NoContent> RevokeAllSessions(ISender sender)
    {
        await sender.Send(new RevokeAllSessionsCommand());
        return TypedResults.NoContent();
    }

    // ─── POST /api/Auth/forgot-password ──────────────────────────────────────

    [EndpointSummary("Request a password reset email")]
    [EndpointDescription(
        "Sends a password reset link to the provided email. " +
        "Always returns 200 regardless of whether the email exists (prevents enumeration).")]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public static async Task<Ok> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        ISender sender)
    {
        await sender.Send(command);
        return TypedResults.Ok();
    }

    // ─── POST /api/Auth/reset-password ───────────────────────────────────────

    [EndpointSummary("Reset password")]
    [EndpointDescription("Resets the password using the token from the reset email.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public static async Task<NoContent> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        ISender sender)
    {
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    // ─── POST /api/Auth/device-token ─────────────────────────────────────────

    [EndpointSummary("Register FCM push notification token")]
    [EndpointDescription(
        "Registers (or updates) the FCM push token for the current user's device. " +
        "Call this on app launch after the user logs in.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<NoContent> RegisterDeviceToken(
        [FromBody] RegisterDeviceTokenBody body,
        ISender sender)
    {
        await sender.Send(new RegisterDeviceTokenCommand(body.FcmToken, body.Platform));
        return TypedResults.NoContent();
    }

    // ─── POST /api/Auth/oauth/google ─────────────────────────────────────────

    [EndpointSummary("Login with Google OAuth")]
    [EndpointDescription(
        "Validates a Google ID token obtained by the client (via Google Sign-In SDK). " +
        "Finds or creates the user account, then returns the same JWT + refresh cookie as a normal login. " +
        "New OAuth accounts are automatically email-confirmed.")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public static async Task<Ok<AuthResponseDto>> OAuthGoogle(
        [FromBody] OAuthLoginCommand command,
        ISender sender,
        HttpResponse response)
    {
        LoginResult result = await sender.Send(command);

        SetRefreshTokenCookie(response, result.RefreshToken);

        return TypedResults.Ok(new AuthResponseDto(result.AccessToken, result.ExpiresIn, result.UserId, result.Email));
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the HttpOnly refresh token cookie with a 7-day expiry.
    /// Secure=true is required in production (HTTPS only).
    /// SameSite=Strict prevents CSRF attacks.
    /// </summary>
    private static void SetRefreshTokenCookie(HttpResponse response, string rawToken)
    {
        response.Cookies.Append(RefreshTokenCookie, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,              // Set to false only for HTTP dev testing
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(7),
            Path = "/api/Auth",         // Scope cookie to Auth endpoints only (minimize exposure)
        });
    }
}

/// <summary>Request body for the RegisterDeviceToken endpoint.</summary>
public record RegisterDeviceTokenBody(string FcmToken, DevicePlatform Platform);
