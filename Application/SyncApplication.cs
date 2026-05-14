internal sealed class SyncApplication
{
    private readonly SyncConfigurationBuilder _configurationBuilder;
    private readonly SyncEngine _syncEngine;
    private readonly ISyncLogger _logger;
    private readonly ISettingsStore _settingsStore;
    private readonly IUserPrompt _userPrompt;

    public SyncApplication(
        ISettingsStore settingsStore,
        SyncConfigurationBuilder configurationBuilder,
        SyncEngine syncEngine,
        ISyncLogger logger,
        IUserPrompt userPrompt)
    {
        _settingsStore = settingsStore;
        _configurationBuilder = configurationBuilder;
        _syncEngine = syncEngine;
        _logger = logger;
        _userPrompt = userPrompt;
    }

    public async Task<int> RunAsync(string[] args)
    {
        var parsedArguments = ArgumentParser.Parse(args);

        if (parsedArguments.ShowHelp)
        {
            HelpText.WriteTo(_userPrompt);
            return 0;
        }

        var presets = await _settingsStore.LoadAsync();
        if (parsedArguments.ShowPresets)
        {
            ShowPresets(presets);
            return 0;
        }

        var configuration = _configurationBuilder.Build(parsedArguments, presets);
        var shouldSaveSettings = true;

        ConfigurationValidator.Validate(configuration);

        if (shouldSaveSettings)
        {
            await _settingsStore.SaveOrMergeAsync(configuration);
        }

        using var cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        _logger.Info(string.Empty);
        _logger.Info("Initial sync started.");
        await _syncEngine.RunAsync(configuration, cancellationTokenSource.Token);

        return 0;
    }

    private void ShowPresets(List<SyncPreset> presets)
    {
        if (presets.Count == 0)
        {
            _userPrompt.WriteLine("No saved presets.");
            return;
        }

        _userPrompt.WriteLine("Saved presets:");
        for (var index = 0; index < presets.Count; index++)
        {
            var preset = presets[index];
            _userPrompt.WriteLine($"{index + 1}. Sourse: {preset.Sourse}");

            foreach (var destination in preset.Destinations)
            {
                _userPrompt.WriteLine($"   Destination: {destination}");
            }
        }
    }
}
