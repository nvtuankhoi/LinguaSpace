using LinguaSpace.Application.Common.Models;

namespace LinguaSpace.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);

    Task<string?> GetEmailAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<(Result Result, string UserId)> CreateUserAsync(string email, string password);

    Task<string?> GetUserByEmailAsync(string email);

    Task<bool> CheckPasswordAsync(string userId, string password);

    Task UpdateRefreshTokenAsync(string userId, string? tokenHash, DateTimeOffset? expiresAt);

    Task<IList<string>> GetRolesAsync(string userId);

    Task<string?> GetUserIdByRefreshTokenHashAsync(string tokenHash, DateTimeOffset now);

    Task<Result> DeleteUserAsync(string userId);
}
