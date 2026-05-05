using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Infrastructure;

namespace KodiNet.Web.Services;

/// <summary>
/// Service partagé (Scoped = un par circuit Blazor) centralisant l'état
/// de tous les Pi. Dashboard et KnTopBar s'abonnent au même polling
/// sans dupliquer les appels vers Kodi.
/// </summary>
public sealed class PiStatusStore(IRaspberryPiService piSvc, IKodiService kodiSvc) : IAsyncDisposable
{
    private readonly Dictionary<int, PiStatusDto> _statusMap = [];
    private List<PiDto> _pis = [];
    private readonly CancellationTokenSource _cts = new();
    private bool _started;

    public event Action? StateChanged;

    public IReadOnlyDictionary<int, PiStatusDto> StatusMap => _statusMap;
    public IReadOnlyList<PiDto> Pis => _pis;

    public PiStatsModel Stats => new(
        Total: _pis.Count,
        Playing: _statusMap.Values.Count(s => s.Status == Domain.Enums.PiStatus.Playing),
        Offline: _statusMap.Values.Count(s => s.Status == Domain.Enums.PiStatus.Offline));

    /// <summary>
    /// Démarre le chargement initial et le polling.
    /// Idempotent — sans effet si déjà démarré.
    /// </summary>
    public async Task StartAsync()
    {
        if (_started) return;
        _started = true;

        _pis = (await piSvc.GetAllAsync()).ToList();
        StateChanged?.Invoke();

        _ = PollLoopAsync(_cts.Token);
    }

    /// <summary>Recharge la liste des Pi (après ajout/suppression).</summary>
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
        // Premier passage immédiat, puis polling périodique
        await PollAllAsync(ct);

        var statusTimer = new PeriodicTimer(AppConstants.Polling.DashboardStatusInterval);
        var listTimer   = new PeriodicTimer(AppConstants.Polling.DashboardListInterval);
        // Lancer les deux boucles en parallèle
        await Task.WhenAll(
            StatusLoopAsync(statusTimer, ct),
            ListRefreshLoopAsync(listTimer, ct));
        //try
        //{
        //    while (await statusTimer.WaitForNextTickAsync(ct))
        //        await PollAllAsync(ct);
        //}
        //catch (OperationCanceledException) { }
    }

    private async Task PollAllAsync(CancellationToken ct)
    {
        foreach (var chunk in _pis.Chunk(AppConstants.Polling.StatusPollChunkSize))
        {
            await Task.WhenAll(chunk.Select(async pi =>
            {
                try
                {
                    _statusMap[pi.Id] = await kodiSvc.GetStatusAsync(pi.Id, ct);
                    StateChanged?.Invoke();
                }
                catch { }
            }));
        }
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

    public record PiStatsModel(int Total, int Playing, int Offline);
}