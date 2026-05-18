namespace KodiNet.Domain.Interfaces.Services;

/// <summary>
/// Folder entry returned by the private storage server.<br />
/// The identifier of an item is its absolute path on the server<br />
/// (e.g., /videos/films/avatar.mp4).
/// </summary>
public record StorageEntry(
    string   Path,          // unique identifier = absolute path
    string   Name,
    bool     IsFolder,
    long     SizeBytes,
    DateTime LastModified);

/// <summary>
/// HTTP client for the private storage server.<br />
/// <br />
/// Expected REST API contract on the server side:<br />
///   GET    /api/files?path=/folder        → StorageEntry[]  (list)<br />
///   GET    /api/files/download?path=/file → octet-stream    (download)<br />
///   POST   /api/files/upload?path=/folder → StorageEntry    (upload multipart/form-data, field "file")<br />
///   POST   /api/files/folder?path=/folder → StorageEntry    (create folder)<br />
///   DELETE /api/files?path=/item          → 204             (delete)<br />
///   PATCH  /api/files/rename              → StorageEntry    (body JSON: {"path":..., "newName":...})<br />
///   PATCH  /api/files/move                → StorageEntry    (body JSON: {"path":..., "destFolder":...})<br />
/// <br />
/// Authentication: header X-Api-Key with the key configured in Storage:ApiKey.
/// </summary>
public interface IPrivateStorageClient
{
    long MaxUploadBytes { get; }
    Task<IReadOnlyList<StorageEntry>> ListAsync(string folderPath, CancellationToken ct = default);
    Task<Stream>        DownloadAsync(string filePath, CancellationToken ct = default);
    Task<StorageEntry>  UploadAsync(string folderPath, string fileName, Stream content, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<StorageEntry>  CreateFolderAsync(string folderPath, CancellationToken ct = default);
    Task                DeleteFileAsync(string path, CancellationToken ct = default);
    Task<StorageEntry>  RenameAsync(string path, string newName, CancellationToken ct = default);
    Task<StorageEntry>  MoveAsync(string path, string destFolder, CancellationToken ct = default);
}
