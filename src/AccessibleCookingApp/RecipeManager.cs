using System;
using System.Collections.Generic;
using Npgsql;

// This class encapsulates all functionality related to fetching and navigating recipe steps.
// It communicates with a PostgreSQL database and provides methods to iterate through a recipe.
public class RecipeManager
{
    private List<string> steps = new List<string>(); // Holds all the steps for a recipe
    private string recipeName = "";                   // Name of the currently loaded recipe
    private int currentStepIndex = 0;                // Tracks which step the user is currently on

    // Testing Recipe Data
    private bool UseTestData() =>
        string.Equals(Environment.GetEnvironmentVariable("USE_TEST_DATA"), "true",
                    StringComparison.OrdinalIgnoreCase);

    // Connects to PostgreSQL and loads a recipe by name.
    public void LoadRecipe(string name)
    {

        if (UseTestData())
        {
            recipeName = name.ToLower();
            currentStepIndex = 0;
            steps.Clear();

            if (TestRecipeData.Recipes.TryGetValue(recipeName, out var list) && list.Count > 0)
                steps.AddRange(list);
            else
                steps.Add("No recipe steps found for that name.");
            return; // skip DB entirely
        }

        recipeName = name.ToLower();
        currentStepIndex = 0;
        steps.Clear(); // Reset the list to avoid mixing with previous recipes

        string connString = "Host=localhost;Username=postgres;Password=password;Database=accessible_recipes";
        // In production: this should use environment variables for safety

        try
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                string query = "SELECT instruction FROM recipes WHERE LOWER(name) = @name ORDER BY step_number ASC";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("name", recipeName);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Read each step from the DB and store it
                            steps.Add(reader.GetString(0));
                        }
                    }
                }

                if (steps.Count == 0)
                {
                    // Recipe was found in DB but has no steps
                    steps.Add("No recipe steps found for that name.");
                }
            }
        }
        catch (Exception)
        {
            if (TestRecipeData.Recipes.TryGetValue(recipeName, out var list) && list.Count > 0)
            {
                steps.Clear();
                steps.AddRange(list);
                steps.Insert(0, "[DB unavailable: using test data]");
            }
            else
            {
                steps.Clear();
                // Replace raw DB error with a friendly message
                steps.Add("Error: Recipe not found. Please try again.");
            }
        }
    }

    // Returns the current step (based on index)
    public string GetCurrentStep()
    {
        if (steps.Count == 0)
            return "No recipe loaded.";

        return steps[currentStepIndex];
    }

    // Moves forward one step (if not at the end)
    public string NextStep()
    {
        if (steps.Count == 0)
            return "No recipe loaded.";

        if (currentStepIndex < steps.Count - 1)
            currentStepIndex++;

        return steps[currentStepIndex];
    }

    // Moves back one step (if not at the beginning)
    public string PreviousStep()
    {
        if (steps.Count == 0)
            return "No recipe loaded.";

        if (currentStepIndex > 0)
            currentStepIndex--;

        return steps[currentStepIndex];
    }

    // Prints the entire recipe, step by step
    public void PrintAllSteps()
    {
        Console.WriteLine($"Recipe: {recipeName}");

        for (int i = 0; i < steps.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {steps[i]}");
        }
    }
}
