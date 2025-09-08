// Main entry point for the Accessible Cooking Assistant
//
// **Outcome**
// -> Lets the user load test recipes by name
// -> Navigate steps (next / previous / repeat / print)
// -> Start/stop multiple named timers (e.g. "timer pasta 12")
// -> Convert common units (ml <-> cups/tbsp/tsp, g <-> oz)
// -> Fully voice controlled (Azure Speech) with keyboard fallback
// -> Voice can be toggled on/off with commands
//
// **Plan**
// 1. Start by loading a recipe (voice-first, keyboard fallback).
// 2. Enter a loop where the app listens for commands.
// 3. Handle navigation, timers, conversions, and exit.
// 4. Surface finished timers every loop so the user is notified.
// 5. Keep UX simple + accessible: short prompts, clear feedback.
//

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Globalization;
using System.Reflection; // for BindingFlags in ListRecipesIfAvailable

class Program
{
    static async Task Main(string[] args)
    {
        var recipeManager = new RecipeManager();
        var timers = new MultiTimer();

        // Try to enable voice; fallback to keyboard if unavailable
        VoiceInterface? voice = null;
        try { voice = new VoiceInterface(); }
        catch (Exception ex) { Console.WriteLine($"[Voice disabled: {ex.Message}]"); }

        bool voicePreferred = true; // toggle with "voice on/off"

        // ~*~ Step 1: pick a recipe ~*~
        var pick = await PromptForRecipeAsync(recipeManager, voice, voicePreferred);
        string currentRecipe = pick.recipe;
        voicePreferred = pick.voicePreferred;

        // ~*~ Step 2: main loop for commands ~*~
        while (true)
        {
            // check timers on every loop
            var finished = timers.CleanupFinishedTimers();
            foreach (var f in finished) Console.WriteLine($"[{f}] finished!");

            string raw = await GetInputAsync(
                voice, voicePreferred,
                "\nCommand (load <name>, next, previous, repeat, print, timer, status, stop, stop all, convert, voice on/off, help, end): "
            );

            string input = Clean(raw);

            switch (input)
            {
                // ~*~ recipe navigation ~*~
                case "next":
                    Console.WriteLine(recipeManager.NextStep());
                    break;

                case "previous":
                case "prev": // shortcut
                    Console.WriteLine(recipeManager.PreviousStep());
                    break;

                case "repeat":
                    Console.WriteLine(recipeManager.GetCurrentStep());
                    break;

                case "print":
                    recipeManager.PrintAllSteps();
                    break;

                case "list recipes":
                    ListRecipesIfAvailable();
                    break;

                // ~*~ load/pivot recipe ~*~
                case var s when s.StartsWith("load "):
                    {
                        var newName = Clean(input.Substring(5));
                        if (string.IsNullOrWhiteSpace(newName))
                        {
                            Console.WriteLine("Please provide a recipe name.");
                            break;
                        }
                        recipeManager.LoadRecipe(newName);
                        Console.WriteLine("First step:");
                        Console.WriteLine(recipeManager.GetCurrentStep());
                        break;
                    }

                // ~*~ timers ~*~
                case var s when s.StartsWith("timer") || s.StartsWith("set ") || s.StartsWith("start "):
                    {
                        if (TryParseTimer(input, out var tName, out var mins))
                        {
                            timers.StartTimer(tName, mins);
                            Console.WriteLine($"Started timer '{tName}' for {mins} minute(s).");
                        }
                        else
                        {
                            Console.WriteLine("Try: 'timer 5' | 'timer pasta 12' | 'set pasta timer for 12 minutes'.");
                        }
                        break;
                    }

                case var s when s.StartsWith("status"):
                    {
                        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 1)
                        {
                            foreach (var line in timers.StatusAll()) Console.WriteLine(line);
                        }
                        else
                        {
                            Console.WriteLine(timers.Status(parts[1]));
                        }
                        break;
                    }

                case var s when s.StartsWith("stop ") && !s.Equals("stop all"):
                    {
                        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && timers.StopTimer(parts[1]))
                            Console.WriteLine($"Stopped '{parts[1]}'.");
                        else
                            Console.WriteLine("Specify a timer name to stop, e.g., 'stop pasta'.");
                        break;
                    }

                case "stop all":
                    timers.StopAll();
                    Console.WriteLine("Stopped all timers.");
                    break;

                // ~*~ conversions ~*~
                case var s when s.StartsWith("convert"):
                    {
                        if (TryParseConvert(input, out var value, out var from, out var to))
                        {
                            try
                            {
                                var result = UnitConverter.Convert(value, from, to);
                                Console.WriteLine($"{value:0.###} {from} = {result:0.###} {to}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Conversion not supported: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Try: convert 240 ml to cups   |   convert 4 oz to g");
                        }
                        break;
                    }

                    static void ListRecipesIfAvailable()
                    {
                        try
                        {
                            var field = typeof(TestRecipeData).GetField("Recipes", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                            if (field?.GetValue(null) is System.Collections.IDictionary dict)
                            {
                                Console.WriteLine("Available recipes:");
                                foreach (var key in dict.Keys) Console.WriteLine($"- {key}");
                            }
                            else
                            {
                                Console.WriteLine("Recipe list unavailable in this mode.");
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Recipe list unavailable in this mode.");
                        }
                    }

                // ~*~ voice toggling ~*~
                case "voice off":
                    voicePreferred = false;
                    Console.WriteLine("Voice disabled (keyboard mode). Say 'voice on' to re-enable.");
                    break;

                case "voice on":
                    if (voice == null) Console.WriteLine("Voice not available (check .env/mic).");
                    else { voicePreferred = true; Console.WriteLine("Voice enabled (listening first)."); }
                    break;

                // ~*~ misc ~*~
                case "help":
                    Console.WriteLine("Commands:");
                    Console.WriteLine("- load <name>     : load another recipe");
                    Console.WriteLine("- next / previous / repeat / print");
                    Console.WriteLine("- timer …         : start timers (e.g., 'timer 5', 'timer pasta 12')");
                    Console.WriteLine("- status [name]   : check timers");
                    Console.WriteLine("- stop <name> / stop all");
                    Console.WriteLine("- convert <val> <from> to <to> (e.g., 'convert 240 ml to cups')");
                    Console.WriteLine("- voice on / voice off");
                    Console.WriteLine("- end / exit      : quit");
                    break;

                case "end":
                case "exit":
                    Console.WriteLine("Exiting.");
                    return;

                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }
    }

    // ~*~ helpers ~*~

    // Voice-first input with retries; keyboard fallback if nothing heard
    static async Task<string> GetInputAsync(VoiceInterface? voice, bool voicePreferred, string prompt, int voiceRetries = 3)
    {
        Console.Write(prompt);

        if (voicePreferred && voice != null)
        {
            for (int i = 0; i < voiceRetries; i++)
            {
                string? heard = await voice.ListenOnceAsync();
                if (!string.IsNullOrWhiteSpace(heard))
                    return heard;
                Console.WriteLine("(didn't catch that — try again… or start typing)");
            }
        }
        return Console.ReadLine() ?? string.Empty;
    }

    // Clean up ASR/keyboard input: trim spaces, strip punctuation, lowercase
    static string Clean(string s) => s.Trim().Trim('"', '.', '!', '?').ToLowerInvariant();

    // parse "timer 5", "timer pasta 12", "set pasta timer for 12 minutes"
    // and "set/start X timer for 12 minutes"
    static bool TryParseTimer(string input, out string name, out double minutes)
    {
        name = "timer";
        minutes = 0;

        // ---------- Pattern A: "set/start X timer for 12 minutes" ----------
        var m = Regex.Match(
            input,
            @"^(set|start)\s+([\w\- ]+)?\s*timer(?:\s+for)?\s+(\d+(?:\.\d+)?)\s*(\w+)?$",
            RegexOptions.IgnoreCase
        );
        if (m.Success)
        {
            if (!string.IsNullOrWhiteSpace(m.Groups[2].Value))
                name = m.Groups[2].Value.Trim();

            var numStr   = m.Groups[3].Value;
            var unitWord = m.Groups[4].Success ? m.Groups[4].Value : "";
            minutes = ParseMinutes(numStr, unitWord);
            return minutes > 0;
        }

        // ---------- Pattern B: commands starting with "timer ..." ----------
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && parts[0] == "timer")
        {
            // B1: "timer 5" or "timer 5 seconds"
            if (double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var numFirst))
            {
                string unitWord = parts.Length >= 3 ? parts[2] : "";
                minutes = ParseMinutes(numFirst.ToString(CultureInfo.InvariantCulture), unitWord);
                name = "timer";
                return minutes > 0;
            }

            // B2: "timer oven 2" or "timer tea 30 seconds"
            // Decide where the number is (last or second-last if the last is a unit)
            int lastIndex = parts.Length - 1;
            bool lastIsUnit = IsTimeUnit(parts[lastIndex]);

            int numberIndex = lastIsUnit ? lastIndex - 1 : lastIndex;
            if (numberIndex <= 1) return false; // need at least a name and a number

            if (!double.TryParse(parts[numberIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var numEnd))
                return false;

            string unitWord2 = lastIsUnit ? parts[lastIndex] : "";
            name = string.Join(' ', parts[1..numberIndex]).Trim();
            minutes = ParseMinutes(numEnd.ToString(CultureInfo.InvariantCulture), unitWord2);
            return minutes > 0;
        }

        return false;
    }

    // simple check for time unit words
    static bool IsTimeUnit(string s)
    {
        s = s.ToLowerInvariant();
        return s.StartsWith("sec") || s == "s"
            || s.StartsWith("min") || s == "m"
            || s == "minute" || s == "minutes";
    }

    // convert raw numbers + optional unit to minutes
    static double ParseMinutes(string numStr, string unit)
    {
        if (!double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
            return 0;

        unit = unit.ToLowerInvariant();
        if (unit.StartsWith("sec") || unit == "s") return val / 60.0; // seconds → minutes
        return val; // default assume minutes
    }

    // parse "convert 240 ml to cups"
    static bool TryParseConvert(string input, out double value, out string from, out string to)
    {
        value = 0; from = ""; to = "";
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 5 && parts[0] == "convert")
        {
            int toIdx = Array.IndexOf(parts, "to");
            if (toIdx > 2 && toIdx < parts.Length - 1 &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                from = parts[2];
                to = parts[toIdx + 1];
                return true;
            }
        }
        return false;
    }
    
    // Is this input a global command (not a recipe name)?
static bool IsGlobalCommand(string s) =>
    s.StartsWith("voice ") || s.StartsWith("help") || s.StartsWith("load ") ||
    s.StartsWith("timer") || s.StartsWith("set ") || s.StartsWith("start ") ||
    s.StartsWith("status") || s.StartsWith("stop") || s.StartsWith("convert");

// Utility: list available recipe names in Test mode (optional nicety)
static void ListRecipesIfAvailable()
{
    try
    {
        // Only works when USE_TEST_DATA=true and TestRecipeData is in use
        var prop = typeof(TestRecipeData).GetField("Recipes");
        if (prop?.GetValue(null) is System.Collections.IDictionary dict)
        {
            Console.WriteLine("Available recipes:");
            foreach (var key in dict.Keys) Console.WriteLine($"- {key}");
        }
        else
        {
            Console.WriteLine("Recipe list unavailable in this mode.");
        }
    }
    catch
    {
        Console.WriteLine("Recipe list unavailable in this mode.");
    }
}

    // Ask for a recipe until one is successfully loaded (Say 'list recipes', 'voice off', etc.)
static async Task<(string recipe, bool voicePreferred)> PromptForRecipeAsync(
    RecipeManager rm, VoiceInterface? voice, bool voicePreferred)
    {
        bool vPref = voicePreferred; // local copy we can mutate

        while (true)
        {
            string raw = await GetInputAsync(voice, vPref, "Say a recipe name (or say 'list recipes'): ");
            string input = Clean(raw);
            if (string.IsNullOrWhiteSpace(input)) continue;

            // allow some global commands at this stage too
            if (input == "voice off") { vPref = false; Console.WriteLine("Voice disabled (keyboard mode)."); continue; }
            if (input == "voice on")  { if (voice == null) Console.WriteLine("Voice not available."); else { vPref = true; Console.WriteLine("Voice enabled."); } continue; }
            if (input == "help")      { Console.WriteLine("Say a recipe name, e.g., 'chocolate chip cookies'. You can also say 'list recipes'."); continue; }
            if (input == "list recipes") { ListRecipesIfAvailable(); continue; }
            if (input.StartsWith("load ")) input = Clean(input.Substring(5)); // allow "load <name>" here too

            // Try to load it
            rm.LoadRecipe(input);
            var first = rm.GetCurrentStep();
            if (!first.StartsWith("No recipe steps found", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("First step:");
                Console.WriteLine(first);
                return (input, vPref); // success → return recipe and (possibly updated) voice pref
            }

            Console.WriteLine("Couldn’t find that recipe. Try again or say 'list recipes'.");
        }
    }
}
