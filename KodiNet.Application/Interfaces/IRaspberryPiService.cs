using KodiNet.Application.DTOs;

namespace KodiNet.Application.Interfaces;

public interface IRaspberryPiService
{
    Task<IReadOnlyList<PiDto>>  GetAllAsync(CancellationToken ct = default);
    Task<PiDto?>                GetByIdAsync(int id, CancellationToken ct = default);
    Task<PiHttpCredentialsDto?> GetCredentialsByIdAsync(int id, CancellationToken ct = default);
    Task<PiDto>                 CreateAsync(CreatePiRequest request, CancellationToken ct = default);
    Task<PiDto>                 UpdateAsync(UpdatePiRequest request, CancellationToken ct = default);
    Task                        DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<PiDto>>  ImportFromCsvAsync(CsvPiImportRequest request, CancellationToken ct = default);
    Task<byte[]>                ExportIntoCsvBytesAsync(CancellationToken ct = default);
    Task<string?>               GetKodiWebUrlAsync(int piId, CancellationToken ct = default);
}