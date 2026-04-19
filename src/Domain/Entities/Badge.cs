namespace LinguaSpace.Domain.Entities;

/// <summary>
/// Master data for badges. Seeded from ApplicationDbContextInitialiser.
/// </summary>
public class Badge : BaseEntity
{
    /// <summary>Unique code identifier (e.g., "FIRST_LOGIN", "STREAK_7").</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Human-readable condition description (e.g., "Log in 7 days in a row").</summary>
    public string? Condition { get; set; }

    public string? IconUrl { get; set; }
}
