namespace LinguaSpace.Application.Common.Interfaces;

/// <summary>
/// JWT token generation and validation service.
/// Implemented by JwtTokenService in Infrastructure.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a signed JWT access token with the given user claims.
    /// Token is valid for 15 minutes.
    /// </summary>
    string GenerateAccessToken(string userId, string email, IList<string> roles);

    /// <summary>
    /// Generates a cryptographically random refresh token string.
    /// The caller is responsible for hashing and storing it.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates an access token's signature and expiry.
    /// Returns the userId (sub claim) if valid, null if invalid or expired.
    /// </summary>
    string? ValidateAccessToken(string token);
}
