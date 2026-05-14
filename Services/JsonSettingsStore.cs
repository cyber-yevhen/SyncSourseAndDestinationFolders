using System.Text.Json;

internal sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public JsonSettingsStore(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public async Task<List<SyncPreset>> LoadAsync()
    {
        if (!File.Exists(_settingsPath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_settingsPath);
        var presets = await JsonSerializer.DeserializeAsync<List<SyncPreset>>(stream, JsonOptions);
        return presets ?? [];
    }

    public async Task SaveOrMergeAsync(SyncConfiguration configuration)
    {
        var presets = await LoadAsync();
        var existing = presets.FirstOrDefault(p =>
            string.Equals(PathUtility.NormalizePath(p.Sourse), configuration.SourcePath, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            presets.Add(new SyncPreset
            {
                Sourse = configuration.SourcePath,
                Destinations = configuration.DestinationPaths
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            });
        }
        else
        {
            existing.Sourse = configuration.SourcePath;
            existing.Destinations = existing.Destinations
                .Concat(configuration.DestinationPaths)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, presets, JsonOptions);
    }
}
