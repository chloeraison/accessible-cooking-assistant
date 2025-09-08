using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var recipeManager = new RecipeManager();
        var timers = new MultiTimer(); // uni note: many named timers (pasta, oven, glaze…)

        // voice: try to enable; if env/mic missing, keyboard still works
        VoiceInterface? voice = null;
        try { voice = new VoiceInterface(); }
        catch (Exception ex) { Console.WriteLine($"[Voice disabled: {ex.Message}]"); }

        bool voicePreferred = true; // say "voice off" / "voice on" to toggle during session

        // ~*~*~ choose recipe (voice-first with retries) ~*~*~
        string rawName = await GetInputAsync(voice, voicePreferred, "Say or type a recipe name: ");
        string name = Clean(rawName);
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("No recipe name provided.");
            return;
        }

        recipeManager.LoadRecipe(name);
        Console.WriteLine("First step:");
        Console.WriteLine(recipeManager.GetCurrentStep());

        // ~*~*~ main loop ~*~*~
        while (true)
        {
            // surface any timers that just finished
            var finished = timers.CleanupFinishedTimers();
            foreach (var f in finished) Console.WriteLine($"[{f}] finished!");

            string raw = await GetInputAsync(
                voice, voicePreferred,
                "\nCommand (load <name>, next, previous, print, timer, status, stop, stop all, voice on/off, end): "
            );

            string input = Clean(raw);

            switch (input)
            {
                // ~*~*~ recipe navigation ~*~*~
                case "next":
                    Console.WriteLine(recipeManager.NextStep());
                    break;

                case "previous":
                case "prev":
                    Console.WriteLine(recipeManager.PreviousStep());
                    break;

                case "print":
                    recipeManager.PrintAllSteps();
                    break;

                // ~*~*~ pivot recipe (load another without restarting) ~*~*~
                // Accepts "load <name>" by voice or typing
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

                // ~*~*~ timers: start/set ~*~*~
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

                // ~*~*~ timers: status ~*~*~
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

                // ~*~*~ timers: stop one ~*~*~
                case var s when s.StartsWith("stop ") && !s.Equals("stop all"):
                {
                    var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && timers.StopTimer(parts[1]))
                        Console.WriteLine($"Stopped '{parts[1]}'.");
                    else
                        Console.WriteLine("Specify a timer name to stop, e.g., 'stop pasta'.");
                    break;
                }

                // ~*~*~ timers: stop all ~*~*~
                case "stop all":
                    timers.StopAll();
                    Console.WriteLine("Stopped all timers.");
                    break;

                // ~*~*~ voice toggle ~*~*~
                case "voice off":
                    voicePreferred = false;
                    Console.WriteLine("Voice disabled (keyboard mode). Say 'voice on' to re-enable.");
                    break;

                case "voice on":
                    if (voice == null) Console.WriteLine("Voice not available (check .env/mic).");
                    else { voicePreferred = true; Console.WriteLine("Voice enabled (listening first)."); }
                    break;

                // ~*~*~ exit ~*~*~
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

    // ~*~*~*~*~*~*~*~*~*~*~*~ helpers ~*~*~*~*~*~*~*~*~*~*~*~

    /// <summary>
    /// Voice-first input with gentle retries, then keyboard fallback.
    /// uni note: feels “always listening” but won’t trap you if mic is quiet.
    /// </summary>
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

                Console.WriteLine("(didn't catch that — try again… or start typing to use the keyboard)");
            }
        }

        // keyboard fallback (or primary if voicePreferred=false)
        return Console.ReadLine() ?? string.Empty;
    }

    // Normalise: trim whitespace, strip trailing ASR punctuation, lower-case.
    static string Clean(string s) => s.Trim().Trim('"', '.', '!', '?').ToLowerInvariant();

    /// Parse natural-ish timer phrases into (name, minutes).
    /// Accepts:
    ///  - "timer 5"                 => name: "timer", minutes: 5
    ///  - "timer pasta 12"          => name: "pasta", minutes: 12
    ///  - "set pasta timer for 12 minutes"
    ///  - "start oven timer 0.5"    => 30 seconds
    static bool TryParseTimer(string input, out string name, out double minutes)
    {
        name = "timer";   // default label so there’s always something to show
        minutes = 0;

        // pattern 1: "set/start X timer for 12 minutes"
        var m = Regex.Match(
            input,
            @"^(set|start)\s+([\w\-]+)?\s*timer(?:\s+for)?\s+(\d+(?:\.\d+)?)\s*(min|mins|minutes|m)?$",
            RegexOptions.IgnoreCase
        );
        if (m.Success)
        {
            if (!string.IsNullOrWhiteSpace(m.Groups[2].Value))
                name = m.Groups[2].Value;
            minutes = double.Parse(m.Groups[3].Value);
            return minutes > 0;
        }

        // pattern 2: "timer 5" or "timer pasta 12"
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && parts[0] == "timer")
        {
            if (parts.Length == 2 && double.TryParse(parts[1], out var minsOnly))
            {
                minutes = minsOnly; name = "timer"; return minutes > 0;
            }

            if (parts.Length >= 3 && double.TryParse(parts[^1], out var minsEnd))
            {
                minutes = minsEnd;
                name = string.Join(' ', parts[1..^1]); // all words after 'timer' except the last number
                return minutes > 0;
            }
        }

        return false;
    }
}
