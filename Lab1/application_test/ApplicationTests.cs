using application;
using lab1_test_framework;

namespace application_test
{
    [TestClass]
    public class ApplicationTests
    {
        RecipeCalorieCalculator app;

        [Start]
        public void StartTest(int dayCalories)
        {
            app = new RecipeCalorieCalculator(dayCalories);
        }

        [End]
        public void EndTest()
        {
            app = null;
        }

        [TestMethod(DayCaloriesNorm = 1500, AdditionalInfo = "Проверка вегетарианства")]
        public void TestVegetarian()
        {
            var meal = new Dictionary<string, double> { ["курица"] = 100 };
            var result = app.CheckDietCompliance(meal, DietType.Vegetarian);

            // Используем магию Expression Trees для проверки свойств и содержания строки
            Tests.Check(() => result.IsCompliant == false);
            Tests.Check(() => result.Message != null);
            Tests.Check(() => result.Message.Contains("мясо"));
        }

        [TestMethod(DayCaloriesNorm = 2500, AdditionalInfo = "Проверка подсчета калорий")]
        [SharedContextParam(AdditionalInfo = "Проверка подсчета допустимой массы нового блюда", DayCaloriesNorm = 2000)]
        [SharedContext(1, 1)]
        public void TestCalorieLimit()
        {
            var meal = new Dictionary<string, double> { ["масло"] = 500 };
            int total = app.CalculateTotalCalories(meal);

            // Если упадет, увидим: Ожидалось total (3585) > app.dayCaloriesNorm (2500)
            Tests.Check(() => total > app.dayCaloriesNorm);
        }

        [TestMethod(AdditionalInfo = "Проверка асинхронного вызова")]
        [Category("Critical")]
        [Timeout(2000)]
        public async Task TestAsyncCalorieGet()
        {
            int cal = await app.GetIngredientCaloriesAsync("яйца");

            Tests.Check(() => cal == 155);
            Tests.Check(() => cal != 0);
        }

        [TestMethod(AdditionalInfo = "Проверка количества записей")]
        [Skip]
        public void TestCollection()
        {
            var list = app.GetAvailableIngredients();
            // Проверка размера коллекции через дерево выражений
            Tests.Check(() => list.Count == 9);
        }

        [TestMethod(AdditionalInfo = "Тест подсчета возможной порции")]
        [SharedContext(1, 2)]
        public void TestPossiblePortionCalculation()
        {
            var meal = new Dictionary<string, double> { ["молоко"] = 100 };
            int gramm = app.PossiblePortionForRecipe(meal);
            var new_meal = new Dictionary<string, double> { ["молоко"] = gramm };
            int cal = app.CalculateTotalCalories(new_meal);

            Tests.Check(() => cal < app.dayCaloriesNorm + 1);
            Tests.Check(() => cal >= 0);
        }

        [TestMethod(AdditionalInfo = "Поиск несуществующего ингридиента")]
        [Parameter(new Object[] { "бетон" })]
        public void TestNullSearch(string ingridient)
        {
            var info = app.GetIngredientDescription(ingridient);
            // Проверка на null
            Tests.Check(() => info == null);
        }

        public static IEnumerable<object[]> GetCaloriesDataBase()
        {
            yield return new object[] { "сахар", 387 };
            yield return new object[] { "масло", 717 };
            yield return new object[] { "яйца", 155 };
            yield return new object[] { "курица", 165 };
        }

        [TestMethod(AdditionalInfo = "Проверка значений словаря")]
        [ValueSource("GetCaloriesDataBase")]
        public void TestDatabaseData(string ingridName, int caloriesExpected)
        {
            var dictIngridient = new Dictionary<string, double> { [ingridName] = 100 };
            int current = app.CalculateTotalCalories(dictIngridient);

            Tests.Check(() => current == caloriesExpected);
        }

        public static IEnumerable<object[]> GetCaloriesData()
        {
            yield return new object[] { "мука", 364 };
            yield return new object[] { "молоко", 64 };
            yield return new object[] { "рис", 344 };
        }

        [TestMethod(AdditionalInfo = "Тест с использованием yield return")]
        [ValueSource("GetCaloriesData")]
        public void TestWithYieldReturn(string ingridName, int expectedCalories)
        {
            var dictIngridient = new Dictionary<string, double> { [ingridName] = 100 };
            int current = app.CalculateTotalCalories(dictIngridient);

            Tests.Check(() => current == expectedCalories);
        }

        [SharedContextParam(AdditionalInfo = "Проверка накопительной возможности контекста", DayCaloriesNorm = 2000)]
        [SharedContext(contextId: 2, priority: 1)]
        public void Step1_CalculateBreakfast()
        {
            var breakfast = new Dictionary<string, double> { ["яйца"] = 200 };
            int currentMeal = app.CalculateTotalCalories(breakfast);

            Tests.Check(() => currentMeal == 111);
            Tests.Check(() => app.totalCalories == 111);
        }

        [SharedContext(contextId: 2, priority: 2)]
        public void Step2_CalculateLunch()
        {
            var lunch = new Dictionary<string, double> { ["курица"] = 200 };
            int currentMeal = app.CalculateTotalCalories(lunch);

            Tests.Check(() => currentMeal == 330);
            Tests.Check(() => app.totalCalories == 640);
        }

        [SharedContext(contextId: 2, priority: 3)]
        public void Step3_CheckDailyLimit()
        {
            Tests.Check(() => app.totalCalories < app.dayCaloriesNorm);
            Tests.Check(() => app.totalCalories > 0);
        }
    }
}

