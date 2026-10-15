using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Clt.Api.Tests.Support;

// A rendezvous before DB execution proves two operations can be in flight together.
// Sequential code cannot release this barrier and fails within a bounded timeout.
internal sealed class ConcurrencyProbe(string commandMarker) : DbCommandInterceptor
{
    private readonly TaskCompletionSource _pair = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _entered;
    public bool Enabled { get; set; }
    public ConcurrentDictionary<Guid, byte> ContextIds { get; } = new();
    public bool PairObserved => _pair.Task.IsCompletedSuccessfully;

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (!Enabled || !command.CommandText.Contains(commandMarker, StringComparison.Ordinal)) return result;
        ContextIds.TryAdd(eventData.Context!.ContextId.InstanceId, 0);
        if (Interlocked.Increment(ref _entered) >= 2) _pair.TrySetResult();
        await _pair.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        return result;
    }
}
