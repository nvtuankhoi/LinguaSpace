namespace LinguaSpace.Application.Common.Models;

// Generic lookup DTO for simple key-value dropdowns.
// AutoMapper mappings will be added per-entity in their respective feature mapping profiles.
public class LookupDto
{
    public int Id { get; init; }

    public string? Title { get; init; }
}

