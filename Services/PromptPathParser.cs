internal static class PromptPathParser
{
    public static bool TryParse(string input, PromptKind promptKind, out string path)
    {
        path = string.Empty;
        var trimmed = input.Trim();

        if (trimmed.Contains(':'))
        {
            var parts = trimmed.Split(':', 2);
            var key = parts[0].Trim();
            var value = parts[1].Trim();

            if (IsPromptKey(promptKind, key) && !string.IsNullOrWhiteSpace(value))
            {
                path = value.Trim().Trim('"');
                return true;
            }
        }

        var firstSpace = trimmed.IndexOf(' ');
        if (firstSpace > 0)
        {
            var key = trimmed[..firstSpace].Trim();
            var value = trimmed[(firstSpace + 1)..].Trim();

            if (IsPromptKey(promptKind, key) && !string.IsNullOrWhiteSpace(value))
            {
                path = value.Trim().Trim('"');
                return true;
            }
        }

        path = trimmed.Trim('"');
        return true;
    }

    private static bool IsPromptKey(PromptKind promptKind, string key)
    {
        return promptKind switch
        {
            PromptKind.Source => ArgumentParser.IsSourceKey(key),
            PromptKind.Destination => ArgumentParser.IsDestinationKey(key),
            _ => false
        };
    }
}
