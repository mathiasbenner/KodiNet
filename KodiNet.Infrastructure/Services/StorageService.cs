using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Domain.Interfaces.Services;

namespace KodiNet.Infrastructure.Services;

/// <summary>
/// A Service for private HTTP storage.
/// Implementation via IPrivateStorageClient (HTTP + API Key).
/// </summary>
public sealed class StorageService(IPrivateStorageClient client) : IStorageService
{
    public async Task<IReadOnlyList<FileItemDto>> ListFolderAsync(string? folderPath = null, CancellationToken ct = default)
    {
        var entries = await client.ListAsync(folderPath ?? "/", ct);
        return entries.Select(MapToDto).ToList();
    }

    public async Task<FileItemDto> CreateFolderAsync(string parentPath, string name, CancellationToken ct = default)
    {
        var fullPath = $"{parentPath.TrimEnd('/')}/{name}";
        var entry    = await client.CreateFolderAsync(fullPath, ct);
        return MapToDto(entry);
    }

    public Task DeleteItemAsync(string path, CancellationToken ct = default)
        => client.DeleteFileAsync(path, ct);

    public async Task<FileItemDto> MoveItemAsync(string path, string destFolder, string? newName = null, CancellationToken ct = default)
    {
        var entry = await client.MoveAsync(path, destFolder, ct);
        if (newName is not null)
            entry = await client.RenameAsync(entry.Path, newName, ct);
        return MapToDto(entry);
    }

    public async Task<FileItemDto> RenameItemAsync(string path, string newName, CancellationToken ct = default)
    {
        var entry = await client.RenameAsync(path, newName, ct);
        return MapToDto(entry);
    }

    public Task<Stream> DownloadStreamAsync(string path, CancellationToken ct = default)
        => client.DownloadAsync(path, ct);

    public async Task<FileItemDto> UploadFileAsync(string folderPath, string fileName, Stream content, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var entry = await client.UploadAsync(folderPath, fileName, content, progress, ct);
        return MapToDto(entry);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static FileItemDto MapToDto(StorageEntry e) => new(
        Id: e.Path,          // the absolute path serves as the identifier
        Name: e.Name,
        IsFolder: e.IsFolder,
        SizeBytes: e.SizeBytes,
        LastModified: e.LastModified,
        ParentId: GetParentPath(e.Path));

    private static string? GetParentPath(string path)
    {
        var idx = path.TrimEnd('/').LastIndexOf('/');
        return idx > 0 ? path[..idx] : null;
    }
}
