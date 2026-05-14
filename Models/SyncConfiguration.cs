internal sealed class SyncConfiguration
{
    public SyncConfiguration(string sourcePath, List<string> destinationPaths)
    {
        SourcePath = sourcePath;
        DestinationPaths = destinationPaths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public List<string> DestinationPaths { get; }
    public string SourcePath { get; }
}
