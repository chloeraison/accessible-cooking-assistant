using System;
using System.Collections.Generic;

public static class TestRecipeData
{
    // Case-insensitive lookup
    public static readonly Dictionary<string, List<string>> Recipes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["chocolate chip cookies"] = new()
            {
                "Preheat oven to 180°C (350°F).",
                "Cream butter and sugar.",
                "Beat in eggs and vanilla.",
                "Stir in flour; fold in chocolate chips.",
                "Bake ~12 minutes."
            },
            ["enchiladas"] = new()
            {
                "Preheat oven to 190°C (375°F).",
                "Fill tortillas with chicken and cheese.",
                "Roll, place seam-down in dish.",
                "Cover with sauce and cheese.",
                "Bake 15–20 min until bubbling."
            },
            ["fish pie"] = new()
            {
                "Heat oven to 200°C (400°F).",
                "Poach mixed fish in milk.",
                "Make roux; add milk for sauce.",
                "Fold in fish and peas; top with mash.",
                "Bake 25–30 min until golden."
            },
            ["bruschetta"] = new()
            {
                "Toast bread slices.",
                "Rub with garlic, drizzle olive oil.",
                "Top with chopped tomatoes & basil.",
                "Season and serve."
            },
            ["full english breakfast"] = new()
            {
                "Fry sausages until cooked.",
                "Add bacon; cook to preference.",
                "Cook mushrooms and tomatoes.",
                "Fry eggs; toast bread; warm beans.",
                "Plate up and serve."
            }
        };
}
