internal sealed record SyncEvent(
    WatchRootType RootType,
    string RootPath,
    ChangeKind Kind,
    string Path,
    string? OldPath = null);
