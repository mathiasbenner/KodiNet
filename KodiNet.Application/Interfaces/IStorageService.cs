using KodiNet.Application.DTOs;

namespace KodiNet.Application.Interfaces;

/// <summary>Application service for the private HTTP storage server.</summary>
public interface IStorageService
{
    Task<IReadOnlyList<FileItemDto>> ListFolderAsync(string? folderPath = null, CancellationToken ct = default);
    Task<FileItemDto> CreateFolderAsync(string parentPath, string name, CancellationToken ct = default);
    Task              DeleteItemAsync(string path, CancellationToken ct = default);
    Task<FileItemDto> MoveItemAsync(string path, string destFolder, string? newName = null, CancellationToken ct = default);
    Task<FileItemDto> RenameItemAsync(string path, string newName, CancellationToken ct = default);
    Task<Stream>      DownloadStreamAsync(string path, CancellationToken ct = default);
    Task<FileItemDto> UploadFileAsync(string folderPath, string fileName, Stream content, IProgress<double>? progress = null, CancellationToken ct = default);
}
