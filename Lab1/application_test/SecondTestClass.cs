using application;
using lab1_test_framework;

namespace application_test
{
    [TestClass]
    public class SecondTestClass
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
