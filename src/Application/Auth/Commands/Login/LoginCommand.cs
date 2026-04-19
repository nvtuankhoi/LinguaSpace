using System.Security.Cryptography;
using System.Text;
using LinguaSpace.Application.Auth.DTOs;
using LinguaSpace.Application.Common.Interfaces;

namespace LinguaSpace.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private const int AccessTokenLifetimeSeconds = 900; // 15 minutes
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public LoginCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        TimeProvider timeProvider)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        string? userId = await _identityService.GetUserByEmailAsync(request.Email);

        if (userId is null || !await _identityService.CheckPasswordAsync(userId, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        IList<string> roles = await _identityService.GetRolesAsync(userId);

        string accessToken = _tokenService.GenerateAccessToken(userId, request.Email, roles);
        string rawRefreshToken = _tokenService.GenerateRefreshToken();

        string tokenHash = ComputeSha256(rawRefreshToken);
        DateTimeOffset expiresAt = _timeProvider.GetUtcNow().Add(RefreshTokenLifetime);

        await _identityService.UpdateRefreshTokenAsync(userId, tokenHash, expiresAt);

        return new LoginResult(accessToken, AccessTokenLifetimeSeconds, rawRefreshToken, userId, request.Email);
    }

    private static string ComputeSha256(string input)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
