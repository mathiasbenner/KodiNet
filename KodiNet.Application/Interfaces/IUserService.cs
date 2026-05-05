using KodiNet.Application.DTOs;
using KodiNet.Domain.Entities;

namespace KodiNet.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?>                  GetByOidAsync(string oid, CancellationToken ct = default);
    Task<UserDto>                   UpsertAsync(NewUserRequest user, CancellationToken ct = default);
    Task<IReadOnlyList<string>>     GetRolesAsync(string oid, CancellationToken ct = default);
    Task<IReadOnlyList<UserDto>>    GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AppRole>>    GetAllRolesAsync(CancellationToken ct = default);
    Task                            AssignRoleAsync(int userId, int roleId, CancellationToken ct = default);
    Task                            RemoveRoleAsync(int userId, int roleId, CancellationToken ct = default);
    Task                            TransferOwnershipAsync(int newOwnerUserId, CancellationToken ct = default);
    Task<UserDto?>                  GetOwnerAsync(CancellationToken ct = default);
    Task                            ToggleNotificationsAsync(int userId, bool enable, CancellationToken ct = default);
    Task                            ModifyNotificationEmailAsync(int userId, string? email, CancellationToken ct = default);
}