using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftGrab.Services;

/// <summary>
/// A simple work-stealing task scheduler that keeps a pool of threads busy
/// by redistributing queued work items.
/// </summary>
public sealed class WorkStealingScheduler : TaskScheduler, IDisposable
{
    private readonly LinkedList<Task> _tasks = new();
    private readonly Thread[] _threads;
    private bool _disposed;

    public WorkStealingScheduler(int threadCount = 0)
    {
        int count = threadCount > 0 ? threadCount : Environment.ProcessorCount;
        _threads = new Thread[count];
        for (int i = 0; i < count; i++)
        {
            _threads[i] = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = $"SwiftGrab-Worker-{i}"
            };
            _threads[i].Start();
        }
    }

    protected override IEnumerable<Task>? GetScheduledTasks()
    {
        lock (_tasks) return new List<Task>(_tasks);
    }

    protected override void QueueTask(Task task)
    {
        lock (_tasks)
        {
            _tasks.AddLast(task);
            Monitor.PulseAll(_tasks);
        }
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;

    private void WorkerLoop()
    {
        while (true)
        {
            Task? task;
            lock (_tasks)
            {
                while (_tasks.Count == 0 && !_disposed)
                    Monitor.Wait(_tasks);

                if (_disposed) return;
                if (_tasks.First == null) continue;
                task = _tasks.First.Value;
                _tasks.RemoveFirst();
            }
            TryExecuteTask(task);
        }
    }

    public void Dispose()
    {
        lock (_tasks)
        {
            _disposed = true;
            Monitor.PulseAll(_tasks);
        }
    }
}
