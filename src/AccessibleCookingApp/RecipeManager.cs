using System;
using System.Collections.Generic;
using Npgsql;

// This class encapsulates all functionality related to fetching and navigating recipe steps.
// It communicates with a PostgreSQL database and provides methods to iterate through a recipe.
public class RecipeManager
{
    private List<string> steps = new List<string>(); // Holds all the steps for a recipe
    private string recipeName;                       // Name of the currently loaded recipe
    private int currentStepIndex = 0;                // Tracks which step the user is currently on

    // Connects to PostgreSQL and loads a recipe by name.
    public void LoadRecipe(string name)
    {
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
        catch (Exception ex)
        {
            // On error, show message but don't crash app
            steps.Clear();
            steps.Add($"Error loading recipe: {ex.Message}");
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
