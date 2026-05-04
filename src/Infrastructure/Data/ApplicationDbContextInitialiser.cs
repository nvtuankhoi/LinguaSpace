using LinguaSpace.Domain.Constants;
using LinguaSpace.Domain.Entities;
using LinguaSpace.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinguaSpace.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // Default roles
        var administratorRole = new IdentityRole(Roles.Administrator);

        if (_roleManager.Roles.All(r => r.Name != administratorRole.Name))
        {
            await _roleManager.CreateAsync(administratorRole);
        }

        // Default admin user
        var administrator = new ApplicationUser { UserName = "administrator@localhost", Email = "administrator@localhost" };

        if (_userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            await _userManager.CreateAsync(administrator, "Administrator1!");
            if (!string.IsNullOrWhiteSpace(administratorRole.Name))
            {
                await _userManager.AddToRolesAsync(administrator, new[] { administratorRole.Name });
            }
        }

        // Seed badge master data (idempotent — only insert if code doesn't exist)
        await SeedBadgesAsync();
    }

    private async Task SeedBadgesAsync()
    {
        (string Code, string Name, string Description, string Condition)[] badgeData =
        [
            ("FIRST_ROOM",  "First Steps",       "Joined your first room",              "Join any room for the first time"),
            ("STREAK_3",    "On a Roll",          "3-day activity streak",               "Maintain a 3-day streak"),
            ("STREAK_7",    "Dedicated Learner",  "7-day activity streak",               "Maintain a 7-day streak"),
            ("STREAK_30",   "Language Champion",  "30-day activity streak",              "Maintain a 30-day streak"),
            ("XP_100",      "Getting Started",    "Earned 100 XP total",                 "Reach 100 total XP"),
            ("XP_500",      "Enthusiast",         "Earned 500 XP total",                 "Reach 500 total XP"),
            ("XP_1000",     "Expert",             "Earned 1000 XP total",                "Reach 1000 total XP"),
        ];

        HashSet<string> existingCodes = (await _context.Badges.Select(b => b.Code).ToListAsync()).ToHashSet();

        foreach ((string code, string name, string description, string condition) in badgeData)
        {
            if (!existingCodes.Contains(code))
            {
                _context.Badges.Add(new Badge
                {
                    Code = code,
                    Name = name,
                    Description = description,
                    Condition = condition,
                });
            }
        }

        await _context.SaveChangesAsync();
    }
}

