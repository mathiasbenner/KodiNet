using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Domain.Enums;
using KodiNet.Domain.Interfaces.Services;
using KodiNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace KodiNet.Infrastructure.Services;

/// <summary>
/// Singleton service managing the background transfer operation queue.
/// Uses IDbContextFactory to create a dedicated context per operation
/// (a singleton cannot share a scoped DbContext).
/// </summary>
public sealed class TransferQueueService(
    IDbContextFactory<AppDbContext> dbFactory,
    IServiceScopeFactory            scopeFactory,
    ILogger<TransferQueueService>   logger) : ITransferQueueService
{
    private readonly ConcurrentDictionary<Guid, TransferOperation> _ops = new();
    private readonly SemaphoreSlim _concurrencyLimiter = new(AppConstants.Transfer.MaxConcurrentTransfers); // max concurrent transfers

    public event Action? StateChanged;

    public IReadOnlyList<TransferOperationDto> Operations =>
        _ops.Values
            .OrderByDescending(o => o.CreatedAt)
            .Select(MapToDto)
            .ToList();

    public void EnqueueMassTransfer(MassTransferRequest request)
    {
        foreach (var (storagePath, fileName) in request.StoragePaths.Zip(request.FileNames))
        {
            foreach (var piId in request.TargetPiIds)
            {
                var op = new TransferOperation(
                    id:           Guid.NewGuid(),
                    storagePath:  storagePath,
                    fileName:     fileName,
                    targetPiId:   piId,
                    restartAfter: request.RestartAfter,
                    overwrite:    request.Overwrite);
                _ops[op.Id] = op;
                _ = RunOperationAsync(op);
            }
        }
        NotifyStateChanged();
    }

    public void ClearCompleted()
    {
        foreach (var op in _ops.Values.Where(o => o.Status is TransferStatus.Done or TransferStatus.Error))
            _ops.TryRemove(op.Id, out _);
        NotifyStateChanged();
    }

    public async Task CancelAsync(Guid operationId)
    {
        if (_ops.TryGetValue(operationId, out var op))
        {
            op.Cancel();
            NotifyStateChanged();
        }
        await Task.CompletedTask;
    }

    // ── Executing an operation ─────────────────────────────────────────────

    private async Task RunOperationAsync(TransferOperation op)
    {
        await _concurrencyLimiter.WaitAsync(op.CancellationToken);
        try
        {
            op.Status = TransferStatus.Running;
            NotifyStateChanged();

            await using var scope = scopeFactory.CreateAsyncScope();
            var sftp       = scope.ServiceProvider.GetRequiredService<ISftpFileClient>();
            var kodi       = scope.ServiceProvider.GetRequiredService<IKodiClient>();
            var storage    = scope.ServiceProvider.GetRequiredService<IPrivateStorageClient>();
            var encryption = scope.ServiceProvider.GetRequiredService<ICredentialEncryption>();

            await using var db = await dbFactory.CreateDbContextAsync(op.CancellationToken);
            var pi = await db.RaspberryPis.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == op.TargetPiId, op.CancellationToken)
                ?? throw new InvalidOperationException($"Pi {op.TargetPiId} introuvable.");

            op.TargetPiName = pi.Name;
            var sshUser    = encryption.Decrypt(pi.SshUserEncrypted);
            var sshPass    = encryption.Decrypt(pi.SshPasswordEncrypted);
            var kodiUser   = encryption.Decrypt(pi.KodiUserEncrypted);
            var kodiPass   = encryption.Decrypt(pi.KodiPasswordEncrypted);
            var remotePath = $"{pi.VideoFolderPath.TrimEnd('/')}/{op.FileName}";

            // Download from the private server via HTTP
            await using var sourceStream = await storage.DownloadAsync(op.StoragePath, op.CancellationToken);

            var progress = new Progress<double>(pct => { op.ProgressPercent = pct; NotifyStateChanged(); });

            // Upload to the Pi via SFTP
            await sftp.UploadStreamAsync(pi.IpAddress, pi.SshPort, sshUser, sshPass,
                sourceStream, remotePath, progress, op.CancellationToken);

            op.Status = TransferStatus.Done;
            op.ProgressPercent = 100;

            if (op.RestartAfter)
            {
                try
                {
                    await kodi.PlayFolderAsync(
                        pi.IpAddress, pi.KodiPort, kodiUser, kodiPass,
                        pi.VideoFolderPath, op.CancellationToken);
                }
                catch (Exception ex)
                {
                    // The file was successfully transferred — display a warning without downgrading to Error
                    op.ErrorMessage = $"Transfer succeeded — Kodi restart failed: {ex.Message}";
                    logger.LogWarning(ex, "Kodi restart failed after transfer {File} → Pi {PiId}", op.FileName, op.TargetPiId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            op.Status = TransferStatus.Error;
            op.ErrorMessage = "Operation canceled.";
        }
        catch (Exception ex)
        {
            op.Status = TransferStatus.Error;
            op.ErrorMessage = ex.Message;
            logger.LogError(ex, "Transfer error {File} to Pi {PiId}", op.FileName, op.TargetPiId);
        }
        finally { _concurrencyLimiter.Release(); NotifyStateChanged(); }
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();

    private static TransferOperationDto MapToDto(TransferOperation op) => new(
        op.Id, op.FileName, op.StoragePath, op.TargetPiId,
        op.TargetPiName ?? $"Pi #{op.TargetPiId}",
        op.Status, op.ProgressPercent, op.RestartAfter, op.ErrorMessage, op.CreatedAt);

    // ── Internal class for the operation ─────────────────────────────────────────

    private sealed class TransferOperation(
        Guid id, string storagePath, string fileName,
        int targetPiId, bool restartAfter, bool overwrite)
    {
        public Guid           Id              { get; } = id;
        public string         StoragePath     { get; } = storagePath;
        public string         FileName        { get; } = fileName;
        public int            TargetPiId      { get; } = targetPiId;
        public string?        TargetPiName    { get; set; }
        public bool           RestartAfter    { get; } = restartAfter;
        public bool           Overwrite       { get; } = overwrite;
        public TransferStatus Status          { get; set; } = TransferStatus.Pending;
        public double         ProgressPercent { get; set; }
        public string?        ErrorMessage    { get; set; }
        public DateTime       CreatedAt       { get; } = DateTime.UtcNow;

        private readonly CancellationTokenSource _cts = new();
        public CancellationToken CancellationToken => _cts.Token;
        public void Cancel() => _cts.Cancel();
    }
}
