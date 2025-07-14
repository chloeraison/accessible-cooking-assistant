using System;

// Entry point for the app
class Program
{
    static void Main(string[] args)
    {
        // Initialise the class that handles recipe data
        var recipeManager = new RecipeManager();

        // Ask the user what recipe they want to load
        Console.Write("Enter a recipe name: ");
        string name = Console.ReadLine();

        // Load recipe steps from database
        recipeManager.LoadRecipe(name);

        // Display the first instruction
        Console.WriteLine("First step:");
        Console.WriteLine(recipeManager.GetCurrentStep());

        string input;

        // Start interaction loop: allows the user to navigate through the recipe
        do
        {
            Console.Write("\nCommand (next, prev, print, exit): ");
            input = Console.ReadLine()?.ToLower();

            switch (input)
            {
                case "next":
                    // Move to next instruction
                    Console.WriteLine(recipeManager.NextStep());
                    break;

                case "prev":
                    // Go back to previous instruction
                    Console.WriteLine(recipeManager.PreviousStep());
                    break;

                case "print":
                    // Show all recipe steps
                    recipeManager.PrintAllSteps();
                    break;

                case "exit":
                    // Leave the application
                    Console.WriteLine("Exiting.");
                    break;

                default:
                    // Failsafe for unknown input
                    Console.WriteLine("Unknown command.");
                    break;
            }

        } while (input != "exit"); // Loop until explicitly told to stop
    }
}
