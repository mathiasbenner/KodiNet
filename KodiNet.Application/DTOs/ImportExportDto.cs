namespace KodiNet.Application.DTOs;

public record PiExportRow(
    string Name,
    string IpAddress,
    string EstablishmentName,
    string Location,
    string Model,
    int KodiPort,
    int SshPort,
    string VideoFolderPath);

public record EstablishmentExportRow(
    string Name,
    string? Address);