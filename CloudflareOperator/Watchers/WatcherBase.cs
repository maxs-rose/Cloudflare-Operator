using Microsoft.Extensions.Hosting;

namespace CloudflareOperator.Watchers;

public abstract class WatcherBase : IHostedService
{
    private readonly CancellationTokenSource _cts = new();
    private Task _watcher = null!;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _watcher = WatchLoop(_cts.Token);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();

        return _watcher.WaitAsync(cancellationToken);
    }

    private async Task WatchLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
            await Watch(cancellationToken);

        Console.WriteLine("Task cancelled");
    }

    protected abstract Task Watch(CancellationToken cancellationToken);
}