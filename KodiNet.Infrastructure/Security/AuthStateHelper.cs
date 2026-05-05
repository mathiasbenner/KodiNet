using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace KodiNet.Infrastructure.Security;

/// <summary>Provides helper methods for retrieving authentication-related state information from an AuthenticationStateProvider.</summary>
/// <param name="authStateProvider">The AuthenticationStateProvider instance used to obtain the current user's authentication state.</param>
public sealed class AuthStateHelper(AuthenticationStateProvider authStateProvider)
{
    /// <summary>Get connected user properties.</summary>
    public async Task<ClaimsPrincipal> GetUserAsync()
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        return user;
    }

    /// <summary>Get user object identifier.</summary>
    public string? GetOid(ClaimsPrincipal user)
    {
        var oid = user.FindFirstValue("oid")
               ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrEmpty(oid)
            ? null
            : oid;
    }

    /// <summary>Get user object identifier.</summary>
    public async Task<string?> GetOidAsync(ClaimsPrincipal? user = null)
    {
        if (user == null)
        {
            var authState = await authStateProvider.GetAuthenticationStateAsync();
            user = authState.User;
        }
        var oid = user.FindFirstValue("oid")
               ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrEmpty(oid) 
            ? null
            : oid;
    }

    /// <summary>Get user displayed name.</summary>
    public string GetDisplayName(ClaimsPrincipal user)
    {
        var displayName = user.FindFirstValue("name")
                       ?? user.FindFirstValue(ClaimTypes.Name);
        return string.IsNullOrEmpty(displayName)
            ? "Utilisateur"
            : displayName;
    }

    /// <summary>Get user displayed name.</summary>
    public async Task<string> GetDisplayNameAsync(ClaimsPrincipal? user = null)
    {
        if (user is null)
        {
            var authState = await authStateProvider.GetAuthenticationStateAsync();
            user = authState.User;
        }
        var displayName = user.FindFirstValue("name")
                       ?? user.FindFirstValue(ClaimTypes.Name);
        return string.IsNullOrEmpty(displayName) 
            ? "Utilisateur"
            : displayName;
    }

    /// <summary>Get user preferred name.</summary>
    public string GetPreferredUsername(ClaimsPrincipal user)
    {
        var displayName = user.FindFirstValue("preferred_username")
                       ?? user.FindFirstValue(ClaimTypes.Email);
        return string.IsNullOrEmpty(displayName)
            ? string.Empty
            : displayName;
    }

    /// <summary>Get user preferred name.</summary>
    public async Task<string> GetPreferredUsernameAsync(ClaimsPrincipal? user = null)
    {
        if (user is null)
        {
            var authState = await authStateProvider.GetAuthenticationStateAsync();
            user = authState.User;
        }
        var displayName = user.FindFirstValue("preferred_username")
                       ?? user.FindFirstValue(ClaimTypes.Email);
        return string.IsNullOrEmpty(displayName)
            ? string.Empty
            : displayName;
    }
}
