namespace KodiNet.Infrastructure.Parsing;

/// <summary>
/// Path resolution for the private storage server.<br />
/// Takes a root path (e.g. "/kodi") and resolves relative or virtual paths to absolute paths under that root.
/// </summary>
public sealed class StoragePathResolver(string rootPath)
{
    private readonly string _root = rootPath.TrimEnd('/');

    /// <summary>
    /// Resolves a relative or virtual path to an absolute path under RootPath.<br />
    /// If the provided path is already absolute (starts with /), it is returned as is.
    /// </summary>
    public string Resolve(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "/" || path == "root")
            return _root.Length > 0 ? _root : "/";

        // Path is already absolute and under RootPath → return as is
        if (path.StartsWith('/') && (_root.Length == 0 || path.StartsWith(_root)))
            return path;

        // Relative path → prefix with RootPath
        return $"{_root}/{path.TrimStart('/')}";
    }
}