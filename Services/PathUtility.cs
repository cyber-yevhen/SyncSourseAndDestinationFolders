internal static class PathUtility
{
    public static string NormalizePath(string path)
    {
        return TrimTrailingSeparators(Path.GetFullPath(Unquote(path)));
    }

    public static bool PathsEqual(string left, string right)
    {
        return string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSameOrChildPath(string parent, string candidate)
    {
        var normalizedParent = NormalizePath(parent);
        var normalizedCandidate = NormalizePath(candidate);

        if (string.Equals(normalizedParent, normalizedCandidate, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var prefix = normalizedParent + Path.DirectorySeparatorChar;
        return normalizedCandidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    public static string FormatRelativePath(string relativePath)
    {
        var normalized = relativePath
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/')
            .TrimStart('/');

        return $"/{normalized}";
    }

    private static string Unquote(string value)
    {
        return value.Trim().Trim('"');
    }

    private static string TrimTrailingSeparators(string path)
    {
        var root = Path.GetPathRoot(path);
        if (string.Equals(path, root, StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
