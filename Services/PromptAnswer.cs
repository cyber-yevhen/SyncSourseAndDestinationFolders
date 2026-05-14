internal static class PromptAnswer
{
    public static bool IsPositive(string value)
    {
        var normalized = value.Trim();
        return normalized.Equals("y", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("yeah", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("ok", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsNegative(string value)
    {
        var normalized = value.Trim();
        return normalized.Equals("n", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("no", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("nope", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("-no", StringComparison.OrdinalIgnoreCase);
    }
}
