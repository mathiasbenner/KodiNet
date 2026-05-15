using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Domain.Entities;
using KodiNet.Domain.Interfaces.Services;
using KodiNet.Infrastructure.Data;
using KodiNet.Infrastructure.Serializers;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace KodiNet.Infrastructure.Services;

public sealed class RaspberryPiService(
    IDbContextFactory<AppDbContext> dbFactory,
    ICredentialEncryption           encryption) : IRaspberryPiService
{
    public async Task<IReadOnlyList<PiDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var pis = await db.RaspberryPis
            .AsNoTracking()
            .Include(p => p.Establishment)
            .OrderBy(p => p.Establishment).ThenBy(p => p.Name)
            .ToListAsync(ct);
        return pis.Select(MapToDto).ToList();
    }

    public async Task<PiDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var pi = await db.RaspberryPis.AsNoTracking().Include(p => p.Establishment).FirstOrDefaultAsync(p => p.Id == id, ct);
        return pi is null ? null : MapToDto(pi);
    }

    public async Task<PiHttpCredentialsDto?> GetCredentialsByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var pi = await db.RaspberryPis.AsNoTracking().Include(p => p.Establishment).FirstOrDefaultAsync(p => p.Id == id, ct);
        return pi is null ? null : MapToCredentialsDto(pi);
    }

    public async Task<PiDto> CreateAsync(CreatePiRequest request, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var ipExists = await db.RaspberryPis.AnyAsync(p => p.IpAddress == request.IpAddress, ct);
        if (ipExists)
            throw new InvalidOperationException(
                $"A Pi with the IP address {request.IpAddress} already exists.");

        var entity = new RaspberryPi
        {
            Name                  = request.Name,
            IpAddress             = request.IpAddress,
            EstablishmentId       = request.EstablishmentId,
            Location              = request.Location,
            Model                 = request.Model,
            KodiUserEncrypted     = encryption.Encrypt(request.KodiUser),
            KodiPasswordEncrypted = encryption.Encrypt(request.KodiPassword),
            KodiPort              = request.KodiPort,
            SshUserEncrypted      = encryption.Encrypt(request.SshUser),
            SshPasswordEncrypted  = encryption.Encrypt(request.SshPassword),
            SshPort               = request.SshPort,
            VideoFolderPath       = request.VideoFolderPath
        };

        db.RaspberryPis.Add(entity);
        await db.SaveChangesAsync(ct);
        return MapToDto(entity);
    }

    public async Task<PiDto> UpdateAsync(UpdatePiRequest request, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var ipConflict = await db.RaspberryPis.AnyAsync(p => p.IpAddress == request.IpAddress && p.Id != request.Id, ct);
        if (ipConflict)
            throw new InvalidOperationException(
                $"The IP address {request.IpAddress} is already used by another Pi.");

        var pi = await db.RaspberryPis.FindAsync([request.Id], ct)
            ?? throw new KeyNotFoundException($"Pi {request.Id} not found.");

        pi.Name            = request.Name;
        pi.IpAddress       = request.IpAddress;
        pi.EstablishmentId = request.EstablishmentId;
        pi.Location        = request.Location;
        pi.Model           = request.Model;
        pi.KodiPort        = request.KodiPort;
        pi.SshPort         = request.SshPort;
        pi.VideoFolderPath = request.VideoFolderPath;
        pi.UpdatedAt       = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.KodiUser))
            pi.KodiUserEncrypted = encryption.Encrypt(request.KodiUser);
        if (!string.IsNullOrWhiteSpace(request.KodiPassword))
            pi.KodiPasswordEncrypted = encryption.Encrypt(request.KodiPassword);
        if (!string.IsNullOrWhiteSpace(request.SshUser))
            pi.SshUserEncrypted = encryption.Encrypt(request.SshUser);
        if (!string.IsNullOrWhiteSpace(request.SshPassword))
            pi.SshPasswordEncrypted = encryption.Encrypt(request.SshPassword);

        await db.SaveChangesAsync(ct);
        return MapToDto(pi);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.RaspberryPis.Where(p => p.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyList<PiDto>> ImportFromCsvAsync(CsvPiImportRequest request, CancellationToken ct = default)
    {
        var results = new List<PiDto>();
        foreach (var row in request.Rows.Where(r => r.ParseError is null))
        {
            if (!request.RowEstablishmentMap.TryGetValue(row.RowIndex, out var establishmentId))
                continue;

            var created = await CreateAsync(new CreatePiRequest(
                row.Name, row.IpAddress, establishmentId, row.Location, row.Model,
                row.KodiUser, row.KodiPassword, row.KodiPort,
                row.SshUser, row.SshPassword, row.SshPort, row.VideoFolderPath), ct);
            results.Add(created);
        }
        return results;
    }

    public async Task<byte[]> ExportIntoCsvBytesAsync(CancellationToken ct = default)
    {
        var pis = await GetAllAsync();

        var sb = new StringBuilder();
        sb.AppendLine("name;ip_address;location;model;kodi_user;kodi_password;kodi_port;ssh_user;ssh_password;ssh_port;video_folder");

        foreach (var pi in pis)
        {
            sb.AppendLine(string.Join(";",
                CsvSerializer.CsvEscape(pi.Name),
                CsvSerializer.CsvEscape(pi.IpAddress),
                CsvSerializer.CsvEscape(pi.Location),
                CsvSerializer.CsvEscape(pi.Model),
                "",
                "",
                pi.KodiPort,
                "",
                "",
                pi.SshPort,
                CsvSerializer.CsvEscape(pi.VideoFolderPath)));
        }

        // Note : voluntarily excluding credentials (user/password) from the export
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<string?> GetKodiWebUrlAsync(int piId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var pi = await db.RaspberryPis.AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == piId, ct);
        if (pi is null) return null;

        var user = encryption.Decrypt(pi.KodiUserEncrypted);
        var pass = encryption.Decrypt(pi.KodiPasswordEncrypted);

        // Encode special characters in user/password for the URL
        var encodedUser = Uri.EscapeDataString(user);
        var encodedPass = Uri.EscapeDataString(pass);

        return $"http://{encodedUser}:{encodedPass}@{pi.IpAddress}:{pi.KodiPort}";
    }

    private static PiDto MapToDto(RaspberryPi pi) => new(
        pi.Id, pi.Name, pi.IpAddress,
        pi.EstablishmentId,
        pi.Establishment?.Name ?? "",
        pi.Location, pi.Model, pi.KodiPort, pi.VideoFolderPath, pi.SshPort);

    private PiHttpCredentialsDto MapToCredentialsDto(RaspberryPi pi) => new(
        pi.Id, pi.IpAddress, pi.KodiPort, 
        encryption.Decrypt(pi.KodiUserEncrypted), 
        encryption.Decrypt(pi.KodiPasswordEncrypted));
}
