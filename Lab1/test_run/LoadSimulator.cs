using System.Diagnostics;
using ThreadPool;

namespace test_run
{
    public class LoadSimulator
    {
        private readonly CustomThreadPool _pool;
        private readonly List<Action> _baseTasks;
        private readonly int _maxTasks;
        private readonly object _consoleLock = new object();

        public LoadSimulator(CustomThreadPool pool, List<Action> baseTasks, int totalTasks = 50)
        {
            _pool = pool;
            _baseTasks = baseTasks;
            _maxTasks = totalTasks;
        }

        public void Run()
        {
            if (_baseTasks == null || _baseTasks.Count == 0)
            {
                Console.WriteLine("Нет тестов для симуляции!");
                return;
            }

            var loadQueue = new List<Action>();
            while (loadQueue.Count < _maxTasks)
            {
                loadQueue.AddRange(_baseTasks);
            }
            loadQueue = loadQueue.Take(_maxTasks).ToList();

            Console.WriteLine("\n=======================================================");
            Console.WriteLine($"    СТАРТ СИМУЛЯЦИИ НАГРУЗКИ ({_maxTasks} ЗАПУСКОВ ТЕСТОВ)      ");
            Console.WriteLine("=======================================================\n");

            CountdownEvent countdown = new CountdownEvent(loadQueue.Count + 1);

            Action WrapTask(Action originalTask) => () =>
            {
                try { originalTask.Invoke(); }
                finally { countdown.Signal(); }
            };

            bool isSimulating = true;
            Thread monitorThread = new Thread(() =>
            {
                while (isSimulating)
                {
                    lock (_consoleLock) {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"\n[МОНИТОР] Активных потоков: {_pool.GetActiveThreads()} | Свободных: {_pool.GetWaitingThreads()} | Задач в очереди: {_pool.GetQueueLength()}");
                        Console.ResetColor();
                        Thread.Sleep(500); // Обновление мониторинга
                    }
                }
            })
            { IsBackground = true, Name = "Simulation_Monitor" };

            monitorThread.Start();
            Stopwatch sw = Stopwatch.StartNew();

            // Сценарии
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n---> ЭТАП 1: Подача 1 теста <---");
            Console.ResetColor();

            _pool.Enqueue(WrapTask(loadQueue[0]));

            // Пауза для сжатия
            Thread.Sleep(1000);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n---> ЭТАП 2: Пиковая нагрузка (30 тестов одновременно) <---");
            Console.ResetColor();

            for (int i = 1; i <= 31; i++)
            {
                _pool.Enqueue(WrapTask(loadQueue[i]));
                Thread.Sleep(10);
            }

            // Пауза для сжатия
            Thread.Sleep(4000);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n---> ЭТАП 3: Подача оставшихся тестов <---");
            Console.ResetColor();

            for (int i = 31; i < _maxTasks; i++)
            {
                _pool.Enqueue(WrapTask(loadQueue[i]));
                Thread.Sleep(50);
            }

            // для сжатия
            Thread.Sleep(5000);
            countdown.Wait();
            sw.Stop();

            isSimulating = false;

            Console.WriteLine($"\n=======================================================");
            Console.WriteLine($"  СИМУЛЯЦИЯ ЗАВЕРШЕНА ЗА: {sw.ElapsedMilliseconds} мс");
            Console.WriteLine("=======================================================\n");
        }
    }
}
