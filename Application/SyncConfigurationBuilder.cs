internal sealed class SyncConfigurationBuilder
{
    private readonly IUserPrompt _prompt;

    public SyncConfigurationBuilder(IUserPrompt prompt)
    {
        _prompt = prompt;
    }

    public SyncConfiguration Build(ParsedArguments parsedArguments, List<SyncPreset> presets)
    {
        var source = parsedArguments.SourcePath is null ? null : PathUtility.NormalizePath(parsedArguments.SourcePath);
        var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? presetAppliedSource = null;

        foreach (var destination in parsedArguments.DestinationPaths)
        {
            destinations.Add(PathUtility.NormalizePath(destination));
        }

        while (true)
        {
            RemoveMissingDestinations(destinations);

            if (source is null || !Directory.Exists(source))
            {
                if (source is not null)
                {
                    _prompt.WriteLine("Sourse folder does not exist.");
                }

                source = PromptForSource(presets);
                presetAppliedSource = null;
            }

            if (!string.Equals(presetAppliedSource, source, StringComparison.OrdinalIgnoreCase))
            {
                ApplyPresetIfRequested(source, destinations, presets);
                presetAppliedSource = source;
            }

            if (destinations.Count == 0)
            {
                CollectDestinations(destinations);
                RemoveMissingDestinations(destinations);
                continue;
            }

            ConfirmStart(source, destinations);
            return new SyncConfiguration(source, destinations.ToList());
        }
    }

    private string PromptForSource(List<SyncPreset> presets)
    {
        while (true)
        {
            _prompt.WriteLine();
            _prompt.WriteLine("Sourse is not set.");

            if (presets.Count > 0)
            {
                _prompt.WriteLine("Saved presets:");

                for (var index = 0; index < presets.Count; index++)
                {
                    var preset = presets[index];
                    _prompt.WriteLine($"  {index + 1}. {preset.Sourse} ({preset.Destinations.Count} destination(s))");
                }

                _prompt.Write("Enter sourse path or preset:<number>: ");
            }
            else
            {
                _prompt.Write("Enter sourse path: ");
            }

            var input = _prompt.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (ArgumentParser.IsHelpToken(input))
            {
                HelpText.WriteTo(_prompt);
                continue;
            }

            if (PresetParser.TryParse(input, presets.Count, out var presetIndex))
            {
                var preset = presets[presetIndex];
                _prompt.WriteLine($"Using preset for '{preset.Sourse}'.");
                return PathUtility.NormalizePath(preset.Sourse);
            }

            if (PromptPathParser.TryParse(input, PromptKind.Source, out var path))
            {
                return PathUtility.NormalizePath(path);
            }

            _prompt.WriteLine("Invalid sourse input.");
        }
    }

    private void ApplyPresetIfRequested(string source, HashSet<string> destinations, List<SyncPreset> presets)
    {
        var matchingPreset = presets.FirstOrDefault(p => PathUtility.PathsEqual(source, p.Sourse));
        if (matchingPreset is null)
        {
            return;
        }

        _prompt.WriteLine($"Existing preset found for '{source}':");
        foreach (var destination in matchingPreset.Destinations)
        {
            _prompt.WriteLine($"  Destination: {destination}");
        }

        var shouldUsePreset = destinations.Count == 0
            ? AskYesNo("Use existing preset? [yes]/[no]: ", defaultValue: true)
            : AskYesNo("Add preset destinations too? [yes]/[no]: ", defaultValue: true);

        if (!shouldUsePreset)
        {
            return;
        }

        foreach (var destination in matchingPreset.Destinations)
        {
            destinations.Add(PathUtility.NormalizePath(destination));
        }

        _prompt.WriteLine($"Loaded {matchingPreset.Destinations.Count} destination(s) from preset.");
    }

    private void CollectDestinations(HashSet<string> destinations)
    {
        while (true)
        {
            var destination = PromptForDestinationOrStop();
            if (destination is null)
            {
                if (destinations.Count > 0)
                {
                    return;
                }

                _prompt.WriteLine("At least one destination folder is required.");
                continue;
            }

            destinations.Add(destination);
        }
    }

    private void RemoveMissingDestinations(HashSet<string> destinations)
    {
        var missingDestinations = destinations
            .Where(static destination => !Directory.Exists(destination))
            .ToList();

        foreach (var missingDestination in missingDestinations)
        {
            destinations.Remove(missingDestination);
            _prompt.WriteLine($"Removed missing destination: {missingDestination}");
        }
    }

    private void ConfirmStart(string source, HashSet<string> destinations)
    {
        while (true)
        {
            _prompt.WriteLine();
            _prompt.WriteLine("Ready to start:");
            _prompt.WriteLine($"  Sourse: {source}");

            foreach (var destination in destinations.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase))
            {
                _prompt.WriteLine($"  Destination: {destination}");
            }

            if (AskYesNo("Start monitoring now? [yes]/[no]: ", defaultValue: true))
            {
                return;
            }
        }
    }

    private string? PromptForDestinationOrStop()
    {
        while (true)
        {
            _prompt.Write("Add a destination folder? Expected input: [a path]/[n]/[no]: ");

            var input = _prompt.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (IsDestinationInputStop(input))
            {
                return null;
            }

            if (ArgumentParser.IsHelpToken(input))
            {
                HelpText.WriteTo(_prompt);
                continue;
            }

            if (PromptPathParser.TryParse(input, PromptKind.Destination, out var path))
            {
                var normalizedPath = PathUtility.NormalizePath(path);
                if (!Directory.Exists(normalizedPath))
                {
                    _prompt.WriteLine("Folder does not exist.");
                    continue;
                }

                return normalizedPath;
            }

            _prompt.WriteLine("Invalid destination input.");
        }
    }

    private static bool IsDestinationInputStop(string input)
    {
        var normalized = input.Trim();
        return normalized.Equals("n", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("no", StringComparison.OrdinalIgnoreCase);
    }

    private bool AskYesNo(string prompt, bool defaultValue)
    {
        while (true)
        {
            _prompt.Write(prompt);
            var input = _prompt.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                return defaultValue;
            }

            if (PromptAnswer.IsPositive(input))
            {
                return true;
            }

            if (PromptAnswer.IsNegative(input))
            {
                return false;
            }

            _prompt.WriteLine("Please answer yes or no.");
        }
    }
}
