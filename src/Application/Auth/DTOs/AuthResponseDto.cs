namespace LinguaSpace.Application.Auth.DTOs;

/// <summary>Returned in the response body by Login and OAuth endpoints.</summary>
public record AuthResponseDto(string AccessToken, int ExpiresIn, string UserId, string Email);

/// <summary>Returned in the response body by Refresh endpoint.</summary>
public record TokenResponseDto(string AccessToken, int ExpiresIn);
