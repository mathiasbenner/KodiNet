using KodiNet.Application.DTOs;
using KodiNet.Domain.Enums;

namespace KodiNet.Application.Interfaces;

/// <summary>
/// Tracks non-transfer operations on the private storage (deletion, renaming, moving, browser upload).
/// Scoped service - one instance per Blazor circuit.
/// </summary>
public interface IStorageActivityService
{
    IReadOnlyList<StorageActivityDto> Activities { get; }
    event Action? StateChanged;
    Guid  Begin(StorageActivityKind kind, string description);
    void  Complete(Guid id);
    void  Fail(Guid id, string error);
    void  ClearCompleted();
}
