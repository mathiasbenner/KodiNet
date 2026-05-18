using KodiNet.Domain.Enums;

namespace KodiNet.Application.DTOs;

public record StorageActivityDto(
    Guid                Id,
    StorageActivityKind Kind,
    string              Description,
    TransferStatus      Status,
    string?             ErrorMessage,
    DateTime            CreatedAt);