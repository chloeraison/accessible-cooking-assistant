using System;

class Program
{
    static void Main(string[] args)
    {
        // Create a new instance of custom RecipeManager class
        var recipeManager = new RecipeManager();

        // Ask user for the name of the recipe they want
        Console.Write("Enter a recipe name: ");
        string name = Console.ReadLine();

        // Call the LoadRecipe method, which:
        // - Connects to PostgreSQL
        // - Finds all steps of the given recipe name
        // - Loads the steps into memory for step-by-step access
        recipeManager.LoadRecipe(name);

        Console.WriteLine("First step:");
        Console.WriteLine(recipeManager.GetCurrentStep());

        string input;

        // This loop allows the user to navigate through the recipe instructions
        do
        {
            Console.Write("\nCommand (next, prev, print, exit): ");
            input = Console.ReadLine()?.ToLower();

            switch (input)
            {
                case "next":
                    // Moves forward one step
                    Console.WriteLine(recipeManager.NextStep());
                    break;

                case "prev":
                    // Moves back one step
                    Console.WriteLine(recipeManager.PreviousStep());
                    break;

                case "print":
                    // Prints the entire recipe, step by step
                    recipeManager.PrintAllSteps();
                    break;

                case "exit":
                    Console.WriteLine("Exiting.");
                    break;

                default:
                    // If the command is not recognised, let user know
                    Console.WriteLine("Unknown command.");
                    break;
            }

        } while (input != "exit"); // Keeps looping until the user types "exit"
    }
}