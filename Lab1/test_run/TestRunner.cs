using application_test;
using lab1_test_framework;
using System.Reflection;

static async Task RunTest(MethodInfo method, object testInstance, object[] parameters)
{
    if (method.ReturnType == typeof(Task))
    {
        await (Task)method.Invoke(testInstance, parameters);
    }
    else
    {
        method.Invoke(testInstance, parameters);
    }
    
}

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


foreach (Type testClassType in testClasses)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"\n=== запуск тестов из класса {testClassType.Name} ===\n");
    Console.ResetColor();
    object testInstance = Activator.CreateInstance(testClassType);
    MethodInfo[] methods = testClassType.GetMethods();

    MethodInfo startMethod = null;
    MethodInfo finishMethod = null;

    foreach (var m in methods)
    {
        if (m.GetCustomAttribute<StartAttribute>() != null) startMethod = m;
        if (m.GetCustomAttribute<EndAttribute>() != null) finishMethod = m;
    }

    int passed = 0;
    int failed = 0;

    Console.WriteLine("--- Запуск обычных тестов ---");
    foreach (var method in methods)
    {
        var attr = method.GetCustomAttribute<TestMethodAttribute>();
        if (attr == null) continue;
        if (method.GetCustomAttribute<SkipAttribute>() != null)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"Тест {method.Name} пока не работает");
            Console.ResetColor();
            continue;
        }
        try
        {
            Console.Write($"Запуск {method.Name} ({attr.AdditionalInfo})... ");


            var paramAttrs = method.GetCustomAttributes<ParameterAttribute>().ToArray();
            Object[] parameters = null;
            if (paramAttrs != null && paramAttrs.Length > 0)
            {
                foreach (var param in paramAttrs)
                {

                    startMethod?.Invoke(testInstance, new object[] { attr.DayCaloriesNorm });
                    await RunTest(method, testInstance, param.parameters);
                    finishMethod?.Invoke(testInstance, null);

                    Console.Write($"\n\tТест с параметрами пройден успешно\t");
                }
            }
            else
            {
                startMethod?.Invoke(testInstance, new object[] { attr.DayCaloriesNorm });
                await RunTest(method, testInstance, null);
                finishMethod?.Invoke(testInstance, null);
            }


            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Пройден");
            Console.ResetColor();
            passed++;
        }
        catch (Exception ex)
        {
            var realException = ex.InnerException ?? ex;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Провален: {realException.Message}");
            Console.ResetColor();
            failed++;
        }
    }

    Console.WriteLine("\n--- Результаты ---");
    Console.WriteLine($"Всего: {passed + failed} | Успешно: {passed} | Ошибок: {failed}");




    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n\n--- Запуск тестов с SharedContext ---");
    Console.ResetColor();
    var methodsWithShared = new List<(MethodInfo method, int contextId, int priority)>();

    foreach (var method in methods)
    {
        var attr = method.GetCustomAttribute<SharedContextAttribute>();
        if (attr != null)
        {
            methodsWithShared.Add((method, attr.ContextId, attr.Priority));
        }
    }

    var contextGroup = methodsWithShared.GroupBy(t => t.contextId).OrderBy(g => g.Key);

    foreach (var group in contextGroup)
    {

        passed = 0;
        failed = 0;
        Console.WriteLine($"\n--- Исполнение Контекста ID: {group.Key} ---");

        var sortedGroup = group.OrderBy(t => t.priority).ToList();

        var paramAttr = sortedGroup[0].method.GetCustomAttribute<SharedContextParamAttribute>();

        Console.Write($"Инициализация контекста для ({paramAttr.AdditionalInfo})... \n");

        startMethod?.Invoke(testInstance, new object[] { paramAttr.DayCaloriesNorm });

        foreach (var methodInfo in sortedGroup)
        {
            var method = methodInfo.method;

            if (method.GetCustomAttribute<SkipAttribute>() != null)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"Тест {method.Name} пока не работает");
                Console.ResetColor();
                continue;
            }
            try
            {
                Console.Write($"{methodInfo.priority}) {method.Name} ... ");


                var paramAttrs = method.GetCustomAttributes<ParameterAttribute>().ToArray();
                Object[] parameters = null;
                if (paramAttrs != null && paramAttrs.Length > 0)
                {
                    foreach (var param in paramAttrs)
                    {

                        await RunTest(method, testInstance, param.parameters);

                        Console.Write($"\n\tТест с параметрами пройден успешно\t");
                    }
                }
                else
                {
                    await RunTest(method, testInstance, null);
                }


                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Пройден");
                Console.ResetColor();
                passed++;
            }
            catch (Exception ex)
            {
                var realException = ex.InnerException ?? ex;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Провален: {realException.Message}");
                Console.ResetColor();
                failed++;
            }
        }

        finishMethod?.Invoke(testInstance, null);
        Console.WriteLine("\n--- Результаты выполнения контекста ---");
        Console.WriteLine($"Всего: {passed + failed} | Успешно: {passed} | Ошибок: {failed}");
    }
}