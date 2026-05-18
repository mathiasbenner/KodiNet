using KodiNet.Domain.Enums;
using KodiNet.Domain.Interfaces.Services;
using KodiNet.Infrastructure.Parsing;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace KodiNet.Infrastructure.Clients;

/// <summary>
/// Implementation of the Kodi JSON-RPC client via HTTP.
/// Documentation: https://kodi.wiki/view/JSON-RPC_API
/// </summary>
public sealed class KodiJsonRpcClient(IHttpClientFactory httpFactory) : IKodiClient
{
    private static int _idCounter;

    public async Task<bool> PingAsync(string ip, int port, string user, string password, CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(AppConstants.Kodi.PingTimeout);
            var result = await CallAsync(ip, port, user, password, "JSONRPC.Ping", null, cts.Token);
            return result?["result"]?.GetValue<string>() == "pong";
        }
        catch { return false; }
    }

    public async Task<KodiPlayerState> GetPlayerStateAsync(string ip, int port, string user, string password, CancellationToken ct = default)
    {
        // Retrieve the active player
        var playerId = await GetVideoPlayerIdAsync(ip, port, user, password, ct);
        if (playerId is null)
            return new KodiPlayerState(false, null, null, 0, 0, 0, RepeatMode.Off);

        // Player properties
        var propsParams = new JsonObject
        {
            ["playerid"]   = playerId.Value,
            ["properties"] = new JsonArray("time", "totaltime", "repeat", "currentvideostream")
        };
        var props = await CallAsync(ip, port, user, password, "Player.GetProperties", propsParams, ct);
        var res   = props?["result"];

        // Application volume
        var volParams = new JsonObject { ["properties"] = new JsonArray("volume") };
        var volRes    = await CallAsync(ip, port, user, password, "Application.GetProperties", volParams, ct);
        var volume    = volRes?["result"]?["volume"]?.GetValue<int>() ?? 0;

        // Current file (no "label" properties as already provided, may cause an error if requested)
        var itemParams = new JsonObject
        {
            ["playerid"]   = playerId.Value,
            ["properties"] = new JsonArray("dateadded", "file", "playcount")
        };
        var item     = await CallAsync(ip, port, user, password, "Player.GetItem", itemParams, ct);
        // Treat empty string as null (Kodi returns "" when no file)
        var fileRaw = item?["result"]?["item"]?["label"]?.GetValue<string>();
        var file    = string.IsNullOrWhiteSpace(fileRaw) ? null : fileRaw;

        var timeNode = res?["time"];
        var position = KodiInfoParser.ParseSeconds(timeNode);
        var totalNode = res?["totaltime"];
        var duration  = KodiInfoParser.ParseSeconds(totalNode);

        var repeatStr = res?["repeat"]?.GetValue<string>() ?? "off";
        var repeat    = KodiInfoParser.ParseRepeatMode(repeatStr);

        return new KodiPlayerState(true, playerId.Value, file, position, duration, volume, repeat);
    }

    public async Task<KodiSystemInfo> GetSystemInfoAsync(string ip, int port, string user, string password, CancellationToken ct = default)
    {
        // ── Kodi version ────────────────────────────────────────────────────────
        var verParams  = new JsonObject { ["properties"] = new JsonArray("version") };
        var verRes     = await CallAsync(ip, port, user, password, "Application.GetProperties", verParams, ct);
        var ver        = verRes?["result"]?["version"];
        var kodiVer    = $"{ver?["major"]}.{ver?["minor"]}";

        // ── System infos ────────────────────────────────────────────────────────
        var sysParams  = new JsonObject { ["labels"] = new JsonArray("System.CpuUsage", "System.FreeMemory", "System.CpuTemperature", "System.FreeSpace") };
        var sysRes     = await CallAsync(ip, port, user, password, "XBMC.GetInfoLabels", sysParams, ct);
        var labels     = sysRes?["result"];

        var cpuStr     = labels?["System.CpuUsage"]?.GetValue<string>() ?? "0%";
        var cpu        = KodiInfoParser.ParseCpuUsage(cpuStr);
        var memStr     = labels?["System.FreeMemory"]?.GetValue<string>() ?? "0 MB";
        var mem        = KodiInfoParser.ParseMemory(memStr);
        var tempStr    = labels?["System.CpuTemperature"]?.GetValue<string>() ?? "0°C";
        var temp       = double.TryParse(tempStr.Replace("°C", "").Trim(), out var t) ? t : 0;
        var diskStr    = labels?["System.FreeSpace"]?.GetValue<string>() ?? "0 GB";
        var disk       = KodiInfoParser.ParseMemory(diskStr);

        return new KodiSystemInfo(kodiVer, "LibreELEC", cpu, mem, temp, disk);
    }

    public async Task PlayFileAsync(string ip, int port, string user, string password, string filePath, CancellationToken ct = default)
    {
        var p = new JsonObject { ["item"] = new JsonObject { ["file"] = filePath } };
        await CallAsync(ip, port, user, password, "Player.Open", p, ct);
    }

    public async Task StopAsync(string ip, int port, string user, string password, CancellationToken ct = default)
    {
        // Stop the first active player
        var id = await GetVideoPlayerIdAsync(ip, port, user, password, ct);
        if (id.HasValue)
            await CallAsync(ip, port, user, password, "Player.Stop", new JsonObject { ["playerid"] = id.Value }, ct);
    }

    public async Task SetVolumeAsync(string ip, int port, string user, string password, int volume, CancellationToken ct = default)
    {
        var p = new JsonObject { ["volume"] = Math.Clamp(volume, 0, 100) };
        await CallAsync(ip, port, user, password, "Application.SetVolume", p, ct);
    }

    public async Task SetRepeatAsync(string ip, int port, string user, string password, RepeatMode repeat, CancellationToken ct = default)
    {
        var id = await GetVideoPlayerIdAsync(ip, port, user, password, ct);
        if (!id.HasValue) return;

        var repeatStr = KodiInfoParser.FormatRepeatMode(repeat);
        var p = new JsonObject { ["playerid"] = id.Value, ["repeat"] = repeatStr };
        await CallAsync(ip, port, user, password, "Player.SetRepeat", p, ct);
    }

    public async Task<IReadOnlyList<string>> ListFilesAsync(string ip, int port, string user, string password, string directory, CancellationToken ct = default)
    {
        var p   = new JsonObject { ["directory"] = directory };
        var res = await CallAsync(ip, port, user, password, "Files.GetDirectory", p, ct);
        return res?["result"]?["files"]?.AsArray()
                  .Select(f => f?["file"]?.GetValue<string>() ?? "")
                  .Where(s => s.Length > 0)
                  .ToList() ?? [];
    }

    public async Task DeleteFileAsync(string ip, int port, string user, string password, string filePath, CancellationToken ct = default)
    {
        var p = new JsonObject { ["path"] = filePath };
        await CallAsync(ip, port, user, password, "Files.Delete", p, ct);
    }

    public async Task RebootAsync(string ip, int port, string user, string password, CancellationToken ct = default)
    {
        try { await CallAsync(ip, port, user, password, "System.Reboot", null, ct); }
        catch { /* connection closed by reboot — normal */ }
    }

    public async Task PlayFolderAsync(string ip, int port, string user, string password, string folderPath, CancellationToken ct = default)
    {
        // Player.Open with a folder — Kodi creates a playlist of all contents
        var p = new JsonObject { ["item"] = new JsonObject { ["directory"] = folderPath } };
        await CallAsync(ip, port, user, password, "Player.Open", p, ct);
    }

    public async Task TogglePlayPauseAsync(string ip, int port, string user, string password, CancellationToken ct = default)
    {
        var id = await GetVideoPlayerIdAsync(ip, port, user, password, ct);
        if (!id.HasValue) return;
        await CallAsync(ip, port, user, password, "Player.PlayPause",
            new JsonObject { ["playerid"] = id.Value }, ct);
    }
    public async Task SkipNextAsync(string ip, int port, string user, string password, CancellationToken ct = default)
    {
        var id = await GetVideoPlayerIdAsync(ip, port, user, password, ct);
        if (!id.HasValue) return;
        await CallAsync(ip, port, user, password, "Player.GoTo",
            new JsonObject { ["playerid"] = id.Value, ["to"] = "next" }, ct);
    }

    public async Task SkipPreviousAsync(string ip, int port, string user, string password, CancellationToken ct = default)
    {
        var id = await GetVideoPlayerIdAsync(ip, port, user, password, ct);
        if (!id.HasValue) return;
        await CallAsync(ip, port, user, password, "Player.GoTo",
            new JsonObject { ["playerid"] = id.Value, ["to"] = "previous" }, ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<JsonNode?> CallAsync(string ip, int port, string user, string password, string method, JsonNode? @params, CancellationToken ct)
    {
        var client  = httpFactory.CreateClient("Kodi");
        var url     = $"http://{ip}:{port}/jsonrpc";
        var payload = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"]  = method,
            ["id"]      = Interlocked.Increment(ref _idCounter)
        };
        if (@params is not null) payload["params"] = @params;

        var auth    = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}"));
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var node = JsonNode.Parse(json);

        // Kodi returns HTTP 200 even in case of JSON-RPC error — it must be detected
        var error = node?["error"];
        if (error is not null)
        {
            var code = error["code"]?.GetValue<int>() ?? 0;
            var message = error["message"]?.GetValue<string>() ?? "Unknown error";
            throw new InvalidOperationException($"Kodi JSON-RPC error {code}: {message}");
        }

        return node;
    }

    /// <summary>
    /// Returns the playerid of the active video player, or null if none.
    /// Player.GetActivePlayers can return multiple players (video, audio,
    /// slideshow) — we explicitly filter on type == "video".
    /// </summary>
    private async Task<int?> GetVideoPlayerIdAsync(string ip, int port, string user, string password, CancellationToken ct)
    {
        var response = await CallAsync(ip, port, user, password, "Player.GetActivePlayers", null, ct);
        var players = response?["result"]?.AsArray();
        if (players is null) return null;

        var videoPlayer = players.FirstOrDefault(p =>
            string.Equals(p?["type"]?.GetValue<string>(), "video", StringComparison.OrdinalIgnoreCase));

        return videoPlayer?["playerid"]?.GetValue<int>();
    }
}
