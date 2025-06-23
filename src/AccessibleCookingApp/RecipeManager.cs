using System;
using System.Collections.Generic;
using Npgsql;

public class RecipeManager
{
    private List<string> steps = new List<string>();
    private string recipeName;
    private int currentStepIndex = 0;

    // Loads a recipe by name from PostgreSQL
    public void LoadRecipe(string name)
    {
        recipeName = name.ToLower();
        currentStepIndex = 0;
        steps.Clear(); // Clear out previous recipe steps

        string connString = "Host=localhost;Username=postgres;Password=password;Database=accessible_recipes"; // Replace with your actual creds

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
                            steps.Add(reader.GetString(0));
                        }
                    }
                }

                if (steps.Count == 0)
                {
                    steps.Add("No recipe steps found for that name.");
                }
            }
        }
        catch (Exception ex)
        {
            steps.Clear();
            steps.Add($"Error loading recipe: {ex.Message}");
        }
    }

    public string GetCurrentStep()
    {
        if (steps.Count == 0)
            return "No recipe loaded.";
        
        return steps[currentStepIndex];
    }

    public string NextStep()
    {
        if (steps.Count == 0)
            return "No recipe loaded.";

        if (currentStepIndex < steps.Count - 1)
            currentStepIndex++;

        return steps[currentStepIndex];
    }

    public string PreviousStep()
    {
        if (steps.Count == 0)
            return "No recipe loaded.";

        if (currentStepIndex > 0)
            currentStepIndex--;

        return steps[currentStepIndex];
    }

    public void PrintAllSteps()
    {
        Console.WriteLine($"Recipe: {recipeName}");

        for (int i = 0; i < steps.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {steps[i]}");
        }
    }
}
