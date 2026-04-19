using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using LinguaSpace.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LinguaSpace.Infrastructure.Auth;

/// <summary>
/// Implements ITokenService using standard JWT (HMAC-SHA256).
///
/// Configuration required in appsettings.json (or environment variables):
/// <code>
/// "Jwt": {
///   "Key": "your-secret-key-at-least-32-chars",
///   "Issuer": "LinguaSpace",
///   "Audience": "LinguaSpaceApi"
/// }
/// </code>
/// </summary>
public class JwtTokenService : ITokenService
{
    private const int AccessTokenLifetimeMinutes = 15;

    private readonly SymmetricSecurityKey _signingKey;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenService(IConfiguration configuration)
    {
        string key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        _issuer = configuration["Jwt:Issuer"] ?? "LinguaSpace";
        _audience = configuration["Jwt:Audience"] ?? "LinguaSpaceApi";

        // SymmetricSecurityKey holds the signing key bytes. HMAC-SHA256 requires >= 256 bits (32 bytes).
        _signingKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key));
    }

    public string GenerateAccessToken(string userId, string email, IList<string> roles)
    {
        // Claims are the payload of the JWT — structured data about the user.
        List<Claim> claims =
        [
            // sub = subject, the standard claim for user identity.
            // ClaimTypes.NameIdentifier maps to "sub" in JWT.
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),

            // jti = JWT ID, unique per token. Useful for token revocation if needed later.
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];

        // Add each role as a separate Claim. ClaimTypes.Role maps to the standard "role" claim.
        // ASP.NET's [Authorize(Roles = "Admin")] and CurrentUser.Roles both read ClaimTypes.Role.
        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        SigningCredentials credentials = new(_signingKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(AccessTokenLifetimeMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // 32 cryptographically random bytes → 256 bits of entropy → URL-safe Base64 string.
        // This is NOT a JWT — it's a random opaque token stored hashed in the DB.
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    public string? ValidateAccessToken(string token)
    {
        JwtSecurityTokenHandler handler = new();

        TokenValidationParameters parameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero, // No tolerance — 15 min means exactly 15 min
        };

        try
        {
            ClaimsPrincipal principal = handler.ValidateToken(token, parameters, out _);
            return principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        }
        catch
        {
            return null; // Invalid signature, expired, tampered — all return null
        }
    }
}
