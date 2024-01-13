#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Sandbox
{
    internal sealed class StaTaskScheduler : TaskScheduler, IDisposable
    {
        private readonly List<Thread> _threads;

        private readonly BlockingCollection<Task> _tasks;
        private static StaTaskScheduler? _DefaultScheduler;

        public StaTaskScheduler(int numberOfThreads)
        {
            if (numberOfThreads < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfThreads));
            }

            _tasks = new BlockingCollection<Task>();

            _threads = Enumerable.Range(0, numberOfThreads).Select(static (i, instance) =>
            {
                var thread = new Thread(() =>
                {
                    foreach (var t in instance._tasks.GetConsumingEnumerable())
                    {
                        instance.TryExecuteTask(t);
                    }
                });

                thread.IsBackground = true;

                thread.SetApartmentState(ApartmentState.STA);
                return thread;

            }, this).ToList();

            _threads.ForEach(t => t.Start());

        }

        public static new StaTaskScheduler Default =>
            _DefaultScheduler ??= new StaTaskScheduler(1);

        protected override IEnumerable<Task> GetScheduledTasks() =>
            _tasks.ToArray();

        protected override void QueueTask(Task task) =>
            _tasks.Add(task);

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) =>
            Thread.CurrentThread.GetApartmentState() == ApartmentState.STA
                && TryExecuteTask(task);

        public override int MaximumConcurrencyLevel => _threads.Count;

        public void Dispose()
        {
            _tasks.CompleteAdding();

            foreach (var thread in _threads)
                thread.Join();

            _tasks.Dispose();
        }
    }
}
