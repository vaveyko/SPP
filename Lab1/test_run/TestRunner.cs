using application_test;
using lab1_test_framework;
using System.Reflection;

static async Task RunTest(MethodInfo method, object testInstance, object[] parameters)
{
    if (method.ReturnType == typeof(Task))
    {
        await(Task)method.Invoke(testInstance, parameters);
    }
    else
    {
        method.Invoke(testInstance, parameters);
    }
}

static async Task CheckStandartTestMethod()
{

}

Console.WriteLine("--- Запуск тестов ---");

Type testClassType = typeof(ApplicationTests);
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

foreach (var method in methods)
{
    var attr = method.GetCustomAttribute<TestMethodAttribute>();
    if (attr == null) continue;

    try
    {
        Console.Write($"Запуск {method.Name} ({attr.AdditionalInfo})... ");


        var paramAttrs = method.GetCustomAttributes<ParameterAttribute>().ToArray();
        Object[] parameters = null;
        if (paramAttrs != null && paramAttrs.Length > 0)
        {
            foreach( var param in paramAttrs)
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
        // Извлекаем реальную ошибку из Reflection
        var realException = ex.InnerException ?? ex;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Провален: {realException.Message}");
        Console.ResetColor();
        failed++;
    }
}

Console.WriteLine("\n--- Результаты ---");
Console.WriteLine($"Всего: {passed + failed} | Успешно: {passed} | Ошибок: {failed}");





Console.WriteLine("--- Запуск тестов с SharedContext ---");
var methodsWithShared = new List<(MethodInfo method, int contextId, int priority)>();
SharedContextParamAttribute contextParamAttr;

foreach(var method in methods)
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

    startMethod?.Invoke(testInstance, new object[] {paramAttr.DayCaloriesNorm});

    foreach(var methodInfo in sortedGroup)
    {
        var method = methodInfo.method;
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
            // Извлекаем реальную ошибку из Reflection
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
