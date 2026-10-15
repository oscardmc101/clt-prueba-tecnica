using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Clt.Api.Infrastructure.Persistence;

// Used only by dotnet ef. Runtime operations use the registered IDbContextFactory.
public sealed class AppDbContextDesignFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var root = Directory.GetCurrentDirectory();
        if (Directory.Exists(Path.Combine(root, "src", "Clt.Api")))
            root = Path.Combine(root, "src", "Clt.Api");
        var configuration = new ConfigurationBuilder().SetBasePath(root)
            .AddJsonFile("appsettings.json", optional: true).AddEnvironmentVariables().Build();
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(PersistenceRegistration.ResolveConnectionString(configuration, root)).Options);
    }
}
