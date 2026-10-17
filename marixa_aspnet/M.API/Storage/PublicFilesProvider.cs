using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace M.API.Storage;

/// <summary>Excludes legacy private documents after filesystem path normalization.</summary>
public sealed class PublicFilesProvider(IFileProvider inner, string privateDirectory) : IFileProvider
{
    private readonly string _privateRoot = Path.GetFullPath(privateDirectory).TrimEnd(Path.DirectorySeparatorChar);

    public IFileInfo GetFileInfo(string subpath)
    {
        var file = inner.GetFileInfo(subpath);
        return IsPrivate(file.PhysicalPath) ? new NotFoundFileInfo(subpath) : file;
    }

    public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;
    public IChangeToken Watch(string filter) => inner.Watch(filter);

    private bool IsPrivate(string? path)
    {
        if (path == null) return false;
        var resolved = Path.GetFullPath(path);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(resolved, _privateRoot, comparison)
            || resolved.StartsWith(_privateRoot + Path.DirectorySeparatorChar, comparison);
    }
}
