using LinguaSpace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LinguaSpace.Infrastructure;

/// <summary>
/// Used by EF Core tools (dotnet ef migrations) at design time.
/// Provides a DbContext without requiring the full ASP.NET host to start
/// (avoids needing Redis, JWT secrets, etc. during migration scaffolding).
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ApplicationDbContext> optionsBuilder =
            new DbContextOptionsBuilder<ApplicationDbContext>();

        // Use a dummy connection string for design-time tooling.
        // The real connection string comes from Aspire at runtime.
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=LinguaSpace;Username=postgres;Password=postgres");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
