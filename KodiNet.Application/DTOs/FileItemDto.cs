namespace KodiNet.Application.DTOs;

public record FileItemDto(
    string Id,           // itemId SharePoint or SFTP path
    string Name,
    bool IsFolder,
    long SizeBytes,
    DateTime LastModified,
    string? ParentId);