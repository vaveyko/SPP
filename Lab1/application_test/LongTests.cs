using application;
using lab1_test_framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace application_test
{
    [TestClass]
    internal class LongTests
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

        [TestMethod(DayCaloriesNorm = 2000, AdditionalInfo = "Тяжелый расчет сложного рецепта")]
        public void HeavyCalculation()
        {
            // Имитируем долгий расчет
            Thread.Sleep(1500);

            var meal = new Dictionary<string, double>
            {
                ["мука"] = 200,
                ["сахар"] = 100,
                ["масло"] = 50,
                ["молоко"] = 200
            };

            int calories = app.CalculateTotalCalories(meal);

            Tests.IsGreater(calories, 1000);
            Tests.IsLess(calories, 2000);
        }

        [TestMethod(DayCaloriesNorm = 1500, AdditionalInfo = "Долгая проверка веганского меню")]
        [Timeout(2500)]
        public async Task SlowVeganCheck()
        {
            await Task.Delay(2000);

            var meal = new Dictionary<string, double>
            {
                ["рис"] = 200,
                ["сыр"] = 50
            };

            var result = app.CheckDietCompliance(meal, DietType.Vegan);

            Tests.IsFalse(result.IsCompliant);
            Tests.StringContains(result.Message, "сыр");
        }

        [TestMethod(DayCaloriesNorm = 3000, AdditionalInfo = "Массовая проверка ингредиентов")]
        public async Task AsyncBatchProcessing()
        {
            await Task.Delay(1000);

            var ingredients = app.GetAvailableIngredients();

            Tests.CollectionCount(ingredients.Count, 9);
            Tests.IsTrue(ingredients.Contains("курица"));

            int cal = await app.GetIngredientCaloriesAsync("картошка");
            Tests.IsEqual(cal, 77);
        }
    }
}
