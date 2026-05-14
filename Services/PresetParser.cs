internal static class PresetParser
{
    public static bool TryParse(string text, int presetCount, out int presetIndex)
    {
        presetIndex = -1;
        var value = text.Trim();

        if (value.Contains(':'))
        {
            var parts = value.Split(':', 2);
            var key = parts[0].Trim();
            var rawIndex = parts[1].Trim();

            if (!key.Equals("preset", StringComparison.OrdinalIgnoreCase) &&
                !key.Equals("p", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!int.TryParse(rawIndex, out var index) || index < 1 || index > presetCount)
            {
                return false;
            }

            presetIndex = index - 1;
            return true;
        }

        if (!int.TryParse(value, out var directIndex) || directIndex < 1 || directIndex > presetCount)
        {
            return false;
        }

        presetIndex = directIndex - 1;
        return true;
    }
}
