using KodiNet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KodiNet.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppSetting>        AppSettings     => Set<AppSetting>();
    public DbSet<AppTheme>          AppThemes       => Set<AppTheme>();
    public DbSet<CronJob>           CronJobs        => Set<CronJob>();
    public DbSet<CronJobExecution>  CronJobExecutions => Set<CronJobExecution>();
    public DbSet<Establishment>     Establishments  => Set<Establishment>();
    public DbSet<RaspberryPi>       RaspberryPis    => Set<RaspberryPi>();
    public DbSet<AppRole>           Roles           => Set<AppRole>();
    public DbSet<AppUser>           Users           => Set<AppUser>();
    public DbSet<UserPreference>    UserPreferences => Set<UserPreference>();
    public DbSet<UserRole>          UserRoles       => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── AppSetting ────────────────────────────────────────────────────────
        modelBuilder.Entity<AppSetting>(e =>
        {
            e.ToTable("app_settings");
            e.HasKey(s => s.Key);
            e.Property(s => s.Key).HasMaxLength(100);
        });

        // ── AppTheme ──────────────────────────────────────────────────────────
        modelBuilder.Entity<AppTheme>(e =>
        {
            e.ToTable("app_themes");
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(100);
            e.Property(t => t.LightPaletteJson).IsRequired();
            e.Property(t => t.DarkPaletteJson).IsRequired();
        });

        // ── CronJob ───────────────────────────────────────────────────────────
        modelBuilder.Entity<CronJob>(e =>
        {
            e.ToTable("app_cron_jobs");
            e.HasKey(j => j.Id);
            e.Property(j => j.Name).IsRequired().HasMaxLength(100);
            e.Property(j => j.Schedule).IsRequired().HasMaxLength(100);
        });

        // ── CronJobExecution ──────────────────────────────────────────────────
        modelBuilder.Entity<CronJobExecution>(e =>
        {
            e.ToTable("app_cron_executions");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.CronJob).WithMany(j => j.Executions).HasForeignKey(x => x.CronJobId);
            e.Property(x => x.ResultJson).HasColumnType("longtext");
        });

        // ── Establishment ─────────────────────────────────────────────────────
        modelBuilder.Entity<Establishment>(e =>
        {
            e.ToTable("establishments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).IsRequired().HasMaxLength(128);
            e.Property(x => x.Address).HasMaxLength(256);
        });

        // ── RaspberryPi ───────────────────────────────────────────────────────
        modelBuilder.Entity<RaspberryPi>(e =>
        {
            e.ToTable("raspberry_pis");
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(64);
            e.Property(p => p.IpAddress).IsRequired().HasMaxLength(45);
            e.Property(p => p.Location).IsRequired().HasMaxLength(128);
            e.Property(p => p.Model).HasMaxLength(64);
            e.Property(p => p.KodiUserEncrypted).IsRequired();
            e.Property(p => p.KodiPasswordEncrypted).IsRequired();
            e.Property(p => p.SshUserEncrypted).IsRequired();
            e.Property(p => p.SshPasswordEncrypted).IsRequired();
            e.Property(p => p.VideoFolderPath).HasMaxLength(500);
            e.HasOne(p => p.Establishment)
             .WithMany(x => x.RaspberryPis)
             .HasForeignKey(p => p.EstablishmentId)
             .OnDelete(DeleteBehavior.Restrict); // no ripple — populated establishment can't be deleted
        });

        // ── AppRole ───────────────────────────────────────────────────────────
        modelBuilder.Entity<AppRole>(e =>
        {
            e.ToTable("app_roles");
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Name).IsUnique();
            e.Property(r => r.Name).IsRequired().HasMaxLength(32);
            e.Property(r => r.Description).HasMaxLength(128);
        });

        // ── AppUser ───────────────────────────────────────────────────────────
        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("app_users");
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.MicrosoftOid).IsUnique();
            e.Property(u => u.MicrosoftOid).IsRequired().HasMaxLength(64);
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.DisplayName).HasMaxLength(200);
        });

        // ── UserRole (jointure) ───────────────────────────────────────────────
        modelBuilder.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles");
            e.HasKey(ur => new { ur.AppUserId, ur.AppRoleId });
            e.HasOne(ur => ur.AppUser).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.AppUserId);
            e.HasOne(ur => ur.AppRole).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.AppRoleId);
        });

        // ── UserPreference ────────────────────────────────────────────────────
        modelBuilder.Entity<UserPreference>(e =>
        {
            e.ToTable("user_preferences");
            e.HasKey(p => p.Id);
            e.Property(p => p.Language).HasMaxLength(8);
            e.HasOne(p => p.AppUser).WithOne().HasForeignKey<UserPreference>(p => p.Id);
            e.HasOne(p => p.SelectedTheme).WithMany().HasForeignKey(p => p.SelectedThemeId).IsRequired(false);
        });

        SeedInitialData(modelBuilder);
    }

    private void SeedInitialData(ModelBuilder modelBuilder)
    {
        // Mail key settings seed (empty values — fill in Owner interface)
        modelBuilder.Entity<AppSetting>().HasData(
            new AppSetting { Key = "Mail:From",      Value = "", IsSensitive = false },
            new AppSetting { Key = "Mail:FromName",  Value = "Kodinet", IsSensitive = false },
            new AppSetting { Key = "Mail:SmtpHost",  Value = "", IsSensitive = false },
            new AppSetting { Key = "Mail:SmtpPort",  Value = "587", IsSensitive = false },
            new AppSetting { Key = "Mail:EnableSsl", Value = "true", IsSensitive = false },
            new AppSetting { Key = "Mail:Username",  Value = "", IsSensitive = false },
            new AppSetting { Key = "Mail:Password",  Value = "", IsSensitive = true }
        );

        // Default themes seed
        modelBuilder.Entity<AppTheme>().HasData(
            new AppTheme { Id = 1, Name = "Ruby",  SwatchColor = "#9D344B",
                LightPaletteJson = "{\r\n  \"primary\": \"#9D344B\",\r\n  \"primaryDarken\": \"#7a2539\",\r\n  \"secondary\": \"#4A7C5E\",\r\n  \"secondaryDarken\": \"#3a6149\",\r\n  \"background\": \"#fdf5f6\",\r\n  \"surface\": \"#ffffff\",\r\n  \"appbarBackground\": \"#9D344B\",\r\n  \"appbarText\": \"#ffffff\",\r\n  \"textPrimary\": \"#3a1520\",\r\n  \"textSecondary\": \"#9a7580\",\r\n  \"divider\": \"#f0d5da\",\r\n  \"error\": \"#b71c1c\",\r\n  \"success\": \"#4A7C5E\",\r\n  \"warning\": \"#e65100\",\r\n  \"info\": \"#7a2539\"\r\n}",
                DarkPaletteJson  = "{\r\n  \"primary\": \"#e08090\",\r\n  \"primaryDarken\": \"#f0a8b4\",\r\n  \"secondary\": \"#7ab89a\",\r\n  \"secondaryDarken\": \"#9acdb5\",\r\n  \"background\": \"#1a0d10\",\r\n  \"surface\": \"#2b1419\",\r\n  \"appbarBackground\": \"#220e13\",\r\n  \"appbarText\": \"#fce8eb\",\r\n  \"textPrimary\": \"#fce8eb\",\r\n  \"textSecondary\": \"#c09090\",\r\n  \"divider\": \"#4a2530\",\r\n  \"error\": \"#ef9a9a\",\r\n  \"success\": \"#a5d6a7\",\r\n  \"warning\": \"#ffcc80\",\r\n  \"info\": \"#e08090\"\r\n}",
                IsSystem = true },
            new AppTheme { Id = 2, Name = "Ocean", SwatchColor = "#28546C",
                LightPaletteJson = "{\r\n  \"primary\": \"#28546C\",\r\n  \"primaryDarken\": \"#1a3d52\",\r\n  \"secondary\": \"#3A8FA3\",\r\n  \"secondaryDarken\": \"#2d7388\",\r\n  \"background\": \"#eef5f8\",\r\n  \"surface\": \"#ffffff\",\r\n  \"appbarBackground\": \"#28546C\",\r\n  \"appbarText\": \"#ffffff\",\r\n  \"textPrimary\": \"#0e2030\",\r\n  \"textSecondary\": \"#5a7a8a\",\r\n  \"divider\": \"#c8dfe8\",\r\n  \"error\": \"#c62828\",\r\n  \"success\": \"#2e7d32\",\r\n  \"warning\": \"#ef6c00\",\r\n  \"info\": \"#1a3d52\"\r\n}",
                DarkPaletteJson  = "{\r\n  \"primary\": \"#6aafc5\",\r\n  \"primaryDarken\": \"#90c8d8\",\r\n  \"secondary\": \"#70b8c8\",\r\n  \"secondaryDarken\": \"#98d0dc\",\r\n  \"background\": \"#080f14\",\r\n  \"surface\": \"#101e28\",\r\n  \"appbarBackground\": \"#0d1a22\",\r\n  \"appbarText\": \"#d8eef5\",\r\n  \"textPrimary\": \"#d8eef5\",\r\n  \"textSecondary\": \"#6090a8\",\r\n  \"divider\": \"#1a3a50\",\r\n  \"error\": \"#ef9a9a\",\r\n  \"success\": \"#a5d6a7\",\r\n  \"warning\": \"#ffcc80\",\r\n  \"info\": \"#6aafc5\"\r\n}",
                IsSystem = true },
            new AppTheme { Id = 3, Name = "Amber", SwatchColor = "#AA6C39",
                LightPaletteJson = "{\r\n  \"primary\": \"#AA6C39\",\r\n  \"primaryDarken\": \"#8a5228\",\r\n  \"secondary\": \"#7A8A3A\",\r\n  \"secondaryDarken\": \"#606e2c\",\r\n  \"background\": \"#faf4ec\",\r\n  \"surface\": \"#ffffff\",\r\n  \"appbarBackground\": \"#AA6C39\",\r\n  \"appbarText\": \"#ffffff\",\r\n  \"textPrimary\": \"#2e1c0a\",\r\n  \"textSecondary\": \"#8a6a50\",\r\n  \"divider\": \"#e8d8c0\",\r\n  \"error\": \"#c62828\",\r\n  \"success\": \"#558b2f\",\r\n  \"warning\": \"#e65100\",\r\n  \"info\": \"#8a5228\"\r\n}",
                DarkPaletteJson  = "{\r\n  \"primary\": \"#d4956a\",\r\n  \"primaryDarken\": \"#e8b898\",\r\n  \"secondary\": \"#aab870\",\r\n  \"secondaryDarken\": \"#c8d090\",\r\n  \"background\": \"#140e06\",\r\n  \"surface\": \"#221608\",\r\n  \"appbarBackground\": \"#1c1205\",\r\n  \"appbarText\": \"#faebd7\",\r\n  \"textPrimary\": \"#faebd7\",\r\n  \"textSecondary\": \"#b09070\",\r\n  \"divider\": \"#3c2810\",\r\n  \"error\": \"#ef9a9a\",\r\n  \"success\": \"#a5d6a7\",\r\n  \"warning\": \"#ffcc80\",\r\n  \"info\": \"#d4956a\"\r\n}",
                IsSystem = true });

        // Jobs seed
        modelBuilder.Entity<CronJob>().HasData(
            new CronJob
            {
                Id = 1,
                Name = "KodiRestarter",
                Description = "Restart any inactive Kodi players and play the video folder on a loop.",
                Schedule = "0 6-16 * * *"
            },
            new CronJob
            {
                Id = 2,
                Name = "KodiRebooter",
                Description = "Reboot the LibreELEC system on all the Pi devices in the list.",
                Schedule = "50 5 * * *"
            }
        );

        // Default roles seed
        modelBuilder.Entity<AppRole>().HasData(
            new AppRole { Id = 1, Name = "Admin", Description = "Full access (Pi management, transfers, deletion)" },
            new AppRole { Id = 2, Name = "Operator", Description = "Pi monitoring, file transfers" },
            new AppRole { Id = 3, Name = "Viewer", Description = "Read-only — view the status of the Pi" },
            new AppRole { Id = 4, Name = "Owner", Description = "Sole owner — non-revocable admin rights, cannot be configured via the UI" }
        );
    }
}
