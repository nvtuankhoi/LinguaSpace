using System.Security.Cryptography;
using System.Text;
using LinguaSpace.Application.Auth.DTOs;
using LinguaSpace.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LinguaSpace.Application.Auth.Commands.OAuthLogin;

public record OAuthLoginCommand(string IdToken) : IRequest<LoginResult>;

public class OAuthLoginCommandValidator : AbstractValidator<OAuthLoginCommand>
{
    public OAuthLoginCommandValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty();
    }
}

public class OAuthLoginCommandHandler : IRequestHandler<OAuthLoginCommand, LoginResult>
{
    private const int AccessTokenLifetimeSeconds = 900; // 15 minutes
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;
    private readonly IGoogleTokenValidator _googleTokenValidator;

    public OAuthLoginCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        TimeProvider timeProvider,
        IGoogleTokenValidator googleTokenValidator)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
        _googleTokenValidator = googleTokenValidator;
    }

    public async Task<LoginResult> Handle(OAuthLoginCommand request, CancellationToken cancellationToken)
    {
        GoogleTokenPayload payload = await _googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken);

        (string userId, string confirmedEmail) = await _identityService.FindOrCreateExternalUserAsync(
            payload.Email,
            loginProvider: "Google",
            providerKey: payload.Subject,
            displayName: payload.Name);

        IList<string> roles = await _identityService.GetRolesAsync(userId);

        string accessToken = _tokenService.GenerateAccessToken(userId, confirmedEmail, roles);
        string rawRefreshToken = _tokenService.GenerateRefreshToken();

        string tokenHash = ComputeSha256(rawRefreshToken);
        DateTimeOffset expiresAt = _timeProvider.GetUtcNow().Add(RefreshTokenLifetime);

        await _identityService.UpdateRefreshTokenAsync(userId, tokenHash, expiresAt);

        return new LoginResult(accessToken, AccessTokenLifetimeSeconds, rawRefreshToken, userId, confirmedEmail);
    }

    private static string ComputeSha256(string input)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
