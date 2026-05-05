namespace KodiNet.Application.DTOs;

public record UserNotificationPrefDto(
    int UserId,
    string UserDisplayName,
    string UserEmail,
    bool CronNotificationsEnabled,
    string? NotificationEmail);