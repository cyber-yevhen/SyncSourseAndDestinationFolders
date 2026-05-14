internal static class ArgumentParser
{
    public static ParsedArguments Parse(string[] args)
    {
        var result = new ParsedArguments();

        for (var index = 0; index < args.Length; index++)
        {
            var current = args[index];

            if (IsHelpToken(current) || IsShortHelpToken(current))
            {
                result.ShowHelp = true;
                continue;
            }

            if (TryParseInline(current, out var key, out var value))
            {
                Apply(result, key, value);
                continue;
            }

            if (IsSourceKey(current) || IsDestinationKey(current) || IsPresetKey(current))
            {
                if (IsPresetKey(current))
                {
                    result.ShowPresets = true;
                }

                if (index + 1 >= args.Length)
                {
                    continue;
                }

                var next = args[++index];
                Apply(result, current, next);
            }
        }

        return result;
    }

    public static bool IsHelpToken(string token)
    {
        var value = token.Trim();
        var helpIndex = value.LastIndexOf("help", StringComparison.OrdinalIgnoreCase);

        return helpIndex >= 0
            && helpIndex == value.Length - 4
            && helpIndex <= 2;
    }

    private static bool IsShortHelpToken(string token)
    {
        var value = token.Trim();
        return value.Equals("-h", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/h", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/?", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseInline(string token, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        var separatorIndex = token.IndexOf(':');
        if (separatorIndex <= 0)
        {
            return false;
        }

        key = token[..separatorIndex];
        value = token[(separatorIndex + 1)..];

        return IsSourceKey(key) || IsDestinationKey(key) || IsPresetKey(key);
    }

    private static void Apply(ParsedArguments result, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (IsSourceKey(key))
        {
            result.SourcePath = value.Trim().Trim('"');
            return;
        }

        if (IsPresetKey(key))
        {
            result.ShowPresets = true;
            return;
        }

        if (IsDestinationKey(key))
        {
            result.DestinationPaths.Add(value.Trim().Trim('"'));
        }
    }

    public static bool IsSourceKey(string key)
    {
        var normalized = key.Trim().TrimStart('-', '/');
        return normalized.Equals("s", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("source", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("sourse", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDestinationKey(string key)
    {
        var normalized = key.Trim().TrimStart('-', '/');
        return normalized.Equals("d", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("dest", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("destination", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPresetKey(string key)
    {
        var normalized = key.Trim().TrimStart('-', '/');
        return normalized.Equals("p", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("preset", StringComparison.OrdinalIgnoreCase);
    }
}
