using Clt.Api.Api.Errors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Infrastructure.Persistence;

public static class DatabaseErrors
{
    public static async Task SaveAsync(AppDbContext db, string duplicateMessage, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException
               { SqliteExtendedErrorCode: 2067 })
        {
            throw ApiException.Conflict(duplicateMessage);
        }
    }
}
