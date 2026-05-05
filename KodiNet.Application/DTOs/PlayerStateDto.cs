using KodiNet.Domain.Enums;

namespace KodiNet.Application.DTOs;

public record PlayerStateDto(
    bool IsPlaying,
    string? CurrentFile,
    double PositionSeconds,
    double DurationSeconds,
    int Volume,
    RepeatMode Repeat);