internal sealed class ConsoleSyncLogger : ISyncLogger
{
    public void FileCopied(FileSyncDirection direction, string relativePath)
    {
        var prefix = direction == FileSyncDirection.SourceToDestination ? "[s >> d]" : "[s << d]";
        Console.WriteLine($"{GetTimestamp()} {prefix} {PathUtility.FormatRelativePath(relativePath)}");
    }

    public void Info(string message)
    {
        Console.WriteLine(message);
    }

    public void Error(string message)
    {
        Console.WriteLine($"{GetTimestamp()} [error] {message}");
    }

    private static string GetTimestamp()
    {
        return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]";
    }
}
