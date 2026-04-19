using System.Security.Cryptography;
using System.Text;
using LinguaSpace.Application.Auth.DTOs;
using LinguaSpace.Application.Common.Interfaces;

namespace LinguaSpace.Application.Auth.Commands.RefreshToken;

/// <summary>
/// The raw refresh token is read from the HttpOnly cookie at the endpoint layer
/// and passed into this command. The handler never touches HttpRequest/Response.
/// </summary>
public record RefreshTokenCommand(string RawRefreshToken) : IRequest<TokenResult>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResult>
{
    private const int AccessTokenLifetimeSeconds = 900;
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        TimeProvider timeProvider)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
    }

    public async Task<TokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        string tokenHash = ComputeSha256(request.RawRefreshToken);

        string? userId = await _identityService.GetUserIdByRefreshTokenHashAsync(tokenHash, now);

        if (userId is null)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        string email = await _identityService.GetEmailAsync(userId) ?? string.Empty;
        IList<string> roles = await _identityService.GetRolesAsync(userId);

        // Token rotation: issue new pair, invalidate old token
        string newAccessToken = _tokenService.GenerateAccessToken(userId, email, roles);
        string newRawRefreshToken = _tokenService.GenerateRefreshToken();
        string newTokenHash = ComputeSha256(newRawRefreshToken);
        DateTimeOffset newExpiresAt = now.Add(RefreshTokenLifetime);

        await _identityService.UpdateRefreshTokenAsync(userId, newTokenHash, newExpiresAt);

        return new TokenResult(newAccessToken, AccessTokenLifetimeSeconds, newRawRefreshToken);
    }

    private static string ComputeSha256(string input)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
