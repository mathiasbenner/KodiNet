namespace KodiNet.Domain.Interfaces.Services;

public record SftpFileEntry(string Name, string FullPath, bool IsDirectory, long SizeBytes, DateTime LastModified);

public interface ISftpFileClient
{
    Task<IReadOnlyList<SftpFileEntry>> ListAsync(string ip, int port, string user, string password, string remotePath, CancellationToken ct = default);
    Task UploadStreamAsync(string ip, int port, string user, string password, Stream source, string remotePath, IProgress<double>? progress = null, CancellationToken ct = default);
    Task DeleteAsync(string ip, int port, string user, string password, string remotePath, CancellationToken ct = default);
    Task RenameAsync(string ip, int port, string user, string password, string oldPath, string newPath, CancellationToken ct = default);
}