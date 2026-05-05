namespace KodiNet.Application.DTOs;

public record CreatePiRequest(
    string Name,
    string IpAddress,
    int EstablishmentId,
    string Location,
    string Model,
    string KodiUser,
    string KodiPassword,
    int KodiPort,
    string SshUser,
    string SshPassword,
    int SshPort,
    string VideoFolderPath);

public record UpdatePiRequest(
    int Id,
    string Name,
    string IpAddress,
    int EstablishmentId,
    string Location,
    string Model,
    string? KodiUser,
    string? KodiPassword,
    int KodiPort,
    string? SshUser,
    string? SshPassword,
    int SshPort,
    string VideoFolderPath);