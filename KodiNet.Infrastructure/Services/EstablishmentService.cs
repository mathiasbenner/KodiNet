using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Domain.Entities;
using KodiNet.Infrastructure.Data;
using KodiNet.Infrastructure.Serializers;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace KodiNet.Infrastructure.Services;

public sealed class EstablishmentService(IDbContextFactory<AppDbContext> dbFactory) : IEstablishmentService
{
    public async Task<IReadOnlyList<EstablishmentDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Establishments
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new EstablishmentDto(e.Id, e.Name, e.Address))
            .ToListAsync(ct);
    }

    public async Task<EstablishmentDto> CreateAsync(string name, string? address, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = new Establishment { Name = name.Trim(), Address = address?.Trim() };
        db.Establishments.Add(entity);
        await db.SaveChangesAsync(ct);
        return new EstablishmentDto(entity.Id, entity.Name, entity.Address);
    }

    public async Task<EstablishmentDto> UpdateAsync(int id, string name, string? address, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await db.Establishments.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Establishment {id} not found.");
        entity.Name = name.Trim();
        entity.Address = address?.Trim();
        await db.SaveChangesAsync(ct);
        return new EstablishmentDto(entity.Id, entity.Name, entity.Address);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var hasPis = await db.RaspberryPis.AnyAsync(p => p.EstablishmentId == id, ct);
        if (hasPis)
            throw new InvalidOperationException("Cannot delete an establishment that contains Pis.");
        await db.Establishments.Where(e => e.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyList<EstablishmentDto>> ImportFromCsvAsync(CsvEstablishmentImportRequest request, CancellationToken ct = default)
    {
        var results = new List<EstablishmentDto>();
        foreach(var row in request.Rows.Where(r => r.ParseError is null))
        {
            var created = await CreateAsync(row.Name, row.Address, ct);
            results.Add(created);
        }
        return results;
    }

    public async Task<byte[]> ExportIntoCsvBytesAsync(CancellationToken ct = default)
    {
        var establishments = await GetAllAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("name;address");
        foreach (var e in establishments)
        {
            sb.AppendLine($"{CsvSerializer.CsvEscape(e.Name)};{CsvSerializer.CsvEscape(e.Address)}");
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}