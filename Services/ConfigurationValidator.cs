internal static class ConfigurationValidator
{
    public static void Validate(SyncConfiguration configuration)
    {
        if (!Directory.Exists(configuration.SourcePath))
        {
            throw new DirectoryNotFoundException($"Sourse folder does not exist: {configuration.SourcePath}");
        }

        foreach (var destination in configuration.DestinationPaths)
        {
            if (PathUtility.PathsEqual(configuration.SourcePath, destination))
            {
                throw new InvalidOperationException("Sourse and destination cannot be the same folder.");
            }

            if (PathUtility.IsSameOrChildPath(configuration.SourcePath, destination) ||
                PathUtility.IsSameOrChildPath(destination, configuration.SourcePath))
            {
                throw new InvalidOperationException("Sourse and destination folders cannot be inside each other.");
            }
        }

        for (var index = 0; index < configuration.DestinationPaths.Count; index++)
        {
            for (var innerIndex = index + 1; innerIndex < configuration.DestinationPaths.Count; innerIndex++)
            {
                var left = configuration.DestinationPaths[index];
                var right = configuration.DestinationPaths[innerIndex];

                if (PathUtility.PathsEqual(left, right))
                {
                    throw new InvalidOperationException("Duplicate destination folders are not allowed.");
                }

                if (PathUtility.IsSameOrChildPath(left, right) || PathUtility.IsSameOrChildPath(right, left))
                {
                    throw new InvalidOperationException("Destination folders cannot be inside each other.");
                }
            }
        }
    }
}
