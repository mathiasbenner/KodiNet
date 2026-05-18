using KodiNet.Application.Exceptions;
using KodiNet.Domain.Interfaces.Services;
using Renci.SshNet;
using SshAuthEx = Renci.SshNet.Common.SshAuthenticationException;
using SshConnEx = Renci.SshNet.Common.SshConnectionException;

namespace KodiNet.Infrastructure.Clients;

// ─── SFTP ─────────────────────────────────────────────────────────────────────

public sealed class SftpFileClient : ISftpFileClient
{
    public async Task<IReadOnlyList<SftpFileEntry>> ListAsync(
        string ip, int port, string user, string password, string remotePath, CancellationToken ct = default)
    {
        try
        {
            return await Task.Run(() =>
            {
                using var client = CreateClient(ip, port, user, password);
                client.Connect();
                var files = client.ListDirectory(remotePath);
                return (IReadOnlyList<SftpFileEntry>)files
                    .Where(f => f.Name is not "." and not "..")
                    .Select(f => new SftpFileEntry(f.Name, f.FullName, f.IsDirectory, f.Length, f.LastWriteTime))
                    .OrderByDescending(f => f.IsDirectory)
                    .ThenBy(f => f.Name)
                    .ToList();
            }, ct);
        }
        catch (SshAuthEx) { throw new SshAuthenticationException(ip); }
        catch (SshConnEx) { throw new PiUnreachableException(ip); }
    }

    public async Task UploadStreamAsync(
        string ip, int port, string user, string password,
        Stream source, string remotePath,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        try
        {
            // SSH.NET reads synchronous stream (Task.Run context).
            // IBrowserFile.OpenReadStream() and some HTTP streams do not support synchronous players + prior copy in MemoryStream.
            Stream uploadSource;
            if (source.CanRead && !source.GetType().Name.Contains("Pipe")
                               && source is MemoryStream)
            {
                uploadSource = source;  // already a MemoryStream, no need to copy
            }
            else
            {
                var ms = new MemoryStream();
                await source.CopyToAsync(ms, ct);
                ms.Position = 0;
                uploadSource = ms;
            }

            await Task.Run(() =>
            {
                using var client = CreateClient(ip, port, user, password);
                client.Connect();

                long totalBytes = uploadSource.CanSeek ? uploadSource.Length : -1;
                long uploaded   = 0;

                client.UploadFile(uploadSource, remotePath, true, bytesUploaded =>
                {
                    uploaded = (long)bytesUploaded;
                    if (progress is not null && totalBytes > 0)
                        progress.Report((double)uploaded / totalBytes * 100);
                });
            }, ct);

            if (!ReferenceEquals(uploadSource, source))
                await uploadSource.DisposeAsync();
        }
        catch (SshAuthEx) { throw new SshAuthenticationException(ip); }
        catch (SshConnEx) { throw new PiUnreachableException(ip); }
    }

    public async Task DeleteAsync(string ip, int port, string user, string password, string remotePath, CancellationToken ct = default)
    {
        try
        {
            await Task.Run(() =>
            {
                using var client = CreateClient(ip, port, user, password);
                client.Connect();
                if (client.GetAttributes(remotePath).IsDirectory)
                    client.DeleteDirectory(remotePath);
                else
                    client.DeleteFile(remotePath);
            }, ct);
        }
        catch (SshAuthEx) { throw new SshAuthenticationException(ip); }
        catch (SshConnEx) { throw new PiUnreachableException(ip); }
    }

    public async Task RenameAsync(string ip, int port, string user, string password, string oldPath, string newPath, CancellationToken ct = default)
    {
        try
        {
            await Task.Run(() =>
            {
                using var client = CreateClient(ip, port, user, password);
                client.Connect();
                client.RenameFile(oldPath, newPath);
            }, ct);
        }
        catch (SshAuthEx) { throw new SshAuthenticationException(ip); }
        catch (SshConnEx) { throw new PiUnreachableException(ip); }
    }

    private static SftpClient CreateClient(string ip, int port, string user, string password)
        => new(ip, port, user, password);
}