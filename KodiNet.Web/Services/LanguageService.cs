using KodiNet.Application.Interfaces;
using KodiNet.Application.Options;
using KodiNet.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace KodiNet.Web.Services;

/// <summary>
/// Scoped service managing themes (system / light / dark).
/// localStorage persistence and system preferences detection.
/// </summary>
public sealed class LanguageService(
    AuthStateHelper                 authStateHelper,
    IUserPreferenceService          userPreferenceSvc,
    IOptions<LocalizationOptions>   localizationOpts,
    IJSRuntime js) : IAsyncDisposable
{
    private readonly LocalizationOptions _localization = localizationOpts.Value;
    public string CurrentLang { get; private set; } = localizationOpts.Value.DefaultCulture;
    public bool Loading { get; private set; } = true;

    public event Action? StateChanged;

    private string _userOid = "";

    public async Task InitAsync()
    {
        try
        {
            // Get user OID to load preferences
            if (await authStateHelper.GetOidAsync() is string oid)
                _userOid = oid;
            else
                throw new Exception("User OID claim not found");

            // Load user preferences
            CurrentLang = await userPreferenceSvc.GetLanguageAsync(_userOid) ?? _localization.DefaultCulture;
        }
        catch 
        {
            CurrentLang = _localization.DefaultCulture;
        }
        finally
        {
            Loading = false;
        }
    }

    public async Task SetLanguageAsync(string lang)
    {
        CurrentLang = lang;

        // Database persistence with UserPreference
        await userPreferenceSvc.SetLanguageAsync(_userOid, lang);
        // Modify active culture — force page reload
        await js.InvokeVoidAsync("eval",
            $"document.cookie='.AspNetCore.Culture=c%3D{lang}%7Cuic%3D{lang};path=/;expires=' + new Date(Date.now() + 365*86400*1000).toUTCString(); location.reload();");
        StateChanged?.Invoke();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}