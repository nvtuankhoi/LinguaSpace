using Google.Apis.Auth;
using LinguaSpace.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LinguaSpace.Infrastructure.Auth;

/// <summary>
/// Validates Google ID tokens using the Google.Apis.Auth library.
/// Audience validation is performed when Google:ClientId is configured in appsettings.
/// </summary>
public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly string? _clientId;

    public GoogleTokenValidator(IConfiguration configuration)
    {
        _clientId = configuration["Google:ClientId"];
    }

    public async Task<GoogleTokenPayload> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        try
        {
            GoogleJsonWebSignature.ValidationSettings settings = new();

            if (!string.IsNullOrWhiteSpace(_clientId))
            {
                settings.Audience = [_clientId];
            }

            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            string email = payload.Email
                ?? throw new UnauthorizedAccessException("Google token does not contain an email claim.");

            return new GoogleTokenPayload(
                Subject: payload.Subject,
                Email: email,
                Name: payload.Name);
        }
        catch (InvalidJwtException ex)
        {
            throw new UnauthorizedAccessException($"Invalid Google ID token: {ex.Message}");
        }
    }
}
