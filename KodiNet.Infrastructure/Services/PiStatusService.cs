using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Application.Options;
using Microsoft.Extensions.Options;

namespace KodiNet.Infrastructure.Services;

/// <summary>
/// Shared Service (Scoped = one per Blazor circuit) centralizing status of all Pis.<br />
/// Dashboard and KnTopBar subscribe to the same polling without duplicating Kodi calls.
/// </summary>
public sealed class PiStatusService(
    IRaspberryPiService piSvc,
    IKodiService kodiSvc,
    IOptions<PollingOptions> pollingOpts) : IAsyncDisposable, IPiStatusService
{
    private readonly PollingOptions _polling = pollingOpts.Value;

    private readonly Dictionary<int, PiStatusDto> _statusMap = [];
    private List<PiDto> _pis = [];
    private readonly CancellationTokenSource _cts = new();
    private bool _started;

    private readonly Dictionary<int, int>      _failureCount    = [];
    private readonly Dictionary<int, DateTime> _nextAllowedPoll = [];

    public event Action? StateChanged;

    public IReadOnlyDictionary<int, PiStatusDto> StatusMap => _statusMap;
    public IReadOnlyList<PiDto> Pis => _pis;

    public PiStatsModelDto Stats => new(
        Total: _pis.Count,
        Playing: _statusMap.Values.Count(s => s.Status == Domain.Enums.PiStatus.Playing),
        Offline: _statusMap.Values.Count(s => s.Status == Domain.Enums.PiStatus.Offline));

    /// <summary>
    /// Start initial loading and polling.<br />
    /// Idempotent — without effect if already started.
    /// </summary>
    public async Task StartAsync()
    {
        if (_started) return;
        _started = true;

        _pis = (await piSvc.GetAllAsync()).ToList();
        StateChanged?.Invoke();

        _ = PollLoopAsync(_cts.Token);
    }

    /// <summary>Reload Pi list (after adding/removing).</summary>
    public async Task RefreshPisAsync()
    {
        _pis = (await piSvc.GetAllAsync()).ToList();
        StateChanged?.Invoke();
    }

    public PiStatusDto? GetStatus(int piId)
        => _statusMap.TryGetValue(piId, out var s) ? s : null;

    // ── Polling ───────────────────────────────────────────────────────────────

    private async Task PollLoopAsync(CancellationToken ct)
    {
        // First immediate call, then periodical polling
        await PollAllAsync(ct);

        var statusTimer = new PeriodicTimer(_polling.StatusInterval);
        var listTimer   = new PeriodicTimer(_polling.ListRefreshInterval);
        // Start both loops in simultaneously
        await Task.WhenAll(
            StatusLoopAsync(statusTimer, ct),
            ListRefreshLoopAsync(listTimer, ct));
    }

    private async Task PollAllAsync(CancellationToken ct)
    {
        var now     = DateTime.UtcNow;
        var toPoll  = _pis
            .Where(p => !_nextAllowedPoll.TryGetValue(p.Id, out var next) || now >= next)
            .ToList();

        using var sem = new SemaphoreSlim(_polling.MaxConcurrency);

        await Task.WhenAll(toPoll.Select(async pi =>
        {
            await sem.WaitAsync(ct);
            try
            {
                _statusMap[pi.Id] = await kodiSvc.GetStatusAsync(pi.Id, ct);
                _failureCount[pi.Id] = 0;
                _nextAllowedPoll.Remove(pi.Id); // reinitialize backoff on success
            }
            catch
            {
                var failures = _failureCount.GetValueOrDefault(pi.Id, 0) + 1;
                _failureCount[pi.Id] = failures;
                var backoff = Math.Min(Math.Pow(2, failures - 1) * 20, 600);
                _nextAllowedPoll[pi.Id] = DateTime.UtcNow.AddSeconds(backoff);
            }
            finally { sem.Release(); }
        }));

        StateChanged?.Invoke();
    }

    private async Task StatusLoopAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
                await PollAllAsync(ct);
        }
        catch (OperationCanceledException) { }
        finally { timer.Dispose(); }
    }

    private async Task ListRefreshLoopAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                _pis = (await piSvc.GetAllAsync(ct)).ToList();
                StateChanged?.Invoke();
            }
        }
        catch (OperationCanceledException) { }
        finally { timer.Dispose(); }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
    }
}