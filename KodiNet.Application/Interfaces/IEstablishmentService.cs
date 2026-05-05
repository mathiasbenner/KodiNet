using KodiNet.Application.DTOs;

namespace KodiNet.Application.Interfaces;

public interface IEstablishmentService
{
    Task<IReadOnlyList<EstablishmentDto>> GetAllAsync(CancellationToken ct = default);
    Task<EstablishmentDto> CreateAsync(string name, string? address, CancellationToken ct = default);
    Task<EstablishmentDto> UpdateAsync(int id, string name, string? address, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<EstablishmentDto>> ImportFromCsvAsync(CsvEstablishmentImportRequest request, CancellationToken ct = default);
    Task<byte[]> ExportIntoCsvBytesAsync(CancellationToken ct = default);
}
