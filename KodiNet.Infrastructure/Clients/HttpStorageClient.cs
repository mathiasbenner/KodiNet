using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using KodiNet.Application.DTOs;
using KodiNet.Domain.Interfaces.Services;
using KodiNet.Infrastructure.Parsing;
using Microsoft.Extensions.Configuration;

namespace KodiNet.Infrastructure.Clients;

/// <summary>
/// HTTP Client for the private storage server.
/// Authentication via API Key in the X-Api-Key header.
///
/// Required configuration (appsettings.json):
///   Storage:BaseUrl → e.g., http://localhost:8090
///   Storage:ApiKey  → secret key shared with the server
///   Storage:RootPath → exposed root path (e.g., /videos)
/// </summary>
public sealed class HttpStorageClient(
    IHttpClientFactory httpFactory,
    IConfiguration configuration) : IPrivateStorageClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // ── Config ────────────────────────────────────────────────────────────────

    private string BaseUrl => (configuration["Storage:BaseUrl"] ?? throw new InvalidOperationException("Storage:BaseUrl missing")).TrimEnd('/');
    private string ApiKey => configuration["Storage:ApiKey"] ?? "";
    private readonly StoragePathResolver _pathResolver = new(configuration["Storage:RootPath"] ?? "/");

    // ── Interface ─────────────────────────────────────────────────────────────
    public long MaxUploadBytes =>
        (configuration.GetValue<long>("Storage:MaxSizeMB", 4096)) * 1024 * 1024;

    public async Task<IReadOnlyList<StorageEntry>> ListAsync(string folderPath, CancellationToken ct = default)
    {
        var path = ResolvePath(folderPath);
        var response = await GetAsync($"/api/files?path={Uri.EscapeDataString(path)}", ct);
        response.EnsureSuccessStatusCode();

        var entries = await response.Content.ReadFromJsonAsync<StorageEntryDto[]>(JsonOpts, ct) ?? [];
        return entries.Select(MapToEntry).ToList();
    }

    public async Task<Stream> DownloadAsync(string filePath, CancellationToken ct = default)
    {
        var path = ResolvePath(filePath);
        var response = await GetAsync($"/api/files/download?path={Uri.EscapeDataString(path)}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(ct);
    }

    public async Task<StorageEntry> UploadAsync(
        string folderPath, string fileName, Stream content,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var path = ResolvePath(folderPath);

        // Wrap the stream to report progress
        var trackingStream = progress is not null && content.CanSeek
            ? new ProgressStream(content, progress)
            : content;

        using var form = new MultipartFormDataContent();
        var streamContent = new StreamContent(trackingStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(streamContent, "file", fileName);

        var response = await PostAsync($"/api/files/upload?path={Uri.EscapeDataString(path)}", form, ct);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<StorageEntryDto>(JsonOpts, ct)
                  ?? throw new InvalidOperationException("Invalid upload response.");
        return MapToEntry(dto);
    }

    public async Task<StorageEntry> CreateFolderAsync(string folderPath, CancellationToken ct = default)
    {
        var path = ResolvePath(folderPath);
        var response = await PostAsync($"/api/files/folder?path={Uri.EscapeDataString(path)}", null, ct);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<StorageEntryDto>(JsonOpts, ct)
                  ?? throw new InvalidOperationException("Invalid folder creation response.");
        return MapToEntry(dto);
    }

    public async Task DeleteFileAsync(string path, CancellationToken ct = default)
    {
        var resolved = ResolvePath(path);
        var response = await DeleteAsync($"/api/files?path={Uri.EscapeDataString(resolved)}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<StorageEntry> RenameAsync(string path, string newName, CancellationToken ct = default)
    {
        var resolved = ResolvePath(path);
        var body     = JsonContent.Create(new { path = resolved, newName });
        var response = await PatchAsync("/api/files/rename", body, ct);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<StorageEntryDto>(JsonOpts, ct)
                  ?? throw new InvalidOperationException("Invalid rename response.");
        return MapToEntry(dto);
    }

    public async Task<StorageEntry> MoveAsync(string path, string destFolder, CancellationToken ct = default)
    {
        var resolved     = ResolvePath(path);
        var resolvedDest = ResolvePath(destFolder);
        var body         = JsonContent.Create(new { path = resolved, destFolder = resolvedDest });
        var response     = await PatchAsync("/api/files/move", body, ct);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<StorageEntryDto>(JsonOpts, ct)
                  ?? throw new InvalidOperationException("Invalid move response.");
        return MapToEntry(dto);
    }

    // ── HTTP Helpers ──────────────────────────────────────────────────────────

    // The API Key is injected per request via HttpRequestMessage.Headers
    // and not via DefaultRequestHeaders to avoid duplicates in the HttpClient pool.
    private HttpClient CreateClient() => httpFactory.CreateClient("Storage");

    private void AddApiKey(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(ApiKey))
            request.Headers.Add("X-Api-Key", ApiKey);
    }

    private Task<HttpResponseMessage> GetAsync(string relativeUrl, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, BaseUrl + relativeUrl);
        AddApiKey(req);
        return CreateClient().SendAsync(req, ct);
    }

    private Task<HttpResponseMessage> PostAsync(string relativeUrl, HttpContent? body, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl + relativeUrl) { Content = body };
        AddApiKey(req);
        return CreateClient().SendAsync(req, ct);
    }

    private Task<HttpResponseMessage> DeleteAsync(string relativeUrl, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, BaseUrl + relativeUrl);
        AddApiKey(req);
        return CreateClient().SendAsync(req, ct);
    }

    private Task<HttpResponseMessage> PatchAsync(string relativeUrl, HttpContent body, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Patch, BaseUrl + relativeUrl) { Content = body };
        AddApiKey(req);
        return CreateClient().SendAsync(req, ct);
    }

    // ── Path Resolution ─────────────────────────────────────────────────────────

    /// <summary>
    /// Prefixes the relative path with RootPath if necessary.
    /// </summary>
    private string ResolvePath(string? path) => _pathResolver.Resolve(path);

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static StorageEntry MapToEntry(StorageEntryDto dto) => new(
        dto.Path, dto.Name, dto.IsFolder, dto.SizeBytes,
        dto.LastModified == default ? DateTime.MinValue : dto.LastModified);
}

// ── Stream with Progress ────────────────────────────────────────────────────

/// <summary>Wrapper stream that reports upload progress.</summary>
internal sealed class ProgressStream(Stream inner, IProgress<double> progress) : Stream
{
    private long _reported;
    private readonly long _total = inner.Length;

    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => inner.CanSeek;
    public override bool CanWrite => inner.CanWrite;
    public override long Length => inner.Length;
    public override long Position { get => inner.Position; set => inner.Position = value; }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = inner.Read(buffer, offset, count);
        Report(read);
        return read;
    }

    public override int Read(Span<byte> buffe)
    {
        var read = inner.Read(buffe);
        Report(read);
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        var read = await inner.ReadAsync(buffer, ct);
        Report(read);
        return read;
    }

    private void Report(int bytes)
    {
        _reported += bytes;
        if (_total > 0)
            progress.Report((double)_reported / _total * 100);
    }

    public override void Flush() => inner.Flush();
    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
    public override void SetLength(long value) => inner.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);

    protected override void Dispose(bool disposing)
    {
        if (disposing) inner.Dispose();
        base.Dispose(disposing);
    }
}
