using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ThreadPool;

namespace test_run
{
    public class LoadSimulator
    {
        private readonly CustomThreadPool _pool;
        private readonly List<Action> _baseTasks;
        private readonly int _maxTasks;

        public LoadSimulator(CustomThreadPool pool, List<Action> baseTasks, int totalTasks = 50)
        {
            _pool = pool;
            _baseTasks = baseTasks;
            _maxTasks = totalTasks;

            _pool.ThreadCreated += OnThreadCreated;
            _pool.ThreadDestroyed += OnThreadDestroyed;
            _pool.TaskStarted += OnTaskStarted;
            _pool.TaskCompleted += OnTaskCompleted;
        }

        private void OnThreadCreated(object sender, ThreadEventArgs e)
        {
            ConsoleLogger.Log($"[EVENT] Пул СОЗДАЛ поток: {e.ThreadName} (ID: {e.ManagedThreadId})", ConsoleColor.Green);
        }

        private void OnThreadDestroyed(object sender, ThreadEventArgs e)
        {
            ConsoleLogger.Log($"[EVENT] Пул УДАЛИЛ поток: {e.ThreadName} (ID: {e.ManagedThreadId} | Всего потоков в пуле: {_pool.GetActiveThreads()})", ConsoleColor.DarkRed);
        }

        private void OnTaskStarted(object sender, ThreadEventArgs e)
        {
            string stats = $"(Всего потоков в пуле: {_pool.GetActiveThreads()} | Свободно: {_pool.GetWaitingThreads()} | В очереди: {_pool.GetQueueLength()})";
            ConsoleLogger.Log($"  [->] Поток {e.ManagedThreadId:D2} взял задачу. {stats}", ConsoleColor.Yellow);
        }

        private void OnTaskCompleted(object sender, ThreadEventArgs e)
        {
            ConsoleLogger.Log($"  [<-] Поток {e.ManagedThreadId:D2} завершил задачу.", ConsoleColor.DarkGray);
        }

        public void Run()
        {
            if (_baseTasks == null || _baseTasks.Count == 0)
            {
                ConsoleLogger.Log("Нет тестов для симуляции!", ConsoleColor.Red);
                return;
            }

            var loadQueue = new List<Action>();
            while (loadQueue.Count < _maxTasks)
            {
                loadQueue.AddRange(_baseTasks);
            }
            loadQueue = loadQueue.Take(_maxTasks).ToList();

            ConsoleLogger.Log("\n=======================================================", ConsoleColor.White);
            ConsoleLogger.Log($"    СТАРТ СИМУЛЯЦИИ НАГРУЗКИ ({_maxTasks} ЗАПУСКОВ ТЕСТОВ)      ", ConsoleColor.White);
            ConsoleLogger.Log("=======================================================\n", ConsoleColor.White);

            CountdownEvent countdown = new CountdownEvent(loadQueue.Count);

            Action WrapTask(Action originalTask) => () =>
            {
                try { originalTask.Invoke(); }
                finally { countdown.Signal(); }
            };

            Stopwatch sw = Stopwatch.StartNew();

            ConsoleLogger.Log("\n---> Подача 1 теста <---", ConsoleColor.Cyan);
            _pool.Enqueue(WrapTask(loadQueue[0]));
            Thread.Sleep(1000);

            ConsoleLogger.Log("\n---> Подача 30 тестов одновременно <---", ConsoleColor.Cyan);
            for (int i = 1; i <= 31; i++)
            {
                if (i < loadQueue.Count) _pool.Enqueue(WrapTask(loadQueue[i]));
                Thread.Sleep(10);
            }

            Thread.Sleep(4000);

            ConsoleLogger.Log("\n---> Подача оставшихся тестов <---", ConsoleColor.Cyan);
            for (int i = 32; i < _maxTasks; i++)
            {
                if (i < loadQueue.Count) _pool.Enqueue(WrapTask(loadQueue[i]));
                Thread.Sleep(50);
            }
            Thread.Sleep(5000);

            countdown.Wait();
            sw.Stop();

            _pool.ThreadCreated -= OnThreadCreated;
            _pool.ThreadDestroyed -= OnThreadDestroyed;
            _pool.TaskStarted -= OnTaskStarted;
            _pool.TaskCompleted -= OnTaskCompleted;

            ConsoleLogger.Log($"\n=======================================================", ConsoleColor.White);
            ConsoleLogger.Log($"  СИМУЛЯЦИЯ ЗАВЕРШЕНА ЗА: {sw.ElapsedMilliseconds} мс", ConsoleColor.White);
            ConsoleLogger.Log("=======================================================\n", ConsoleColor.White);
        }
    }
}