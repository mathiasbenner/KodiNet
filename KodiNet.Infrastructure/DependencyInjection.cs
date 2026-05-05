using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Domain.Interfaces.Services;
using KodiNet.Infrastructure.Clients;
using KodiNet.Infrastructure.Data;
using KodiNet.Infrastructure.Security;
using KodiNet.Infrastructure.Serializers;
using KodiNet.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace KodiNet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        string connectionStringText = "KodinetConnection";
        var connectionString = configuration.GetConnectionString(connectionStringText)
            ?? throw new InvalidOperationException($"ConnectionString '{connectionStringText}' missing.");

        // ── IDbContextFactory — the cornerstone ─────────────────────────────────
        // AddPooledDbContextFactory: pool of reusable contexts (optimal perf).
        // Each service creates and disposes its own context via CreateDbContextAsync().
        services.AddPooledDbContextFactory<AppDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                mysql => mysql.EnableRetryOnFailure(3)));

        // Also necessary for ASP.NET middlewares (migrations, auth) that inject
        // AppDbContext directly without using the factory.
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        // ── AES encryption of credentials (DataProtection API) ─────────────────
        services.AddDataProtection().SetApplicationName("KodiNet");
        services.AddSingleton<ICredentialEncryption>(sp =>
        {
            var dp = sp.GetRequiredService<IDataProtectionProvider>();
            return new CredentialEncryptionService(dp.CreateProtector("KodiNet.Credentials"));
        });

        // ── Technical clients ───────────────────────────────────────────────────
        services.AddHttpClient("Kodi").ConfigureHttpClient(c => c.Timeout = AppConstants.Kodi.RequestTimeout);
        services.AddScoped<IKodiClient, KodiJsonRpcClient>();
        services.AddScoped<ISftpFileClient, SftpFileClient>();
        services.AddScoped<IPrivateStorageClient, HttpStorageClient>();
        services.AddScoped(typeof(IThemePaletteSerializer<ThemePaletteDto>), typeof(ThemePaletteSerializer));

        // ── Application services (implementations in Infrastructure) ───────────
        services.AddScoped<IAppSettingService, AppSettingService>();
        services.AddScoped<ICronManagementService, CronManagementService>();
        services.AddScoped<IEstablishmentService, EstablishmentService>();
        services.AddScoped<IKodiRestarterJobService, KodiRestarterJobService>();
        services.AddScoped<IKodiRebooterJobService, KodiRebooterJobService>();
        services.AddScoped<IKodiService, KodiService>();
        services.AddScoped<IMailNotificationService, MailNotificationService>();
        services.AddScoped<IRaspberryPiService, RaspberryPiService>();
        services.AddScoped<IStorageActivityService, StorageActivityService>();
        services.AddScoped<IStorageService, StorageService>();
        services.AddScoped<IThemeManagementService, ThemeManagementService>();
        services.AddScoped<IUserPreferenceService, UserPreferenceService>();
        services.AddScoped<IUserService, UserService>();

        // Singleton: global state shared across all Blazor sessions.
        // Uses IDbContextFactory to create a context per operation — safe.
        services.AddSingleton<ITransferQueueService, TransferQueueService>();

        // ── Claims transformation: local roles injected from the database ──────
        services.AddScoped<IClaimsTransformation, LocalRolesClaimsTransformation>();

        // ── AuthenticationStateProvider wrapper ─────────────────────────────────
        services.AddScoped<AuthStateHelper>();

        services.AddHostedService<CronJobHostedService>();

        services.AddSignalR(options =>
        {
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}
