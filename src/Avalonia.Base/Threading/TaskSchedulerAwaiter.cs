using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Avalonia.Threading;

public record struct TaskSchedulerAwaiter : INotifyCompletion
{
    private readonly TaskScheduler _capturedScheduler;
    private bool _isCompleted;

    internal TaskSchedulerAwaiter(TaskScheduler taskScheduler)
    {
        _capturedScheduler = taskScheduler;
    }

    public TaskSchedulerAwaiter GetAwaiter() => this;

    public bool IsCompleted => _isCompleted;

    public void GetResult()
    {
    }

    public void OnCompleted(Action continuation)
    {
        var self = this;
        Task.Factory.StartNew(continuation, default,
            TaskCreationOptions.None, self._capturedScheduler)
            .ContinueWith(t => self._isCompleted = true);
    }
}
