namespace KodiNet.Application.DTOs;

// ─── CSV Pi Analysis ──────────────────────────────────────────────────────────

public record CsvPiRow(
    int RowIndex,
    string Name,
    string IpAddress,
    string Location,
    string Model,
    string KodiUser,
    string KodiPassword,
    int KodiPort,
    string SshUser,
    string SshPassword,
    int SshPort,
    string VideoFolderPath,
    string? ParseError);

public record CsvPiImportRequest(
    IReadOnlyList<CsvPiRow> Rows,
    IReadOnlyDictionary<int, int> RowEstablishmentMap);

// ─── CSV Establishment Analysis ───────────────────────────────────────────────

public record CsvEstablishmentRow(
    int RowIndex,
    string Name,
    string? Address,
    string? ParseError);

public record CsvEstablishmentImportRequest(
    IReadOnlyList<CsvEstablishmentRow> Rows);