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
using System.IO;
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

        // load profile early
        var profile = ProfileStore.Load("default");
        bool voicePreferred = profile.VoicePreferred;
        double scaleFactor = profile.ScaleFactor;        // This affects how prints are *announced*
        string preferredUnits = profile.PreferredUnits;  // "metric"|"imperial"
        string currentRecipe = "";

        // Try to enable voice; fallback to keyboard if unavailable
        VoiceInterface? voice = null;
        try { voice = new VoiceInterface(); }
        catch (Exception ex) { Console.WriteLine($"[Voice disabled: {ex.Message}]"); }

        // bool voicePreferred = true; // toggle with "voice on/off"

        // ~*~ Step 1: pick a recipe ~*~
        var pick = await PromptForRecipeAsync(recipeManager, voice, voicePreferred);
        currentRecipe = pick.recipe;
        voicePreferred = pick.voicePreferred;
        profile.VoicePreferred = voicePreferred; // keep profile in sync
        ProfileStore.Save(profile);

        // ~*~ Step 2: main loop for commands ~*~
        while (true)
        {
            // check timers on every loop
            var finished = timers.CleanupFinishedTimers();
            foreach (var f in finished) Console.WriteLine($"[{f}] finished!");

            string raw = await GetInputAsync(
                voice, voicePreferred,
                $"\n[{currentRecipe}] Command (load/search/next/previous/repeat/print/timer/status/stop/stop all/convert/scale/units/profile/mock iot/voice on/off/help/end): "
            );

            string input = Clean(raw);
            LogUsage(currentRecipe, raw, input);

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
                    if (Math.Abs(scaleFactor - 1.0) > 0.0001) Console.WriteLine($"(Scaled to {scaleFactor:0.##}x)");
                    recipeManager.PrintAllSteps();
                    break;

                case "list recipes":
                    ListRecipesIfAvailable();
                    break;

                // ===== synonyms (because I kept saying some words instead of others on testing) =====
                case "again": // "repeat" alias
                    Console.WriteLine(recipeManager.GetCurrentStep());
                    break;

                case "back":  // "previous" alias
                    Console.WriteLine(recipeManager.PreviousStep());
                    break;

                // ===== search recipes (test data) =====
                case var s when s.StartsWith("search "):
                    {
                        var term = input.Substring(7).Trim();
                        var matches = SearchRecipes(term);
                        if (matches.Count == 0) Console.WriteLine("No matches.");
                        else { Console.WriteLine("Matches:"); foreach (var m in matches) Console.WriteLine($"- {m}"); }
                        break;
                    }

                // ===== scale portions (announce multiplier on print) =====
                case var s when s.StartsWith("scale "):
                    {
                        if (TryParseScale(input, out var factor))
                        {
                            scaleFactor = factor;
                            profile.ScaleFactor = scaleFactor; ProfileStore.Save(profile);
                            Console.WriteLine($"Scaled to {scaleFactor:0.##}× (applies when printing/reading ingredients).");
                        }
                        else Console.WriteLine("Try: scale 2x   |   scale 0.5x");
                        break;
                    }

                // ===== units preference (metric/imperial) =====
                case var s when s.StartsWith("units "):
                    {
                        var u = input.Substring(6).Trim();
                        if (u is "metric" or "imperial")
                        {
                            preferredUnits = u;
                            profile.PreferredUnits = preferredUnits; ProfileStore.Save(profile);
                            Console.WriteLine($"Units set to {preferredUnits}.");
                        }
                        else Console.WriteLine("Try: units metric  |  units imperial");
                        break;
                    }

                // ===== profile save/load =====
                case var s when s.StartsWith("profile save"):
                    {
                        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var name = parts.Length >= 3 ? parts[2] : "default";
                        profile.Name = name;
                        profile.PreferredUnits = preferredUnits;
                        profile.VoicePreferred = voicePreferred;
                        profile.ScaleFactor = scaleFactor;
                        ProfileStore.Save(profile);
                        Console.WriteLine($"Profile '{name}' saved.");
                        break;
                    }
                case var s when s.StartsWith("profile load"):
                    {
                        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var name = parts.Length >= 3 ? parts[2] : "default";
                        profile = ProfileStore.Load(name);
                        preferredUnits = profile.PreferredUnits;
                        voicePreferred = profile.VoicePreferred;
                        scaleFactor = profile.ScaleFactor;
                        Console.WriteLine($"Profile '{name}' loaded. (units={preferredUnits}, voice={(voicePreferred ? "on" : "off")}, scale={scaleFactor:0.##}x)");
                        break;
                    }

                // ===== mock IoT (pretend to control appliances) =====
                case var s when s.StartsWith("preheat ") || s.StartsWith("set oven"):
                    {
                        if (TryParseMockOven(input, preferredUnits, out var tempC, out var display))
                            Console.WriteLine($"[MOCK] Oven set to {display}.");
                        else Console.WriteLine("Try: preheat oven 180 C   |   set oven 350 F");
                        break;
                    }
                case var s when s.StartsWith("oven off") || s.StartsWith("turn off oven"):
                    Console.WriteLine("[MOCK] Oven turned off.");
                    break;
                case var s when s.StartsWith("start hob ") || s.StartsWith("set hob "):
                    {
                        if (TryParseMockHob(input, out var ring, out var level))
                            Console.WriteLine($"[MOCK] Hob ring {ring} set to level {level}.");
                        else Console.WriteLine("Try: start hob 2 level 6");
                        break;
                    }
                case var s when s.StartsWith("stop hob"):
                    {
                        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var ring = parts.Length >= 3 ? parts[2] : "?";
                        Console.WriteLine($"[MOCK] Hob ring {ring} off.");
                        break;
                    }

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
                    Console.WriteLine("=== Accessible Cooking Assistant — Help ===");
                    Console.WriteLine();
                    Console.WriteLine("Recipes");
                    Console.WriteLine("- list recipes        : show available recipes (test mode)");
                    Console.WriteLine("- search <term>       : find recipes by name (e.g., 'search pie')");
                    Console.WriteLine("- load <name>         : load a recipe (e.g., 'load fish pie')");
                    Console.WriteLine();
                    Console.WriteLine("Navigation");
                    Console.WriteLine("- next                : go to the next step");
                    Console.WriteLine("- previous | back     : go to the previous step");
                    Console.WriteLine("- repeat | again      : speak the current step again");
                    Console.WriteLine("- print               : print all steps (announces scale if set)");
                    Console.WriteLine();
                    Console.WriteLine("Timers");
                    Console.WriteLine("- timer <mins>                    : start a default timer (e.g., 'timer 5')");
                    Console.WriteLine("- timer <name> <mins>             : named timer (e.g., 'timer pasta 12')");
                    Console.WriteLine("- timer <name> <secs> seconds     : seconds support (e.g., 'timer tea 30 seconds')");
                    Console.WriteLine("- set <name> timer for <n> <unit> : natural phrasing (e.g., 'set glaze timer for 8 minutes')");
                    Console.WriteLine("- status [name]                   : show all timers or one timer (e.g., 'status pasta')");
                    Console.WriteLine("- stop <name>                     : stop a named timer (e.g., 'stop oven')");
                    Console.WriteLine("- stop all                        : stop all timers");
                    Console.WriteLine();
                    Console.WriteLine("Conversions (units)");
                    Console.WriteLine("- convert <value> <from> to <to>  : e.g., 'convert 240 ml to cups', 'convert 4 oz to g'");
                    Console.WriteLine("  Supported: ml, cup(s), tbsp, tsp, g, oz (no cups↔grams without an ingredient).");
                    Console.WriteLine();
                    Console.WriteLine("Portion scaling & preferences");
                    Console.WriteLine("- scale <factor>x      : set portion multiplier (e.g., 'scale 2x', 'scale 0.5x')");
                    Console.WriteLine("- units metric|imperial: choose display preference for mock temperatures");
                    Console.WriteLine("- profile save <name>  : save current settings (units/voice/scale)");
                    Console.WriteLine("- profile load <name>  : load saved settings");
                    Console.WriteLine();
                    Console.WriteLine("Mock kitchen controls (demo only)");
                    Console.WriteLine("- preheat oven <temp> [C|F]  : e.g., 'preheat oven 180 c', 'set oven 350 f'");
                    Console.WriteLine("- oven off                   : turn off oven (mock)");
                    Console.WriteLine("- start hob <ring> level <n> : e.g., 'start hob 2 level 6' (0-9)");
                    Console.WriteLine("- stop hob <ring>            : e.g., 'stop hob 2'");
                    Console.WriteLine();
                    Console.WriteLine("Voice & exit");
                    Console.WriteLine("- voice on | voice off  : toggle listening-first mode");
                    Console.WriteLine("- end | exit            : quit the app");
                    Console.WriteLine();
                    Console.WriteLine("Tip: punctuation from speech (e.g, 'Next.') is cleaned automatically.");
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

            var numStr = m.Groups[3].Value;
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

    // parse phrases like "convert 240 ml to cups", "convert 2 mills to tbsp", "convert 4 oz to g"
    static bool TryParseConvert(string input, out double value, out string from, out string to)
    {
        value = 0; from = ""; to = "";
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4 || parts[0] != "convert") return false;

        // find the numeric part (first token after 'convert' that parses)
        int numIndex = -1;
        for (int i = 1; i < parts.Length; i++)
        {
            if (double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                numIndex = i;
                break;
            }
        }
        if (numIndex == -1) return false;

        // expect something like: convert <num> <fromUnit> to <toUnit>
        if (numIndex + 2 < parts.Length && parts[numIndex + 2] == "to")
        {
            from = parts[numIndex + 1];
            if (numIndex + 3 < parts.Length) to = parts[numIndex + 3];
            return true;
        }

        // fallback: try to detect "convert <num> <from> to <to>" without strict spacing
        int toIdx = Array.IndexOf(parts, "to");
        if (toIdx > numIndex && toIdx + 1 < parts.Length)
        {
            from = parts[numIndex + 1];
            to = parts[toIdx + 1];
            return true;
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

    // writes "UTC | recipe=... | cmd="raw" | parsed="...""
    static void LogUsage(string currentRecipe, string raw, string parsed)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(dir);
            var line = $"{DateTime.UtcNow:O} | recipe={currentRecipe} | cmd=\"{raw}\" | parsed=\"{parsed}\"";
            File.AppendAllText(Path.Combine(dir, "usage.log"), line + Environment.NewLine);
        }
        catch { /* ignore for MVP */ }
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
            if (input == "voice on") { if (voice == null) Console.WriteLine("Voice not available."); else { vPref = true; Console.WriteLine("Voice enabled."); } continue; }
            if (input == "help") { Console.WriteLine("Say a recipe name, e.g., 'chocolate chip cookies'. You can also say 'list recipes'."); continue; }
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

    // search names in TestRecipeData (reflection so we don't depend directly)
    static System.Collections.Generic.List<string> SearchRecipes(string term)
    {
        var results = new System.Collections.Generic.List<string>();
        try
        {
            var field = typeof(TestRecipeData).GetField("Recipes",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (field?.GetValue(null) is System.Collections.IDictionary dict)
            {
                foreach (var key in dict.Keys)
                {
                    var name = key?.ToString() ?? "";
                    if (name.Contains(term, StringComparison.OrdinalIgnoreCase))
                        results.Add(name);
                }
            }
        }
        catch { }
        return results;
    }

    // "scale 2x" or "scale 0.5x"
    static bool TryParseScale(string input, out double factor)
    {
        factor = 1.0;
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;
        var raw = parts[1].Replace("x", "", StringComparison.OrdinalIgnoreCase);
        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out factor) && factor > 0;
    }

    // mock oven: "preheat oven 180 c", "set oven 350 f"
    static bool TryParseMockOven(string input, string preferredUnits, out int tempC, out string display)
    {
        tempC = 0; display = "";
        // grab the first number and possible unit after it
        var m = Regex.Match(input, @"(\d{2,3})\s*([cCfF]?)");
        if (!m.Success) return false;
        int val = int.Parse(m.Groups[1].Value);
        string unit = m.Groups[2].Value.ToLowerInvariant();

        if (unit == "f") tempC = (int)Math.Round((val - 32) * 5.0 / 9.0);
        else tempC = val; // assume Celsius if blank or 'c'

        // show in user's preferred units
        if (preferredUnits == "imperial")
        {
            var f = (int)Math.Round(tempC * 9.0 / 5.0 + 32);
            display = $"{f} °F";
        }
        else display = $"{tempC} °C";
        return true;
    }

    // mock hob: "start hob 2 level 6" or "set hob 1 4"
    static bool TryParseMockHob(string input, out int ring, out int level)
    {
        ring = 0; level = 0;
        // accept "start hob <ring> level <level>" or "set hob <ring> <level>"
        var m1 = Regex.Match(input, @"hob\s+(\d+)\s+level\s+(\d+)", RegexOptions.IgnoreCase);
        var m2 = Regex.Match(input, @"hob\s+(\d+)\s+(\d+)", RegexOptions.IgnoreCase);
        var m = m1.Success ? m1 : m2.Success ? m2 : null;
        if (m == null) return false;
        ring = int.Parse(m.Groups[1].Value);
        level = int.Parse(m.Groups[2].Value);
        ring = Math.Clamp(ring, 1, 6);   // assume 6 rings max
        level = Math.Clamp(level, 0, 9); // assume 0..9 levels
        return true;
    }

}
