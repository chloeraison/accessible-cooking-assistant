using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var recipeManager = new RecipeManager();

        // Try to enable voice; if creds missing, we’ll just use keyboard.
        VoiceInterface? voice = null;
        try
        {
            voice = new VoiceInterface();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Voice disabled: {ex.Message}]");
        }

        Console.Write("Say or type a recipe name: ");

        // Try voice once; fall back to keyboard if null/empty or voice not available
        string? spokenName = voice != null ? await voice.ListenOnceAsync() : null;
        string? name = !string.IsNullOrWhiteSpace(spokenName) ? spokenName : Console.ReadLine();

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("No recipe name provided.");
            return;
        }

        name = name.Trim().Trim('"', '.', '!', '?').ToLowerInvariant();
        recipeManager.LoadRecipe(name);

        Console.WriteLine("First step:");
        Console.WriteLine(recipeManager.GetCurrentStep());

        while (true)
        {
            Console.Write("\nCommand (next, previous, print, end): ");

            // Voice first, then keyboard fallback
            string? voiceCommand = voice != null ? await voice.ListenOnceAsync() : null;

            string input = !string.IsNullOrWhiteSpace(voiceCommand)
                ? voiceCommand
                : (Console.ReadLine() ?? string.Empty);

            // normalise: trim whitespace + stray punctuation, then lower-case once
            input = input.Trim().Trim('"', '.', '!', '?').ToLowerInvariant();

            switch (input)
            {
                case "next":
                    Console.WriteLine(recipeManager.NextStep());
                    break;

                case "previous":
                    Console.WriteLine(recipeManager.PreviousStep());
                    break;

                case "print":
                    recipeManager.PrintAllSteps();
                    break;

                case "end":
                    Console.WriteLine("Exiting.");
                    return;

                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }
    }
}
