namespace LinguaSpace.Application.Auth.DTOs;

/// <summary>
/// Returned by LoginCommand. The raw RefreshToken is handed to the Web endpoint,
/// which stores it in an HttpOnly cookie — the handler itself has no access to HttpResponse.
/// </summary>
public record LoginResult(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    string UserId,
    string Email);
