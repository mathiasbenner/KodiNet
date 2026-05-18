using KodiNet.Domain.Enums;

namespace KodiNet.Application.DTOs;

public sealed record StorageEntryDto(
        string Path,
        string Name,
        bool IsFolder,
        long SizeBytes,
        DateTime LastModified);