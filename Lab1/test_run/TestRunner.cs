using application_test;
using lab1_test_framework;
using System.Diagnostics;
using System.Reflection;
using test_run;
using ThreadPool;


namespace test_runner
{
    public class TestWorkItem
    {
        public MethodInfo Method { get; set; }
        public Type ClassType { get; set; }
        public MethodInfo StartMethod { get; set; }
        public MethodInfo FinishMethod { get; set; }
        public TestMethodAttribute Config { get; set; }
    }
    class Program
    {
        private static object consoleLock = new object();
        private static int _minPool = 2;
        private static int _maxPool = 10;
        private static int _waitTime = 2;
        private static int _execTime = 5;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== ЗАПУСК ЛАБОРАТОРНОЙ РАБОТЫ ===\n");


            Assembly assembly = Assembly.LoadFrom("application_test");
            Type[] allTypes = assembly.GetTypes();
            List<Type> testClasses = new();
            foreach (Type testType in allTypes)
            {
                var classAttr = testType.GetCustomAttribute<TestClassAttribute>();
                if (classAttr == null) continue;

                Console.WriteLine($"\n>>>> НАЙДЕН ТЕСТОВЫЙ КЛАСС: {testType.Name} <<<<\n");
                testClasses.Add(testType);
            }

            var regularTests = new List<TestWorkItem>();
            var sharedGroups = new List<IGrouping<int, MethodInfo>>();

            foreach (var type in testClasses)
            {
                var methods = type.GetMethods();
                var start = methods.FirstOrDefault(m => m.GetCustomAttribute<StartAttribute>() != null);
                var end = methods.FirstOrDefault(m => m.GetCustomAttribute<EndAttribute>() != null);

                foreach (var m in methods)
                {
                    if (m.GetCustomAttribute<SkipAttribute>() != null) continue;

                    var testAttr = m.GetCustomAttribute<TestMethodAttribute>();
                    var sharedAttr = m.GetCustomAttribute<SharedContextAttribute>();

                    if (testAttr != null)
                    {
                        regularTests.Add(new TestWorkItem
                        {
                            Method = m,
                            ClassType = type,
                            StartMethod = start,
                            FinishMethod = end,
                            Config = testAttr
                        });
                    }
                }

                // Shared Context
                var typeShared = methods
                    .Where(m => m.GetCustomAttribute<SharedContextAttribute>() != null)
                    .GroupBy(m => m.GetCustomAttribute<SharedContextAttribute>().ContextId);
                sharedGroups.AddRange(typeShared);
            }

            Stopwatch sw = Stopwatch.StartNew();

            List<Action> allTasks = new List<Action>();

            foreach (var test in regularTests)
            {
                var currentTest = test;
                allTasks.Add(() => ExecuteTest(currentTest).GetAwaiter().GetResult());
            }

            foreach (var group in sharedGroups)
            {
                var currentGroup = group;
                allTasks.Add(() => ExecuteSharedGroup(currentGroup).GetAwaiter().GetResult());
            }

            using (var customPool = new CustomThreadPool(
                minThreads: _minPool,
                maxThreads: _maxPool,
                idleTimeout: TimeSpan.FromSeconds(_waitTime),
                executionTimeout: TimeSpan.FromSeconds(_execTime)))
            {
                var simulator = new LoadSimulator(customPool, allTasks, 50);
                simulator.Run();
            }
                sw.Stop();

            Console.WriteLine($"\n========================================");
            Console.WriteLine($"Все тесты завершены за: {sw.ElapsedMilliseconds} мс");
            Console.WriteLine($"Минимальный параллелизм: {_minPool}");
            Console.WriteLine($"Максимальный параллелизм: {_maxPool}");
            Console.WriteLine("========================================");
        }

        private static async Task ExecuteTest(TestWorkItem work)
        {
            var paramAttrs = work.Method.GetCustomAttributes<ParameterAttribute>().ToArray();

            object[][] allParams = paramAttrs.Length > 0
                ? paramAttrs.Select(p => p.parameters).ToArray()
                : new object[][] { null };

            foreach (var parameters in allParams)
            {
                object instance = Activator.CreateInstance(work.ClassType);
                var timeoutAttr = work.Method.GetCustomAttribute<TimeoutAttribute>();

                try
                {
                    work.StartMethod?.Invoke(instance, new object[] { work.Config.DayCaloriesNorm });

                    Task? testTask = (work.Method.ReturnType == typeof(Task))
                        ? (Task)work.Method.Invoke(instance, parameters)
                        : Task.Run(() => work.Method.Invoke(instance, parameters));

                    if (timeoutAttr != null)
                    {
                        if (await Task.WhenAny(testTask, Task.Delay(timeoutAttr.Milliseconds)) != testTask)
                            throw new Exception($"TimeOut: {timeoutAttr.Milliseconds}мс");
                    }

                    await testTask;
                    work.FinishMethod?.Invoke(instance, null);
                    LogResult(work.ClassType.Name, work.Method.Name, "ПРОЙДЕН", ConsoleColor.Green);
                }
                catch (Exception ex)
                {
                    var msg = (ex.InnerException ?? ex).Message;
                    LogResult(work.ClassType.Name, work.Method.Name, $"ПРОВАЛЕН: {msg}", ConsoleColor.Red);
                }
            }
        }

        private static async Task ExecuteSharedGroup(IGrouping<int, MethodInfo> group)
        {
            var firstMethod = group.First();
            var classType = firstMethod.DeclaringType;
            object instance = Activator.CreateInstance(classType);

            var startMethod = classType.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<StartAttribute>() != null);
            var finishMethod = classType.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<EndAttribute>() != null);

            var contextParam = group
                .Select(m => m.GetCustomAttribute<SharedContextParamAttribute>())
                .FirstOrDefault(a => a != null) ?? new SharedContextParamAttribute();

            try
            {
                startMethod?.Invoke(instance, new object[] { contextParam.DayCaloriesNorm });

                var sorted = group.OrderBy(m => m.GetCustomAttribute<SharedContextAttribute>().Priority);
                foreach (var method in sorted)
                {
                    try
                    {
                        if (method.ReturnType == typeof(Task)) await (Task)method.Invoke(instance, null);
                        else method.Invoke(instance, null);
                        LogResult($"Context-{group.Key}", method.Name, "OK", ConsoleColor.Cyan);
                    }
                    catch (Exception ex)
                    {
                        LogResult($"Context-{group.Key}", method.Name, $"ПРОВАЛЕН: {ex.InnerException?.Message}", ConsoleColor.Red);
                        break;
                    }
                }
                finishMethod?.Invoke(instance, null);
            }
            catch (Exception ex)
            {
                LogResult($"Context-{group.Key}", "Инициализация", $"ОШИБКА: {ex.Message}", ConsoleColor.Red);
            }
        }

        private static void LogResult(string className, string methodName, string status, ConsoleColor color)
        {
            lock (consoleLock)
            {
                Console.Write($"[{Thread.CurrentThread.ManagedThreadId}] ");
                Console.Write($"{className.PadRight(20)} | {methodName.PadRight(25)} : ");
                Console.ForegroundColor = color;
                Console.WriteLine(status);
                Console.ResetColor();
            }
        }
    }
}
