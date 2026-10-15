using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Infrastructure.Persistence;

public static class PersistenceRegistration
{
    public static IServiceCollection AddPersistence(this IServiceCollection services,
        IConfiguration configuration, string contentRoot)
    {
        var connectionString = ResolveConnectionString(configuration, contentRoot);
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));
        return services;
    }

    internal static string ResolveConnectionString(IConfiguration configuration, string contentRoot)
    {
        var connection = new SqliteConnectionStringBuilder(
            configuration.GetConnectionString("DefaultConnection") ?? "Data Source=clt.db")
        {
            ForeignKeys = true,
            DefaultTimeout = 10
        };
        if (!Path.IsPathRooted(connection.DataSource) && connection.DataSource != ":memory:")
            connection.DataSource = Path.Combine(contentRoot, connection.DataSource);
        return connection.ToString();
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        var factory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    }
}
