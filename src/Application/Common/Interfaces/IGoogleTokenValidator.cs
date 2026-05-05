namespace LinguaSpace.Application.Common.Interfaces;

/// <summary>Validated payload extracted from a Google ID token.</summary>
public record GoogleTokenPayload(
    string Subject,
    string Email,
    string? Name);

/// <summary>
/// Validates a Google ID token and returns the extracted claims.
/// Implemented in Infrastructure using Google.Apis.Auth.
/// Throws <see cref="UnauthorizedAccessException"/> if the token is invalid.
/// </summary>
public interface IGoogleTokenValidator
{
    Task<GoogleTokenPayload> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}
