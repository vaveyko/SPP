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

            Tests.IsFalse(result.IsCompliant); // 1 проверка
            Tests.IsNotNull(result.Message);    // 2 проверка
            Tests.StringContains(result.Message, "мясо"); // 3 проверка
        }

        [TestMethod(DayCaloriesNorm = 2500, AdditionalInfo = "Проверка подсчета калорий")]
        [SharedContextParam(AdditionalInfo = "Проверка подсчета допустимой массы нового блюда", DayCaloriesNorm = 2000)]
        [SharedContext(1, 1)]
        public void TestCalorieLimit()
        {
            var meal = new Dictionary<string, double> { ["масло"] = 500 }; // Очень много калорий
            int total = app.CalculateTotalCalories(meal);

            Tests.IsGreater(total, app.dayCaloriesNorm); // 4 проверка
        }

        [TestMethod(AdditionalInfo = "Проверка асинхронного вызова")]
        public async Task TestAsyncCalorieGet() // Асинхронный тест
        {
            int cal = await app.GetIngredientCaloriesAsync("яйца");
            Tests.IsEqual(cal, 155); // 6 проверка
            Tests.IsNotEqual(cal, 0); // 7 проверка
        }



        [TestMethod(AdditionalInfo = "Проверка количества записей")]
        public void TestCollection()
        {
            var list = app.GetAvailableIngredients();
            Tests.CollectionCount(list.Count, 9);
        }

        [TestMethod(AdditionalInfo = "Тест подсчета возможной порции")]
        [SharedContext(1, 2)]
        public void TestPossiblePortionCalculation()
        {
            var meal = new Dictionary<string, double> { ["молоко"] = 100 };
            int gramm = app.PossiblePortionForRecipe(meal);
            var new_meal = new Dictionary<string, double> { ["молоко"] = gramm };
            int cal = app.CalculateTotalCalories(new_meal);

            Tests.IsLess(cal, app.dayCaloriesNorm+1);
            Tests.IsTrue(cal >= 0);
        }

        [TestMethod(AdditionalInfo = "Поиск несуществующего ингридиента")]
        [Parameter(new Object[] { "бетон" })]
        //[Parameter(new Object[] { "молоко"})]
        public void TestNullSearch(string ingridient)
        {
            var info = app.GetIngredientDescription(ingridient);
            Tests.IsNull(info);
        }

        [TestMethod(AdditionalInfo = "Проверка значений словаря")]
        [Parameter(new Object[] { "мука", 364 })]
        [Parameter(new Object[] { "сахар", 387 })]
        [Parameter(new Object[] { "масло", 717 })]
        [Parameter(new Object[] { "яйца", 155 })]
        [Parameter(new Object[] { "молоко", 64 })]
        [Parameter(new Object[] { "курица", 165 })]
        public void TestDatabaseData(string ingridName, int caloriesExpected)
        {
            var dictIngridient = new Dictionary<string, double> { [ingridName] = 100 };
            int current = app.CalculateTotalCalories(dictIngridient);
            Tests.IsEqual(current, caloriesExpected);
        }




        [SharedContextParam(AdditionalInfo = "Проверка накопительной возможности контекста", DayCaloriesNorm = 2000)]
        [SharedContext(contextId: 2, priority: 1)]
        public void Step1_CalculateBreakfast()
        {
            // 310 ккал.
            var breakfast = new Dictionary<string, double> { ["яйца"] = 200 };

            int currentMeal = app.CalculateTotalCalories(breakfast);

            Tests.IsEqual(currentMeal, 310);          
            Tests.IsEqual(app.totalCalories, 310);   
        }

        [SharedContext(contextId: 2, priority: 2)]
        public void Step2_CalculateLunch()
        {
            // 330 ккал.
            var lunch = new Dictionary<string, double> { ["курица"] = 200 };

            int currentMeal = app.CalculateTotalCalories(lunch);

            Tests.IsEqual(currentMeal, 330);
            Tests.IsEqual(app.totalCalories, 640); 
        }

        [SharedContext(contextId: 2, priority: 3)]
        public void Step3_CheckDailyLimit()
        {
            Tests.IsLess(app.totalCalories, app.dayCaloriesNorm);

            Tests.IsTrue(app.totalCalories > 0);
        }
    }
}

