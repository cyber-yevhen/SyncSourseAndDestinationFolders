internal sealed class ParsedArguments
{
    public List<string> DestinationPaths { get; } = [];
    public string? SourcePath { get; set; }
    public bool ShowPresets { get; set; }
    public bool ShowHelp { get; set; }
}
