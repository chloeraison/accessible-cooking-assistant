using System;

// This is the entry point of the whole application.
// Like the first step in a recipe, Main() is where things begin!
class Program
{
    static void Main(string[] args)
    {
        // Create a new instance of our custom RecipeManager class.
        // Think of this as opening our virtual cookbook.
        var recipeManager = new RecipeManager();

        // Ask the user for the name of the recipe they want.
        // For now, they have to type it exaactly as it exists in the database.
        Console.Write("Enter a recipe name: ");
        string name = Console.ReadLine();

        // Call the LoadRecipe method, which:
        // - Connects to PostgreSQL
        // - Finds all steps of the given recipe name
        // - Loads the steps into memory for step-by-step access
        recipeManager.LoadRecipe(name);

        // We print the very first instruction, so the user knows what to do.
        Console.WriteLine("First step:");
        Console.WriteLine(recipeManager.GetCurrentStep());

        string input;

        // This loop allows the user to navigate through the recipe instructions.
        // Like flipping through the pages of a coookbook.
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
                    // Graceful goodbye message
                    Console.WriteLine("Exiting.");
                    break;

                default:
                    // If the command is not recognised, let the user know.
                    Console.WriteLine("Unknown command.");
                    break;
            }

        } while (input != "exit"); // Keeps looping until the user types "exit"
    }
}