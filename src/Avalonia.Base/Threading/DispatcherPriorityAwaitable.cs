using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Avalonia.Threading;

public class DispatcherPriorityAwaitable : INotifyCompletion
{
    private readonly Dispatcher _dispatcher;
    private protected readonly Task? Task;
    private readonly DispatcherPriority _priority;
    private bool _isCompleted;

    internal DispatcherPriorityAwaitable(Dispatcher dispatcher, Task task, DispatcherPriority priority)
    {
        _dispatcher = dispatcher;
        Task = task;
        _priority = priority;
    }

    internal DispatcherPriorityAwaitable(Dispatcher dispatcher, DispatcherPriority priority)
    {
        _dispatcher = dispatcher;
        Task = default;
        _priority = priority;
    }

    public void OnCompleted(Action continuation)
    {
        if (Task is { } task)
        {
            task.ContinueWith(_ => _dispatcher.Post(continuation, _priority));
        }
        else
        {
            _dispatcher.Post(() =>
            {
                continuation();
                _isCompleted = true;
            }, _priority);
        }

    }

    public bool IsCompleted => Task?.IsCompleted ?? _isCompleted;

    public void GetResult() => Task?.GetAwaiter().GetResult();

    public DispatcherPriorityAwaitable GetAwaiter() => this;
}

public sealed class DispatcherPriorityAwaitable<T> : DispatcherPriorityAwaitable
{
    internal DispatcherPriorityAwaitable(Dispatcher dispatcher, Task<T> task, DispatcherPriority priority) : base(
        dispatcher, task, priority)
    {
    }

    public new T GetResult() => ((Task<T>)Task!).GetAwaiter().GetResult();

    public new DispatcherPriorityAwaitable<T> GetAwaiter() => this;
}
