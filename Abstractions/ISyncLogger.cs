internal interface ISyncLogger
{
    void FileCopied(FileSyncDirection direction, string relativePath);
    void Info(string message);
    void Error(string message);
}
