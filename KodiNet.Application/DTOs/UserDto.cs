using KodiNet.Domain.Entities;

namespace KodiNet.Application.DTOs;

public record UserDto(
    int Id,
    string DisplayName,
    string Email,
    DateTime LastLoginAt,
    ICollection<UserRole> UserRoles,
    bool CronNotificationsEnabled,
    string? NotificationEmail);

public record NewUserRequest(
    string MicrosoftOid,
    string DisplayName,
    string Email,
    DateTime LastLoginAt);