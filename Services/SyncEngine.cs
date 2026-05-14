using System.Collections.Concurrent;
using System.Threading.Channels;

internal sealed class SyncEngine
{
    private static readonly TimeSpan SuppressDuration = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan RecentSourceDuration = TimeSpan.FromSeconds(5);

    private readonly Channel<SyncEvent> _channel = Channel.CreateUnbounded<SyncEvent>();
    private readonly ISyncLogger _logger;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _recentSourceChanges =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _suppressedPaths =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<FileSystemWatcher> _watchers = [];

    public SyncEngine(ISyncLogger logger)
    {
        _logger = logger;
    }

    public async Task RunAsync(SyncConfiguration configuration, CancellationToken cancellationToken)
    {
        foreach (var destination in configuration.DestinationPaths)
        {
            Directory.CreateDirectory(destination);
        }

        InitialSync(configuration);

        using var registration = cancellationToken.Register(() => _channel.Writer.TryComplete());
        StartWatchers(configuration);
        _logger.Info("Monitoring started. Press Ctrl+C to stop.");

        try
        {
            await foreach (var syncEvent in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                ProcessEvent(configuration, syncEvent);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            foreach (var watcher in _watchers)
            {
                watcher.Dispose();
            }
        }
    }

    private void InitialSync(SyncConfiguration configuration)
    {
        foreach (var directory in Directory.EnumerateDirectories(configuration.SourcePath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(configuration.SourcePath, directory);

            foreach (var destinationRoot in configuration.DestinationPaths)
            {
                Directory.CreateDirectory(Path.Combine(destinationRoot, relativePath));
            }
        }

        foreach (var file in Directory.EnumerateFiles(configuration.SourcePath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(configuration.SourcePath, file);
            var copiedToAnyDestination = false;

            foreach (var destinationRoot in configuration.DestinationPaths)
            {
                var destinationFile = Path.Combine(destinationRoot, relativePath);
                copiedToAnyDestination |= CopyFileIfDifferent(file, destinationFile);
            }

            if (copiedToAnyDestination)
            {
                _logger.FileCopied(FileSyncDirection.SourceToDestination, relativePath);
            }
        }
    }

    private void StartWatchers(SyncConfiguration configuration)
    {
        StartWatcher(configuration.SourcePath, WatchRootType.Source);

        foreach (var destination in configuration.DestinationPaths)
        {
            StartWatcher(destination, WatchRootType.Destination);
        }
    }

    private void StartWatcher(string rootPath, WatchRootType rootType)
    {
        var watcher = new FileSystemWatcher(rootPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName
                | NotifyFilters.DirectoryName
                | NotifyFilters.LastWrite
                | NotifyFilters.Size
                | NotifyFilters.CreationTime
        };

        watcher.Created += (_, args) => Enqueue(new SyncEvent(rootType, rootPath, ChangeKind.Created, args.FullPath));
        watcher.Changed += (_, args) => Enqueue(new SyncEvent(rootType, rootPath, ChangeKind.Changed, args.FullPath));
        watcher.Deleted += (_, args) => Enqueue(new SyncEvent(rootType, rootPath, ChangeKind.Deleted, args.FullPath));
        watcher.Renamed += (_, args) => Enqueue(new SyncEvent(rootType, rootPath, ChangeKind.Renamed, args.FullPath, args.OldFullPath));
        watcher.Error += (_, args) =>
        {
            _logger.Error($"Watcher error on {rootPath}: {args.GetException()?.Message ?? "Unknown watcher error"}");
        };

        watcher.EnableRaisingEvents = true;
        _watchers.Add(watcher);
    }

    private void Enqueue(SyncEvent syncEvent)
    {
        _channel.Writer.TryWrite(syncEvent);
    }

    private void ProcessEvent(SyncConfiguration configuration, SyncEvent syncEvent)
    {
        CleanupExpiredTracking();

        if (syncEvent.Kind == ChangeKind.Changed && Directory.Exists(syncEvent.Path))
        {
            return;
        }

        if (IsSuppressed(syncEvent.Path) || (syncEvent.OldPath is not null && IsSuppressed(syncEvent.OldPath)))
        {
            return;
        }

        try
        {
            switch (syncEvent.RootType)
            {
                case WatchRootType.Source:
                    ApplySourceEvent(configuration, syncEvent);
                    break;
                case WatchRootType.Destination:
                    ApplyDestinationEvent(configuration, syncEvent);
                    break;
            }
        }
        catch (Exception exception)
        {
            _logger.Error($"Sync error: {exception.Message}");
        }
    }

    private void ApplySourceEvent(SyncConfiguration configuration, SyncEvent syncEvent)
    {
        var relativePath = Path.GetRelativePath(configuration.SourcePath, syncEvent.Path);
        MarkRecentSourceChange(relativePath);

        switch (syncEvent.Kind)
        {
            case ChangeKind.Created:
            case ChangeKind.Changed:
                var copiedToAnyDestination = false;

                foreach (var destinationRoot in configuration.DestinationPaths)
                {
                    var destinationPath = Path.Combine(destinationRoot, relativePath);
                    copiedToAnyDestination |= SyncPath(syncEvent.Path, destinationPath);
                }

                if (copiedToAnyDestination)
                {
                    _logger.FileCopied(FileSyncDirection.SourceToDestination, relativePath);
                }

                return;

            case ChangeKind.Deleted:
                foreach (var destinationRoot in configuration.DestinationPaths)
                {
                    DeletePath(Path.Combine(destinationRoot, relativePath));
                }

                return;

            case ChangeKind.Renamed:
                if (syncEvent.OldPath is null)
                {
                    return;
                }

                var oldRelativePath = Path.GetRelativePath(configuration.SourcePath, syncEvent.OldPath);
                MarkRecentSourceChange(oldRelativePath);
                var copiedAfterRename = false;

                foreach (var destinationRoot in configuration.DestinationPaths)
                {
                    var oldDestinationPath = Path.Combine(destinationRoot, oldRelativePath);
                    var newDestinationPath = Path.Combine(destinationRoot, relativePath);
                    RenamePath(oldDestinationPath, newDestinationPath);
                    copiedAfterRename |= SyncPath(syncEvent.Path, newDestinationPath);
                }

                if (copiedAfterRename)
                {
                    _logger.FileCopied(FileSyncDirection.SourceToDestination, relativePath);
                }

                return;
        }
    }

    private void ApplyDestinationEvent(SyncConfiguration configuration, SyncEvent syncEvent)
    {
        var relativePath = Path.GetRelativePath(syncEvent.RootPath, syncEvent.Path);
        var sourcePath = Path.Combine(configuration.SourcePath, relativePath);

        var sourceRecentlyChanged = WasSourceRecentlyChanged(relativePath)
            || (syncEvent.OldPath is not null &&
                WasSourceRecentlyChanged(Path.GetRelativePath(syncEvent.RootPath, syncEvent.OldPath)));

        switch (syncEvent.Kind)
        {
            case ChangeKind.Created:
            case ChangeKind.Changed:
                if (sourceRecentlyChanged)
                {
                    if (SyncPath(sourcePath, syncEvent.Path))
                    {
                        _logger.FileCopied(FileSyncDirection.SourceToDestination, relativePath);
                    }

                    return;
                }

                if (!PathExists(sourcePath))
                {
                    return;
                }

                var copiedToSource = SyncPath(syncEvent.Path, sourcePath);
                if (copiedToSource)
                {
                    _logger.FileCopied(FileSyncDirection.DestinationToSource, relativePath);
                }

                MarkRecentSourceChange(relativePath);
                SyncSourceToOtherDestinations(configuration, relativePath, syncEvent.RootPath);
                return;

            case ChangeKind.Deleted:
                if (sourceRecentlyChanged)
                {
                    if (SyncPath(sourcePath, syncEvent.Path))
                    {
                        _logger.FileCopied(FileSyncDirection.SourceToDestination, relativePath);
                    }

                    return;
                }

                DeletePath(sourcePath);
                MarkRecentSourceChange(relativePath);
                DeleteFromOtherDestinations(configuration, relativePath, syncEvent.RootPath);
                return;

            case ChangeKind.Renamed:
                if (syncEvent.OldPath is null)
                {
                    return;
                }

                var oldRelativePath = Path.GetRelativePath(syncEvent.RootPath, syncEvent.OldPath);
                var oldSourcePath = Path.Combine(configuration.SourcePath, oldRelativePath);

                if (sourceRecentlyChanged)
                {
                    if (SyncPath(oldSourcePath, syncEvent.OldPath))
                    {
                        _logger.FileCopied(FileSyncDirection.SourceToDestination, oldRelativePath);
                    }

                    if (SyncPath(sourcePath, syncEvent.Path))
                    {
                        _logger.FileCopied(FileSyncDirection.SourceToDestination, relativePath);
                    }

                    return;
                }

                if (!PathExists(oldSourcePath))
                {
                    return;
                }

                RenamePath(oldSourcePath, sourcePath);
                if (SyncPath(syncEvent.Path, sourcePath))
                {
                    _logger.FileCopied(FileSyncDirection.DestinationToSource, relativePath);
                }

                MarkRecentSourceChange(oldRelativePath);
                MarkRecentSourceChange(relativePath);
                RenameSourceInOtherDestinations(configuration, oldRelativePath, relativePath, syncEvent.RootPath);
                return;
        }
    }

    private void SyncSourceToOtherDestinations(SyncConfiguration configuration, string relativePath, string skipDestinationRoot)
    {
        var sourcePath = Path.Combine(configuration.SourcePath, relativePath);
        var copiedToAnyDestination = false;

        foreach (var destinationRoot in configuration.DestinationPaths)
        {
            if (string.Equals(destinationRoot, skipDestinationRoot, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var destinationPath = Path.Combine(destinationRoot, relativePath);
            copiedToAnyDestination |= SyncPath(sourcePath, destinationPath);
        }

        if (copiedToAnyDestination)
        {
            _logger.FileCopied(FileSyncDirection.SourceToDestination, relativePath);
        }
    }

    private void DeleteFromOtherDestinations(SyncConfiguration configuration, string relativePath, string skipDestinationRoot)
    {
        foreach (var destinationRoot in configuration.DestinationPaths)
        {
            if (string.Equals(destinationRoot, skipDestinationRoot, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            DeletePath(Path.Combine(destinationRoot, relativePath));
        }
    }

    private void RenameSourceInOtherDestinations(
        SyncConfiguration configuration,
        string oldRelativePath,
        string newRelativePath,
        string skipDestinationRoot)
    {
        var sourcePath = Path.Combine(configuration.SourcePath, newRelativePath);
        var copiedToAnyDestination = false;

        foreach (var destinationRoot in configuration.DestinationPaths)
        {
            if (string.Equals(destinationRoot, skipDestinationRoot, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var oldDestinationPath = Path.Combine(destinationRoot, oldRelativePath);
            var newDestinationPath = Path.Combine(destinationRoot, newRelativePath);
            RenamePath(oldDestinationPath, newDestinationPath);
            copiedToAnyDestination |= SyncPath(sourcePath, newDestinationPath);
        }

        if (copiedToAnyDestination)
        {
            _logger.FileCopied(FileSyncDirection.SourceToDestination, newRelativePath);
        }
    }

    private bool SyncPath(string sourcePath, string destinationPath)
    {
        if (File.Exists(sourcePath))
        {
            return CopyFileIfDifferent(sourcePath, destinationPath);
        }

        if (Directory.Exists(sourcePath))
        {
            Suppress(destinationPath);
            Directory.CreateDirectory(destinationPath);
            return false;
        }

        DeletePath(destinationPath);
        return false;
    }

    private bool CopyFileIfDifferent(string sourceFile, string destinationFile)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

        if (File.Exists(destinationFile) && FilesAreEqual(sourceFile, destinationFile))
        {
            return false;
        }

        Suppress(destinationFile);
        File.Copy(sourceFile, destinationFile, overwrite: true);
        File.SetLastWriteTimeUtc(destinationFile, File.GetLastWriteTimeUtc(sourceFile));
        return true;
    }

    private void DeletePath(string path)
    {
        if (File.Exists(path))
        {
            Suppress(path);
            File.Delete(path);
            return;
        }

        if (Directory.Exists(path))
        {
            Suppress(path);
            Directory.Delete(path, recursive: true);
        }
    }

    private void RenamePath(string oldPath, string newPath)
    {
        if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (File.Exists(oldPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
            Suppress(oldPath);
            Suppress(newPath);

            if (File.Exists(newPath))
            {
                File.Delete(newPath);
            }

            File.Move(oldPath, newPath);
            return;
        }

        if (Directory.Exists(oldPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
            Suppress(oldPath);
            Suppress(newPath);

            if (Directory.Exists(newPath))
            {
                Directory.Delete(newPath, recursive: true);
            }

            Directory.Move(oldPath, newPath);
        }
    }

    private static bool PathExists(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }

    private static bool FilesAreEqual(string leftPath, string rightPath)
    {
        var leftInfo = new FileInfo(leftPath);
        var rightInfo = new FileInfo(rightPath);

        if (leftInfo.Length != rightInfo.Length)
        {
            return false;
        }

        const int bufferSize = 1024 * 64;
        using var leftStream = File.OpenRead(leftPath);
        using var rightStream = File.OpenRead(rightPath);

        var leftBuffer = new byte[bufferSize];
        var rightBuffer = new byte[bufferSize];

        while (true)
        {
            var leftRead = leftStream.Read(leftBuffer, 0, leftBuffer.Length);
            var rightRead = rightStream.Read(rightBuffer, 0, rightBuffer.Length);

            if (leftRead != rightRead)
            {
                return false;
            }

            if (leftRead == 0)
            {
                return true;
            }

            for (var index = 0; index < leftRead; index++)
            {
                if (leftBuffer[index] != rightBuffer[index])
                {
                    return false;
                }
            }
        }
    }

    private void Suppress(string path)
    {
        _suppressedPaths[path] = DateTimeOffset.UtcNow.Add(SuppressDuration);
    }

    private bool IsSuppressed(string path)
    {
        return _suppressedPaths.TryGetValue(path, out var expiresAt) && expiresAt > DateTimeOffset.UtcNow;
    }

    private void MarkRecentSourceChange(string relativePath)
    {
        _recentSourceChanges[relativePath] = DateTimeOffset.UtcNow.Add(RecentSourceDuration);
    }

    private bool WasSourceRecentlyChanged(string relativePath)
    {
        return _recentSourceChanges.TryGetValue(relativePath, out var expiresAt) && expiresAt > DateTimeOffset.UtcNow;
    }

    private void CleanupExpiredTracking()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var pair in _suppressedPaths)
        {
            if (pair.Value <= now)
            {
                _suppressedPaths.TryRemove(pair.Key, out _);
            }
        }

        foreach (var pair in _recentSourceChanges)
        {
            if (pair.Value <= now)
            {
                _recentSourceChanges.TryRemove(pair.Key, out _);
            }
        }
    }
}
