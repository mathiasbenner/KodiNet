using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KodiNet.Application.Interfaces;

namespace KodiNet.Web.Controllers;

[ApiController]
[Authorize]
[Route("kodi")]
public sealed class KodiRedirectController(IRaspberryPiService piSvc) : ControllerBase
{
    [HttpGet("{piId:int}")]
    public async Task<IActionResult> RedirectToKodi(int piId, CancellationToken ct)
    {
        var url = await piSvc.GetKodiWebUrlAsync(piId, ct);
        if (url is null) return NotFound();
        return Redirect(url);  // 302 — le navigateur suit et envoie les credentials
    }
}