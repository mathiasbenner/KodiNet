using KodiNet.Application.DTOs;

namespace KodiNet.Application.Interfaces;

/// <summary>
/// Application service for admin theme management (Owner only)
/// </summary>
public interface IThemeManagementService
{
    Task<IReadOnlyList<AppThemeDto>>    GetAllAsync(CancellationToken ct = default);
    Task<AppThemeDto>                   CreateAsync(CreateThemeRequest request, CancellationToken ct = default);
    Task<AppThemeDto>                   UpdateAsync(UpdateThemeRequest request, CancellationToken ct = default);
    Task                                DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<AppThemeDto>>    ImportFromCsvAsync(CsvThemeImportRequest request, CancellationToken ct = default);
    Task<byte[]>                        ExportIntoCsvBytesAsync(CancellationToken ct = default);
}