internal static class HelpText
{
    public static void WriteTo(IUserPrompt prompt)
    {
        prompt.WriteLine("SyncSourseAndDestinationFolders");
        prompt.WriteLine("Short executable name: ssad.exe");
        prompt.WriteLine();
        prompt.WriteLine("Usage:");
        prompt.WriteLine("  ssad.exe");
        prompt.WriteLine("  ssad.exe -s \"C:\\Sourse\" -d \"C:\\Dest1\" -d \"C:\\Dest2\"");
        prompt.WriteLine("  ssad.exe s:C:\\Sourse dest:C:\\Dest1");
        prompt.WriteLine("  ssad.exe -p");
        prompt.WriteLine("  ssad.exe preset:anything");
        prompt.WriteLine("  ssad.exe --help");
        prompt.WriteLine();
        prompt.WriteLine("Supported sourse keys:");
        prompt.WriteLine("  -s  -source  -sourse  s:  source:  sourse:");
        prompt.WriteLine("Supported preset keys:");
        prompt.WriteLine("  -p  -preset  p:  preset:");
        prompt.WriteLine("Supported destination keys:");
        prompt.WriteLine("  -d  -dest  -destination  d:  dest:  destination:");
        prompt.WriteLine();
        prompt.WriteLine("Behavior:");
        prompt.WriteLine("  1. Copy all files from sourse to all destinations if files are different.");
        prompt.WriteLine("  2. Watch sourse and destinations for create, change, delete, and rename.");
        prompt.WriteLine("  3. If a destination changes first, update sourse and then sync other destinations.");
        prompt.WriteLine("  4. Save presets into sync-settings.json near the executable.");
        prompt.WriteLine("  5. Any preset key shows saved presets and exits.");
        prompt.WriteLine();
        prompt.WriteLine("Press Ctrl+C to stop monitoring.");
    }
}
