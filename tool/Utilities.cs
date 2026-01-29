using LangSwap.translation;
using System;

namespace LangSwap.tool;

// Utility functions for string manipulation and common operations
public static class Utilities
{
    // Capitalize the first character of a string
    public static string CapitalizeFirst(string text)
    {
        // Return as-is if text is null or empty
        if (string.IsNullOrEmpty(text))
            return text;

        // Capitalize first character
        return char.ToUpper(text[0]) + text[1..];
    }

    // Remove [a] [p] tags from a string
    public static string RemoveAPTags(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        return text.Replace("[a]", "").Replace("[/a]", "").Replace("[p]", "").Replace("[/p]", "");
    }
}
