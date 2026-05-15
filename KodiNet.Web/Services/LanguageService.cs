using KodiNet.Application.Interfaces;
using KodiNet.Infrastructure.Security;
using Microsoft.JSInterop;
using static KodiNet.Infrastructure.AppConstants;

namespace KodiNet.Web.Services;

/// <summary>
/// Service Scoped gérant le thème (système / clair / sombre).
/// Persiste le choix dans localStorage et détecte la préférence système.
/// </summary>
public sealed class LanguageService(
    AuthStateHelper authStateHelper,
    IUserPreferenceService userPreferenceSvc,
    IJSRuntime js) : IAsyncDisposable
{
    public string CurrentLang { get; private set; } = Localization.DefaultCulture;
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
            CurrentLang = await userPreferenceSvc.GetLanguageAsync(_userOid) ?? Localization.DefaultCulture;
        }
        catch 
        {
            CurrentLang = Localization.DefaultCulture;
        }
        finally
        {
            Loading = false;
        }
    }

    public async Task SetLanguageAsync(string lang)
    {
        CurrentLang = lang;

        // Persister en base via UserPreference
        await userPreferenceSvc.SetLanguageAsync(_userOid, lang);
        // Changer la culture active — nécessite un rechargement de page
        await js.InvokeVoidAsync("eval",
            $"document.cookie='.AspNetCore.Culture=c%3D{lang}%7Cuic%3D{lang};path=/;expires=' + new Date(Date.now() + 365*86400*1000).toUTCString(); location.reload();");
        StateChanged?.Invoke();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}