using System.Security.Cryptography;
using System.Text;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LinguaSpace.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory,
        IAuthorizationService authorizationService)
    {
        _userManager = userManager;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _authorizationService = authorizationService;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId);

        return user?.UserName;
    }

    public async Task<string?> GetEmailAsync(string userId)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId);

        return user?.Email;
    }

    public async Task<(Result Result, string UserId)> CreateUserAsync(string email, string password)
    {
        ApplicationUser user = new()
        {
            UserName = email,
            Email = email,
        };

        IdentityResult result = await _userManager.CreateAsync(user, password);

        return (result.ToApplicationResult(), user.Id);
    }

    public async Task<string?> GetUserByEmailAsync(string email)
    {
        ApplicationUser? user = await _userManager.FindByEmailAsync(email);

        return user?.Id;
    }

    public async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return false;
        }

        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task UpdateRefreshTokenAsync(string userId, string? tokenHash, DateTimeOffset? expiresAt)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return;
        }

        user.RefreshTokenHash = tokenHash;
        user.RefreshTokenExpiresAt = expiresAt;

        await _userManager.UpdateAsync(user);
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId);

        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        System.Security.Claims.ClaimsPrincipal principal = await _userClaimsPrincipalFactory.CreateAsync(user);

        AuthorizationResult result = await _authorizationService.AuthorizeAsync(principal, policyName);

        return result.Succeeded;
    }

    public async Task<IList<string>> GetRolesAsync(string userId)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return [];
        }

        return await _userManager.GetRolesAsync(user);
    }

    public async Task<string?> GetUserIdByRefreshTokenHashAsync(string tokenHash, DateTimeOffset now)
    {
        ApplicationUser? user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshTokenHash == tokenHash && u.RefreshTokenExpiresAt > now);

        return user?.Id;
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId);

        return user != null ? await DeleteUserAsync(user) : Result.Success();
    }

    private async Task<Result> DeleteUserAsync(ApplicationUser user)
    {
        IdentityResult result = await _userManager.DeleteAsync(user);

        return result.ToApplicationResult();
    }

    // ─── Email verification ───────────────────────────────────────────────────

    public async Task<string> GenerateEmailVerificationTokenAsync(string userId)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<Result> VerifyEmailAsync(string userId, string token)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Result.Failure(["User not found."]);
        }

        IdentityResult result = await _userManager.ConfirmEmailAsync(user, token);

        return result.ToApplicationResult();
    }

    public async Task<bool> IsEmailConfirmedAsync(string userId)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId);

        return user?.EmailConfirmed ?? false;
    }

    // ─── Password reset ───────────────────────────────────────────────────────

    public async Task<(string Token, string UserId)?> GeneratePasswordResetTokenAsync(string email)
    {
        ApplicationUser? user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return null;
        }

        string token = await _userManager.GeneratePasswordResetTokenAsync(user);

        return (token, user.Id);
    }

    public async Task<Result> ResetPasswordAsync(string userId, string token, string newPassword)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Result.Failure(["User not found."]);
        }

        IdentityResult result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        return result.ToApplicationResult();
    }

    // ─── Moderation ───────────────────────────────────────────────────────────

    public async Task<Result> LockoutUserAsync(string userId, DateTimeOffset? until)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Result.Failure(["User not found."]);
        }

        await _userManager.SetLockoutEnabledAsync(user, true);

        IdentityResult result = await _userManager.SetLockoutEndDateAsync(
            user,
            until ?? DateTimeOffset.MaxValue);

        return result.ToApplicationResult();
    }

    public async Task<Result> UnlockUserAsync(string userId)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Result.Failure(["User not found."]);
        }

        IdentityResult result = await _userManager.SetLockoutEndDateAsync(user, null);

        return result.ToApplicationResult();
    }
}
