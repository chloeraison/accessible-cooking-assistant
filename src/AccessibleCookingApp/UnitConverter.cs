// Handles simple unit conversions (volume <-> volume, mass <-> mass).

/* 
**Outcome:**
This module enables:
-> Converting between millilitres, cups, tablespoons, teaspoons
-> Converting between grams and ounces
-> Normalising different ways of saying units (e.g. "grams" vs "g")

/*
**Plan:**
Purpose of this class is to give a quick way for users to say things like:
"convert 240 ml to cups" or "convert 4 oz to g" and get a clear answer back.

Avoiding more specific measurement conversions like "cups -> grams" (needs ingredient density).
Only support the basic, universal conversions that don’t change by ingredient.

**UnitConverter will be made up of...**
- Constants for safe conversion factors (ml per cup/tbsp/tsp, grams per ounce)
- A dictionary of aliases so "tablespoons" = "tbsp"
- A single Convert(double value, string fromUnit, string toUnit) method
    - Looks up the units
    - Applies the right factor if supported
    - Throws a friendly error if the conversion isn’t supported

**Example flow:**
User says: "convert 240 ml to cups"
-> Parse gives value=240, fromUnit="ml", toUnit="cups"
-> Convert returns 1.0
-> App speaks/prints: "240 ml = 1 cup"
*/


using System;
using System.Collections.Generic;

// MVP conversions only (no ingredient densities).
// Mass <-> mass, volume <-> volume.
public static class UnitConverter
{
    private const double MlPerCup = 240.0;
    private const double MlPerTbsp = 15.0;
    private const double MlPerTsp  = 5.0;
    private const double GramsPerOunce = 28.3495;

    // Normalising unit names so "teaspoons" == "tsp", etc.
    private static readonly Dictionary<string, string> Alias = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ml"] = "ml", ["millilitre"] = "ml", ["millilitres"] = "ml", ["mills"] = "ml", ["mill"] = "ml",
        ["cup"] = "cup", ["cups"] = "cup",
        ["tbsp"] = "tbsp", ["tablespoon"] = "tbsp", ["tablespoons"] = "tbsp",
        ["tsp"] = "tsp", ["teaspoon"] = "tsp", ["teaspoons"] = "tsp",
        ["g"] = "g", ["gram"] = "g", ["grams"] = "g",
        ["oz"] = "oz", ["ounce"] = "oz", ["ounces"] = "oz",
    };

    public static double Convert(double value, string fromRaw, string toRaw)
    {
        if (!Alias.TryGetValue(fromRaw, out var from) || !Alias.TryGetValue(toRaw, out var to))
            throw new NotSupportedException($"Unsupported units: {fromRaw} -> {toRaw}");

        // Mass
        if (from == "g"  && to == "oz") return value / GramsPerOunce;
        if (from == "oz" && to == "g")  return value * GramsPerOunce;

        // Volume
        if (from == "ml"  && to == "cup")  return value / MlPerCup;
        if (from == "cup" && to == "ml")   return value * MlPerCup;

        if (from == "ml"   && to == "tbsp") return value / MlPerTbsp;
        if (from == "tbsp" && to == "ml")   return value * MlPerTbsp;

        if (from == "ml"  && to == "tsp")  return value / MlPerTsp;
        if (from == "tsp" && to == "ml")   return value * MlPerTsp;

        // Intra-volume convenience
        if (from == "tsp"  && to == "tbsp") return (value * MlPerTsp) / MlPerTbsp;
        if (from == "tbsp" && to == "tsp")  return (value * MlPerTbsp) / MlPerTsp;

        // Intentionally NOT doing cups <-> grams (needs ingredient density).
        throw new NotSupportedException($"Unsupported conversion: {fromRaw} -> {toRaw}");
    }
}
