// Stores and loads simple user preferences for the app (JSON-based).
//
// **Outcome**
// -> Remembers user settings between runs (units, voice preference, scale factor).
// -> Allows to save/load named profiles (e.g., "default", "demo").
// -> Keeps everything local (no database required).
//
// **Plan (top → bottom)**
// 1) UserProfile class: a tiny data bag with four fields:
//      - Name: profile name (e.g., "default")
//      - PreferredUnits: "metric" or "imperial" (affects temperature display, etc.)
//      - VoicePreferred: should the app listen first?
//      - ScaleFactor: portion scaling multiplier (e.g, 2x)
// 2) ProfileStore static helper:
//      - Figures out where profiles live on disk: ./profiles/<name>.json
//      - Load(name):
//          * Creates the folder if needed.
//          * If file exists -> read + parse JSON -> return profile (fallback to 'sane' defaults).
//          * If not -> return a new profile with the given name.
//      - Save(profile):
//          * Creates the folder if needed.
//          * Serialises profile to pretty JSON and writes it to disk.
//      - All file I/O is wrapped in try/catch so the app never crashes if disk access fails.
//        (This is MVP-friendly: if saving breaks, it still keeps running.)
// 
// **Example flow**
// - On startup: var profile = ProfileStore.Load("default");
// - When user says "units imperial" or "voice off": update profile fields and ProfileStore.Save(profile);
// - On next run: the choices are remembered.

using System;
using System.IO;
using System.Text.Json;

// minimal profile which can persist between runs.
public class UserProfile
{
    public string Name { get; set; } = "default";
    public string PreferredUnits { get; set; } = "metric"; // "metric" or "imperial"
    public bool VoicePreferred { get; set; } = true;
    public double ScaleFactor { get; set; } = 1.0;
}

public static class ProfileStore
{
    private static string ProfilesDir => Path.Combine(AppContext.BaseDirectory, "profiles");

    public static UserProfile Load(string name = "default")
    {
        try
        {
            Directory.CreateDirectory(ProfilesDir);
            var path = Path.Combine(ProfilesDir, $"{name}.json");
            if (!File.Exists(path)) return new UserProfile { Name = name };
            var json = File.ReadAllText(path);
            var profile = JsonSerializer.Deserialize<UserProfile>(json) ?? new UserProfile { Name = name };
            profile.Name = name;
            return profile;
        }
        catch { return new UserProfile { Name = name }; }
    }

    public static void Save(UserProfile p)
    {
        try
        {
            Directory.CreateDirectory(ProfilesDir);
            var path = Path.Combine(ProfilesDir, $"{p.Name}.json");
            var json = JsonSerializer.Serialize(p, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { /* ignore write errors for MVP */ }
    }
}
