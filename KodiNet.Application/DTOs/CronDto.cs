namespace KodiNet.Application.DTOs;

public record CronJobResultDto(
    DateTime    ExecutedAt,
    int         PisChecked,
    int         PisRestarted,
    int         PisFailed,
    IReadOnlyList<CronPiResultDto> Details);

public record CronPiResultDto(
    int     PiId,
    string  PiName,
    string  PreviousStatus,  // "Idle", "Paused", "Stopped"
    bool    Restarted,
    string? ErrorMessage);
