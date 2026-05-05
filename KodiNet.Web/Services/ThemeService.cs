using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Infrastructure.Security;
using KodiNet.Web.Layout;
using Microsoft.JSInterop;
using MudBlazor;

namespace KodiNet.Web.Services;

public enum ThemeMode { System, Light, Dark }

/// <summary>
/// Service Scoped gérant le thème (système / clair / sombre).
/// Persiste le choix dans localStorage et détecte la préférence système.
/// </summary>
public sealed class ThemeService(
    AuthStateHelper authStateHelper,
    IUserPreferenceService userPreferenceSvc,
    IThemeManagementService themeMgmtSvc,
    IJSRuntime js) : IAsyncDisposable
{
    private const string KeyMode = "kn_theme_mode";
    public int CurrentThemeId { get; private set; }
    public ThemeMode CurrentMode { get; private set; } = ThemeMode.System;
    public bool IsDarkMode { get; private set; }

    public event Action? StateChanged;

    private IReadOnlyList<AppThemeDto> _themes = [];
    private string _userOid = string.Empty;

    public async Task InitAsync()
    {
        try
        {
            // Get user OID to load preferences
            if (await authStateHelper.GetOidAsync() is string oid)
                _userOid = oid;
            else
                throw new Exception("User OID claim not found");

            // Load themes from DB
            _themes = await themeMgmtSvc.GetAllAsync();

            // Load user preferences
            var pref = await userPreferenceSvc.GetSelectedThemeAsync(_userOid);
            CurrentThemeId = pref?.Id ?? _themes.First().Id;

            CurrentMode = await js.InvokeAsync<string?>("localStorage.getItem", KeyMode) switch
            {
                "light" => ThemeMode.Light,
                "dark" => ThemeMode.Dark,
                _ => ThemeMode.System
            };
        }
        catch 
        {
            CurrentThemeId = _themes.First().Id;
            CurrentMode = ThemeMode.System;
        }

        IsDarkMode = await ResolveDarkModeAsync();
    }

    public IReadOnlyList<ThemeDefinition> AvailableThemes =>
        _themes.Select(t => new ThemeDefinition(
            t.Id,
            t.Name,
            t.Light.Primary, // couleur du mode clair pour la pastille de sélection
            KodiNetTheme.BuildFromDto(t))).ToList();

    public MudTheme CurrentMudTheme =>
        _themes.FirstOrDefault(t => t.Id == CurrentThemeId) is { } dto
            ? KodiNetTheme.BuildFromDto(dto)
            : KodiNetTheme.BuildDefault(); // fallback hard-codé si DB vide

    public async Task SetThemeAsync(int themeId)
    {
        CurrentThemeId = themeId;
        await userPreferenceSvc.SetSelectedThemeAsync(_userOid, themeId);
        StateChanged?.Invoke();
    }

    public async Task SetModeAsync(ThemeMode mode)
    {
        CurrentMode = mode;
        try
        {
            var value = mode switch
            {
                ThemeMode.Light => "light",
                ThemeMode.Dark => "dark",
                _ => "system"
            };
            await js.InvokeVoidAsync("localStorage.setItem", KeyMode, value);
        }
        catch { }

        IsDarkMode = await ResolveDarkModeAsync();
        StateChanged?.Invoke();
    }

    private async Task<bool> ResolveDarkModeAsync()
    {
        if (CurrentMode == ThemeMode.Light) return false;
        if (CurrentMode == ThemeMode.Dark) return true;

        // Lire la préférence système via matchMedia
        try
        {
            return await js.InvokeAsync<bool>(
                "eval", "window.matchMedia('(prefers-color-scheme: dark)').matches");
        }
        catch { return false; }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}