using KodiNet.Domain.Enums;

namespace KodiNet.Application.DTOs;

/// <param name="StoragePath"> Absolute path on the private server.</param>
public record TransferOperationDto(
    Guid           Id,
    string         FileName,
    string         StoragePath,
    int            TargetPiId,
    string         TargetPiName,
    TransferStatus Status,
    double         ProgressPercent,
    bool           RestartAfter,
    string?        ErrorMessage,
    DateTime       CreatedAt);

/// <summary>
/// Mass transfer request.<br />
/// The private server uses a configured API Key
/// on the server side, without dependency on the user session.
/// </summary>
/// <param name="StoragePaths">Absolute paths on the private server.</param>
public record MassTransferRequest(
    IReadOnlyList<string> StoragePaths,
    IReadOnlyList<string> FileNames,
    IReadOnlyList<int>    TargetPiIds,
    bool                  RestartAfter,
    bool                  Overwrite);
