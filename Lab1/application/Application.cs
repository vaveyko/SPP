namespace application
{
    public class RecipeCalorieCalculator
    {
        private readonly Dictionary<string, double> _ingredientCalories = new()
        {
            ["мука"] = 364,      // ккал/100г
            ["сахар"] = 387,
            ["масло"] = 717,
            ["яйца"] = 155,
            ["молоко"] = 64,
            ["курица"] = 165,
            ["рис"] = 344,
            ["картошка"] = 77,
            ["сыр"] = 402
        };

        public int totalCalories { get; set; }
        public int dayCaloriesNorm { get; }

        public RecipeCalorieCalculator(int dayNormCalories)
        {
            this.dayCaloriesNorm = dayNormCalories;
        }

        public int CalculateTotalCalories(Dictionary<string, double> ingredientsInGrams)
        {
            double total = 0;
            foreach (var kvp in ingredientsInGrams)
            {
                if (_ingredientCalories.TryGetValue(kvp.Key.ToLower(), out double calPer100g))
                {
                    total += (kvp.Value / 100) * calPer100g;
                }
            }
            totalCalories += (int)total;
            return (int)total;
        }

        public DietCheckResult CheckDietCompliance(Dictionary<string, double> ingredientsInGrams,
                                                  DietType dietType)
        {
            double calories = CalculateTotalCalories(ingredientsInGrams);

            if (calories > dayCaloriesNorm)
                return new DietCheckResult(false, $"Превышен лимит калорий: {calories} > {dayCaloriesNorm}");

            switch (dietType)
            {
                case DietType.Vegetarian:
                    if (ingredientsInGrams.ContainsKey("курица") || ingredientsInGrams.ContainsKey("яйца"))
                        return new DietCheckResult(false, "Нельзя мясо или яйца");
                    break;
                case DietType.Vegan:
                    if (ingredientsInGrams.ContainsKey("курица") || ingredientsInGrams.ContainsKey("сыр") || ingredientsInGrams.ContainsKey("молоко") || ingredientsInGrams.ContainsKey("яйца"))
                        return new DietCheckResult(false, "Нельзя мясо, яйца, сыр, молоко");
                    break;
                case DietType.LowCarb:
                    double carbsWeight = (ingredientsInGrams.ContainsKey("рис") ? ingredientsInGrams["рис"] : 0) +
                                       (ingredientsInGrams.ContainsKey("мука") ? ingredientsInGrams["мука"] : 0);
                    if (carbsWeight > 100)
                        return new DietCheckResult(false, "Слишком много углеводов");
                    break;
            }
            return new DietCheckResult(true, $"OK, {calories} ккал");
        }

        public List<string> GetAvailableIngredients()
        {
            return new List<string>(_ingredientCalories.Keys);
        }

        public string GetIngredientDescription(string name)
        {
            if (_ingredientCalories.ContainsKey(name.ToLower()))
                return $"Продукт {name} очень полезен";

            return null;
        }

        public async Task<int> GetIngredientCaloriesAsync(string name)
        {
            await Task.Delay(10); // Искусственная задержка
            if (_ingredientCalories.ContainsKey(name.ToLower()))
                return (int)_ingredientCalories[name.ToLower()];
            return 0;
        }

        public int CalculateRecipeGramms(Dictionary<string, double> ingredientsInGrams)
        {
            double total = 0;
            foreach (var kvp in ingredientsInGrams)
            {
                total += kvp.Value;
            }
            return (int)total;
        }

        public int PossiblePortionForRecipe(Dictionary<string, double> ingredientsInGrams)
        {
            int recipeCalories = CalculateTotalCalories(ingredientsInGrams);
            int totalGrams = CalculateRecipeGramms(ingredientsInGrams);

            int recomendedGramm = (int)(totalGrams * (dayCaloriesNorm - totalCalories) / recipeCalories);
            if (recomendedGramm <= 0) return 0;
            return recomendedGramm;
        }
    }

    public enum DietType
    {
        Vegetarian,
        LowCarb,
        Vegan,
    }

    public class DietCheckResult
    {
        public bool IsCompliant { get; }
        public string Message { get; }

        public DietCheckResult(bool isCompliant, string message)
        {
            IsCompliant = isCompliant;
            Message = message;
        }
    }

}
