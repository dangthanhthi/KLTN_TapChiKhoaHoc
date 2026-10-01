namespace HuitJournal.Api.Infrastructure;

/// <summary>
/// Resolves uploaded content to durable App Service storage when configured, while
/// retaining the existing content-root layout for local and MonsterASP deployments.
/// </summary>
public static class UploadStoragePaths
{
    public static string GetRoot(string contentRoot)
    {
        var configuredRoot = Environment.GetEnvironmentVariable("HUIT_UPLOADS_PATH");
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            var expandedRoot = Environment.ExpandEnvironmentVariables(configuredRoot.Trim());
            return Path.GetFullPath(expandedRoot);
        }

        return Path.GetFullPath(Path.Combine(contentRoot, "Uploads"));
    }

    public static string? ResolveExistingFile(string contentRoot, string storedPath)
    {
        var relative = storedPath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(relative) || relative.Split(Path.DirectorySeparatorChar).Any(part => part == ".."))
            return null;

        var root = Path.GetFullPath(contentRoot);
        var uploadsRoot = GetRoot(root);
        var withoutUploadsPrefix = relative.StartsWith("Uploads" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? relative["Uploads".Length..].TrimStart(Path.DirectorySeparatorChar)
            : relative;

        var candidates = new[]
        {
            Path.Combine(root, relative),
            Path.Combine(root, "wwwroot", relative),
            Path.Combine(uploadsRoot, withoutUploadsPrefix),
            Path.Combine(uploadsRoot, relative)
        };

        foreach (var candidate in candidates.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var isWithinContentRoot = candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            var isWithinUploadsRoot = candidate.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            if ((isWithinContentRoot || isWithinUploadsRoot) && File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}
