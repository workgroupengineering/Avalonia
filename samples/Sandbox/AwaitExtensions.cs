using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Sandbox
{
    public struct TaskSchedulerAwaiter : INotifyCompletion
    {
        private readonly TaskScheduler _capturedScheduler;
        private bool _isCompleted;

        public TaskSchedulerAwaiter(TaskScheduler taskScheduler)
        {
            _capturedScheduler = taskScheduler;
            _isCompleted = false;
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

    internal static class AwaitExtensions
    {
        public static TaskSchedulerAwaiter GetAwaiter(this TaskScheduler scheduler) =>
            new(scheduler);

        public static Avalonia.Threading.DispatcherPriorityAwaitable GetAwaiter(this Avalonia.Threading.Dispatcher dispatcher) =>
            dispatcher.AwaitWithPriority(Task.Delay(1), Avalonia.Threading.DispatcherPriority.Normal);
    }

}
