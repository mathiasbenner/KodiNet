using System.Collections.Concurrent;
using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Domain.Enums;

namespace KodiNet.Infrastructure.Services;

/// <summary>
/// Scoped (per Blazor circuit) for tracking storage operations.
/// </summary>
public sealed class StorageActivityService : IStorageActivityService
{
    private readonly ConcurrentDictionary<Guid, StorageActivityDto> _activities = new();

    public event Action? StateChanged;

    public IReadOnlyList<StorageActivityDto> Activities =>
        _activities.Values.OrderByDescending(a => a.CreatedAt).ToList();

    public Guid Begin(StorageActivityKind kind, string description)
    {
        var id  = Guid.NewGuid();
        var dto = new StorageActivityDto(id, kind, description, TransferStatus.Running, null, DateTime.UtcNow);
        _activities[id] = dto;
        StateChanged?.Invoke();
        return id;
    }

    public void Complete(Guid id)
    {
        if (_activities.TryGetValue(id, out var dto))
            _activities[id] = dto with { Status = TransferStatus.Done };
        StateChanged?.Invoke();
    }

    public void Fail(Guid id, string error)
    {
        if (_activities.TryGetValue(id, out var dto))
            _activities[id] = dto with { Status = TransferStatus.Error, ErrorMessage = error };
        StateChanged?.Invoke();
    }

    public void ClearCompleted()
    {
        foreach (var key in _activities.Keys.Where(k =>
            _activities[k].Status is TransferStatus.Done or TransferStatus.Error))
            _activities.TryRemove(key, out _);
        StateChanged?.Invoke();
    }
}
