using System.Threading.Tasks;
using Avalonia.Threading;

namespace Avalonia;

public static class AwaitExtensions
{
    public static TaskSchedulerAwaiter GetAwaiter(this TaskScheduler scheduler) =>
        new(scheduler);

    public static DispatcherPriorityAwaitable GetAwaiter(this Dispatcher dispatcher) =>
        dispatcher.AwaitWithPriority(DispatcherPriority.Normal);
}
