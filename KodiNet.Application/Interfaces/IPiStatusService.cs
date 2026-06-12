using KodiNet.Application.DTOs;

namespace KodiNet.Application.Interfaces;

public interface IPiStatusService
{
    IReadOnlyList<PiDto> Pis { get; }
    PiStatsModelDto Stats { get; }
    IReadOnlyDictionary<int, PiStatusDto> StatusMap { get; }

    event Action? StateChanged;

    ValueTask DisposeAsync();
    PiStatusDto? GetStatus(int piId);
    Task RefreshPisAsync();
    Task StartAsync();
}