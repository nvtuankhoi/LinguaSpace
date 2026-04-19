namespace LinguaSpace.Application.Auth.DTOs;

/// <summary>
/// Returned by RefreshTokenCommand. NewRefreshToken is the raw token that the endpoint
/// must rotate into the HttpOnly cookie (token rotation strategy).
/// </summary>
public record TokenResult(string AccessToken, int ExpiresIn, string NewRefreshToken);
