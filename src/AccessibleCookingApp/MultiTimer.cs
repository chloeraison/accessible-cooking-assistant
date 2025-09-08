using System;
using System.Collections.Generic;
using System.Linq;

public class MultiTimer
{
    // map: timer name -> when it should end (UTC, so it's timezone-safe).
    // uni note: using OrdinalIgnoreCase so "Pasta" and "pasta" are the same key
    private readonly Dictionary<string, DateTime> _timers =
        new(StringComparer.OrdinalIgnoreCase);

    // Start or restart a timer.
    // 'minutes' can be decimal (e.g., 0.5 = 30 sec) because some recipes can be speedy.
    public void StartTimer(string name, double minutes)
    {
        if (minutes <= 0) throw new ArgumentOutOfRangeException(nameof(minutes), "Timer must be > 0 minutes.");
        if (string.IsNullOrWhiteSpace(name)) name = "timer"; // uni note: default name so we always have *something*
        _timers[name] = DateTime.UtcNow.AddMinutes(minutes); // uni note: overwrite = "reset" behaviour
    }
    
    // Stop a single timer by name.
    // returns true if something actually got removed, false if nothing matched.
    public bool StopTimer(string name) => _timers.Remove(name);

    // Clears every timer
    public void StopAll() => _timers.Clear();

    // Quick check on whether we have a timer with this name
    public bool HasTimer(string name) => _timers.ContainsKey(name);

    // Status for ONE timer, formatted in a readable style.
    public string Status(string name)
    {
        if (!_timers.TryGetValue(name, out var end))
            return $"No timer called '{name}'.";
        return FormatStatusLine(name, end);
    }

    // Status for ALL timers, sorted by soonest to finish.
    // returns strings so the caller can just Console.WriteLine each one.
    public IEnumerable<string> StatusAll()
    {
        if (_timers.Count == 0) return new[] { "No active timers." };
        return _timers
            .OrderBy(kv => kv.Value) // earliest finishing first
            .Select(kv => FormatStatusLine(kv.Key, kv.Value));
    }

    // Call this every loop tick:
    // - finds finished timers
    // - removes them from the dictionary (so things don't pile up)
    // - returns the names that finished so UI can tell the user
    public List<string> CleanupFinishedTimers()
    {
        var now = DateTime.UtcNow;
        var finished = _timers.Where(kv => now >= kv.Value)
                              .Select(kv => kv.Key)
                              .ToList();
        foreach (var key in finished) _timers.Remove(key); // mutate AFTER listing them
        return finished;
    }

    // pretty-printer for a single timer line
    private static string FormatStatusLine(string name, DateTime endUtc)
    {
        var now = DateTime.UtcNow;
        if (now >= endUtc) return $"[{name}] finished!"; // caller may already remove it, but message reads well
        var rem = endUtc - now;

        // show minutes if >= 1, otherwise show seconds (rounded up so "0.1 sec" doesn't look odd)
        string pretty = rem.TotalMinutes >= 1
            ? $"{Math.Ceiling(rem.TotalMinutes)} min"
            : $"{Math.Ceiling(rem.TotalSeconds)} sec";

        return $"[{name}] {pretty} remaining.";
    }
}