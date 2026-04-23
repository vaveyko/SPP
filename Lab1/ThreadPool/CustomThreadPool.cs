using System;
using System.Collections.Generic;
using System.Threading;

namespace ThreadPool
{
    public static class ConsoleLogger
    {
        private static readonly object _globalLock = new object();

        public static void Log(string message, ConsoleColor color = ConsoleColor.White)
        {
            lock (_globalLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        public static void LogTestResult(string className, string methodName, string status, ConsoleColor color)
        {
            lock (_globalLock)
            {
                Console.Write($"[Поток {Thread.CurrentThread.ManagedThreadId:D2}] ");
                Console.Write($"{className.PadRight(20)} | {methodName.PadRight(25)} : ");
                Console.ForegroundColor = color;
                Console.WriteLine(status);
                Console.ResetColor();
            }
        }
    }


    public class ThreadEventArgs : EventArgs
    {
        public string ThreadName { get; }
        public int ManagedThreadId { get; }

        public ThreadEventArgs(string name, int id)
        {
            ThreadName = name;
            ManagedThreadId = id;
        }
    }

    public class CustomThreadPool : IDisposable
    {
        public event EventHandler<ThreadEventArgs> ThreadCreated;
        public event EventHandler<ThreadEventArgs> ThreadDestroyed;
        public event EventHandler<ThreadEventArgs> TaskStarted;
        public event EventHandler<ThreadEventArgs> TaskCompleted;

        private readonly Queue<Action> _taskQueue = new Queue<Action>();
        private readonly List<Thread> _workers = new List<Thread>();
        private readonly object _lockObj = new object();
        private bool _isShuttingDown = false;

        private readonly int _minThreads;
        private readonly int _maxThreads;
        private readonly TimeSpan _idleTimeout;

        private readonly TimeSpan _executionTimeout;
        private readonly Dictionary<Thread, DateTime> _busyThreads = new Dictionary<Thread, DateTime>();
        private readonly object _trackingLock = new object();
        private readonly Thread _monitorThread;

        private int _activeThreadsCount = 0;
        private int _waitingThreadsCount = 0;

        public CustomThreadPool(int minThreads, int maxThreads, TimeSpan idleTimeout, TimeSpan executionTimeout)
        {
            if (minThreads < 0 || maxThreads < minThreads)
                throw new ArgumentException("Неверные границы потоков.");

            _minThreads = minThreads;
            _maxThreads = maxThreads;
            _idleTimeout = idleTimeout;
            _executionTimeout = executionTimeout;

            for (int i = 0; i < minThreads; i++)
            {
                CreateWorker();
            }

            _monitorThread = new Thread(MonitorLoop) { IsBackground = true, Name = "Pool_Monitor" };
            _monitorThread.Start();
        }

        public void Enqueue(Action task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            lock (_lockObj)
            {
                if (_isShuttingDown)
                    throw new InvalidOperationException("Пул останавливается, новые таски не принимаются.");

                _taskQueue.Enqueue(task);

                if (_waitingThreadsCount == 0 && _activeThreadsCount < _maxThreads)
                {
                    CreateWorker();
                }

                Monitor.Pulse(_lockObj);
            }
        }

        private void CreateWorker()
        {
            Thread worker = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = $"Worker_{Guid.NewGuid().ToString().Substring(0, 4)}"
            };

            _workers.Add(worker);
            _activeThreadsCount++;

            ThreadCreated?.Invoke(this, new ThreadEventArgs(worker.Name, worker.ManagedThreadId));

            worker.Start();
        }

        private void WorkerLoop()
        {
            bool stayAlive = true;

            while (stayAlive)
            {
                Action task = null;

                lock (_lockObj)
                {
                    while (_taskQueue.Count == 0 && !_isShuttingDown)
                    {
                        _waitingThreadsCount++;
                        try
                        {
                            bool taskArrived = Monitor.Wait(_lockObj, _idleTimeout);

                            if (!taskArrived && _activeThreadsCount > _minThreads)
                            {
                                stayAlive = false;
                                break;
                            }
                        }
                        finally
                        {
                            _waitingThreadsCount--;
                        }
                    }

                    if (!stayAlive) break;

                    if (_isShuttingDown && _taskQueue.Count == 0)
                    {
                        stayAlive = false;
                        break;
                    }

                    task = _taskQueue.Dequeue();
                }

                if (task != null)
                {
                    lock (_trackingLock)
                    {
                        _busyThreads[Thread.CurrentThread] = DateTime.UtcNow;
                    }

                    try
                    {
                        TaskStarted?.Invoke(this, new ThreadEventArgs(Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId));

                        task.Invoke();

                        TaskCompleted?.Invoke(this, new ThreadEventArgs(Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId));
                    }
                    catch (Exception ex)
                    {
                        ConsoleLogger.Log($"[Thread Pool] Сбой в задаче: {ex.Message}", ConsoleColor.Red);
                    }
                    finally
                    {
                        lock (_trackingLock)
                        {
                            _busyThreads.Remove(Thread.CurrentThread);
                        }
                    }

                    lock (_lockObj)
                    {
                        if (!_workers.Contains(Thread.CurrentThread))
                        {
                            stayAlive = false;
                        }
                    }
                }
            }

            lock (_lockObj)
            {
                if (_workers.Contains(Thread.CurrentThread))
                {
                    _workers.Remove(Thread.CurrentThread);
                    _activeThreadsCount--;

                    ThreadDestroyed?.Invoke(this, new ThreadEventArgs(Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId));
                }
            }
        }

        private void MonitorLoop()
        {
            while (!_isShuttingDown)
            {
                Thread.Sleep(1000);

                List<Thread> hungThreads = new List<Thread>();
                DateTime now = DateTime.UtcNow;

                lock (_trackingLock)
                {
                    foreach (var kvp in _busyThreads)
                    {
                        if (now - kvp.Value > _executionTimeout)
                        {
                            hungThreads.Add(kvp.Key);
                        }
                    }

                    foreach (var th in hungThreads)
                    {
                        _busyThreads.Remove(th);
                    }
                }

                if (hungThreads.Count > 0)
                {
                    lock (_lockObj)
                    {
                        foreach (var th in hungThreads)
                        {
                            _workers.Remove(th);
                            _activeThreadsCount--;

                            ThreadDestroyed?.Invoke(this, new ThreadEventArgs(th.Name + " (ЗАВИС)", th.ManagedThreadId));

                            CreateWorker();
                        }
                    }
                }
            }
        }

        public int GetActiveThreads()
        {
            lock (_lockObj) { return _activeThreadsCount; }
        }

        public int GetWaitingThreads()
        {
            lock (_lockObj) { return _waitingThreadsCount; }
        }

        public int GetQueueLength()
        {
            lock (_lockObj) { return _taskQueue.Count; }
        }

        public void Dispose()
        {
            _isShuttingDown = true;

            lock (_lockObj)
            {
                Monitor.PulseAll(_lockObj);
            }
        }
    }
}