using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace KodiNet.Infrastructure.Security;

// ─── Enrichment of claims with local roles (DB) ───────────────────

public sealed class LocalRolesClaimsTransformation(IUserService userService) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.HasClaim(c => c.Type == "kn_roles_loaded"))
            return principal;

        var oid = principal.FindFirstValue("oid")
               ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(oid))
            return principal;

        var email = principal.FindFirstValue("preferred_username")
                 ?? principal.FindFirstValue(ClaimTypes.Email)
                 ?? "";
        var name  = principal.FindFirstValue("name")
                 ?? principal.FindFirstValue(ClaimTypes.Name)
                 ?? "";

        await userService.UpsertAsync(new NewUserRequest
        (
            MicrosoftOid: oid,
            Email: email,
            DisplayName: name,
            LastLoginAt: DateTime.UtcNow
        ));

        var roles    = await userService.GetRolesAsync(oid);
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("kn_roles_loaded", "true"));
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
            if (role == "Owner")
                identity.AddClaim(new Claim(ClaimTypes.Role, "Admin")); // Owner ⊇ Admin
        }

        principal.AddIdentity(identity);
        return principal;
    }
}
