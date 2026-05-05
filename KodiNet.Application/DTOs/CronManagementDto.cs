namespace KodiNet.Application.DTOs;

public record CronJobDto(
    int Id,
    string Name,
    string Description,
    string Schedule,
    bool IsEnabled,
    DateTime? LastRunAt);

public record CronJobExecutionDto(
    int Id,
    int CronJobId,
    DateTime StartedAt,
    DateTime? FinishedAt,
    bool Success,
    string? ErrorMessage,
    CronJobResultDto? Result);   // deserialized from ResultJson

public record UpdateCronJobRequest(int Id, string Schedule, bool IsEnabled);

public record AppSettingDto(string Key, string Value, bool IsSensitive, string? InputHint = null);
public record UpdateAppSettingRequest(string Key, string Value);