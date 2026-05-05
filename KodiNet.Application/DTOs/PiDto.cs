using KodiNet.Domain.Enums;

namespace KodiNet.Application.DTOs;

public record PiDto(
    int Id,
    string Name,
    string IpAddress,
    int EstablishmentId,
    string EstablishmentName,   // denormalized for display — avoids joins on the UI side
    string Location,
    string Model,
    int KodiPort,
    string VideoFolderPath,
    int SshPort);

public record PiStatusDto(
    int PiId,
    PiStatus Status,
    string? KodiVersion,
    double CpuPercent,
    long FreeMemoryBytes,
    double TemperatureCelsius,
    long FreeDiskBytes);

public record PiHttpCredentialsDto(
    int Id,
    string IpAddress,
    int KodiPort,
    string KodiLogin,
    string KodiPassword);

public record PiDetailDto(PiDto Pi, PiStatusDto Status, PlayerStateDto? Player);