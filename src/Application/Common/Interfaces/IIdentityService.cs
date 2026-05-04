using LinguaSpace.Application.Common.Models;

namespace LinguaSpace.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);

    Task<string?> GetEmailAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<(Result Result, string UserId)> CreateUserAsync(string email, string password);

    Task<string?> GetUserByEmailAsync(string email);

    Task<bool> CheckPasswordAsync(string userId, string password);

    Task UpdateRefreshTokenAsync(string userId, string? tokenHash, DateTimeOffset? expiresAt);

    Task<IList<string>> GetRolesAsync(string userId);

    Task<string?> GetUserIdByRefreshTokenHashAsync(string tokenHash, DateTimeOffset now);

    Task<Result> DeleteUserAsync(string userId);

    // ─── Moderation ───────────────────────────────────────────────────────────

    /// <summary>Locks out a user account. Pass <c>null</c> for <paramref name="until"/> for a permanent ban.</summary>
    Task<Result> LockoutUserAsync(string userId, DateTimeOffset? until);

    /// <summary>Removes lockout, restoring normal access.</summary>
    Task<Result> UnlockUserAsync(string userId);

    // ─── Email verification ───────────────────────────────────────────────────

    /// <summary>Generates an ASP.NET Identity email confirmation token for the user.</summary>
    Task<string> GenerateEmailVerificationTokenAsync(string userId);

    /// <summary>Confirms the user's email with the provided token. Returns failure if token invalid.</summary>
    Task<Result> VerifyEmailAsync(string userId, string token);

    /// <summary>Returns true if the user's email is confirmed.</summary>
    Task<bool> IsEmailConfirmedAsync(string userId);

    // ─── Password reset ───────────────────────────────────────────────────────

    /// <summary>
    /// Generates a password reset token. Returns (token, userId) if email exists, null userId if not found.
    /// Caller decides whether to reveal "not found" to the client (don't — return 200 either way).
    /// </summary>
    Task<(string Token, string UserId)?> GeneratePasswordResetTokenAsync(string email);

    /// <summary>Resets password using a previously generated reset token.</summary>
    Task<Result> ResetPasswordAsync(string userId, string token, string newPassword);
}
