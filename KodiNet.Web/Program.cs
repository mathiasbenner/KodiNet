using KodiNet.Application.Options;
using KodiNet.Infrastructure;
using KodiNet.Web;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Auth Microsoft (delegate) ─────────────────────────────────────────────────
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// Force authentication on all pages
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents() //Interactive server-side components.
    .AddMicrosoftIdentityConsentHandler();
builder.Services.AddRazorPages();

// Authorization by application role (loaded from the database via ClaimsTransformation)
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole("Admin","Operator","Viewer")
        .Build();
    options.AddPolicy("OwnerOnly", p => p.RequireRole("Owner"));
    options.AddPolicy("AdminOnly",    p => p.RequireRole("Owner", "Admin"));
    options.AddPolicy("OperatorPlus", p => p.RequireRole("Owner", "Admin", "Operator"));
});

// ── Infrastructure (BDD, clients, services) ───────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<LocalizationOptions>(
    builder.Configuration.GetSection(LocalizationOptions.Section));
builder.Services.Configure<PollingOptions>(
    builder.Configuration.GetSection(PollingOptions.Section));

// ── MudBlazor UI ──────────────────────────────────────────────────────────────
builder.Services.AddMudServices();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// ── Shared services ───────────────────────────────────────────────────────────
builder.Services.AddScoped<KodiNet.Web.Services.LanguageService>();
builder.Services.AddScoped<KodiNet.Web.Services.ThemeService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                     | ForwardedHeaders.XForwardedProto
                     | ForwardedHeaders.XForwardedHost
});
app.UseHttpsRedirection();
app.UseStaticFiles();

// ── Culture settings ───────────────────────────────────────────────────────────
var supportedCultures = new[] { "en", "fr" };
var defaultCulture = builder.Configuration.GetValue<string>("Localization:DefaultCulture");
if (defaultCulture is null || !supportedCultures.Contains(defaultCulture))
{
    Console.WriteLine("WARN: default culture fallback to \"en\"");
    defaultCulture = "en";
}
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture(defaultCulture)
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(); // For interactive server-side rendering

app.UseStatusCodePagesWithRedirects("/StatusCode/{0}");

// ── Automatic migrations at startup ───────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KodiNet.Infrastructure.Data.AppDbContext>();
    db.Database.Migrate();
}

app.Run();
