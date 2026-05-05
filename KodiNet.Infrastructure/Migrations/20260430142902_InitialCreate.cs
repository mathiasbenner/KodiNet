using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KodiNet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "app_cron_jobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Schedule = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_cron_jobs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "app_roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_roles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSensitive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_settings", x => x.Key);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "app_themes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SwatchColor = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LightPaletteJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DarkPaletteJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSystem = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_themes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "app_users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MicrosoftOid = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CronNotificationsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NotificationEmail = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "establishments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_establishments", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "app_cron_executions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CronJobId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Success = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ResultJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_cron_executions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_app_cron_executions_app_cron_jobs_CronJobId",
                        column: x => x.CronJobId,
                        principalTable: "app_cron_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SelectedThemeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_preferences_app_themes_SelectedThemeId",
                        column: x => x.SelectedThemeId,
                        principalTable: "app_themes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_user_preferences_app_users_Id",
                        column: x => x.Id,
                        principalTable: "app_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    AppRoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.AppUserId, x.AppRoleId });
                    table.ForeignKey(
                        name: "FK_user_roles_app_roles_AppRoleId",
                        column: x => x.AppRoleId,
                        principalTable: "app_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_app_users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "app_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "raspberry_pis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstablishmentId = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Model = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KodiUserEncrypted = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KodiPasswordEncrypted = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KodiPort = table.Column<int>(type: "int", nullable: false),
                    SshUserEncrypted = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SshPasswordEncrypted = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SshPort = table.Column<int>(type: "int", nullable: false),
                    VideoFolderPath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raspberry_pis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_raspberry_pis_establishments_EstablishmentId",
                        column: x => x.EstablishmentId,
                        principalTable: "establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "app_cron_jobs",
                columns: new[] { "Id", "CreatedAt", "Description", "IsEnabled", "LastRunAt", "Name", "Schedule" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 4, 30, 14, 29, 1, 769, DateTimeKind.Utc).AddTicks(8459), "Restart any inactive Kodi players and play the video folder on a loop.", false, null, "KodiRestarter", "0 6-16 * * *" },
                    { 2, new DateTime(2026, 4, 30, 14, 29, 1, 769, DateTimeKind.Utc).AddTicks(8465), "Reboot the LibreELEC system on all the Pi devices in the list.", false, null, "KodiRebooter", "50 5 * * *" }
                });

            migrationBuilder.InsertData(
                table: "app_roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Full access (Pi management, transfers, deletion)", "Admin" },
                    { 2, "Pi monitoring, file transfers", "Operator" },
                    { 3, "Read-only — view the status of the Pi", "Viewer" },
                    { 4, "Sole owner — non-revocable admin rights, cannot be configured via the UI", "Owner" }
                });

            migrationBuilder.InsertData(
                table: "app_settings",
                columns: new[] { "Key", "IsSensitive", "UpdatedAt", "Value" },
                values: new object[,]
                {
                    { "Mail:EnableSsl", false, new DateTime(2026, 4, 30, 14, 29, 1, 769, DateTimeKind.Utc).AddTicks(8251), "true" },
                    { "Mail:From", false, new DateTime(2026, 4, 30, 14, 29, 1, 769, DateTimeKind.Utc).AddTicks(8242), "" },
                    { "Mail:FromName", false, new DateTime(2026, 4, 30, 14, 29, 1, 769, DateTimeKind.Utc).AddTicks(8247), "Kodinet" },
                    { "Mail:Password", true, new DateTime(2026, 4, 30, 14, 29, 1, 769, DateTimeKind.Utc).AddTicks(8253), "" },
                    { "Mail:SmtpHost", false, new DateTime(2026, 4, 30, 14, 29, 1, 769, DateTimeKind.Utc).AddTicks(8248), "" },
                    { "Mail:SmtpPort", false, new DateTime(2026, 4, 30, 14, 29, 1, 769, DateTimeKind.Utc).AddTicks(8250), "587" },
                    { "Mail:Username", false, new DateTime(2026, 4, 30, 14, 29, 1, 769, DateTimeKind.Utc).AddTicks(8252), "" }
                });

            migrationBuilder.InsertData(
                table: "app_themes",
                columns: new[] { "Id", "CreatedAt", "DarkPaletteJson", "IsSystem", "LightPaletteJson", "Name", "SwatchColor" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 4, 30, 14, 29, 1, 769, DateTimeKind.Utc).AddTicks(8417), "{\r\n  \"primary\": \"#e08090\",\r\n  \"primaryDarken\": \"#f0a8b4\",\r\n  \"secondary\": \"#7ab89a\",\r\n  \"secondaryDarken\": \"#9acdb5\",\r\n  \"background\": \"#1a0d10\",\r\n  \"surface\": \"#2b1419\",\r\n  \"appbarBackground\": \"#220e13\",\r\n  \"appbarText\": \"#fce8eb\",\r\n  \"textPrimary\": \"#fce8eb\",\r\n  \"textSecondary\": \"#c09090\",\r\n  \"divider\": \"#4a2530\",\r\n  \"error\": \"#ef9a9a\",\r\n  \"success\": \"#a5d6a7\",\r\n  \"warning\": \"#ffcc80\",\r\n  \"info\": \"#e08090\"\r\n}", true, "{\r\n  \"primary\": \"#9D344B\",\r\n  \"primaryDarken\": \"#7a2539\",\r\n  \"secondary\": \"#4A7C5E\",\r\n  \"secondaryDarken\": \"#3a6149\",\r\n  \"background\": \"#fdf5f6\",\r\n  \"surface\": \"#ffffff\",\r\n  \"appbarBackground\": \"#9D344B\",\r\n  \"appbarText\": \"#ffffff\",\r\n  \"textPrimary\": \"#3a1520\",\r\n  \"textSecondary\": \"#9a7580\",\r\n  \"divider\": \"#f0d5da\",\r\n  \"error\": \"#b71c1c\",\r\n  \"success\": \"#4A7C5E\",\r\n  \"warning\": \"#e65100\",\r\n  \"info\": \"#7a2539\"\r\n}", "Ruby", "#9D344B" },
                    { 2, new DateTime(2026, 4, 30, 14, 29, 1, 769, DateTimeKind.Utc).AddTicks(8422), "{\r\n  \"primary\": \"#6aafc5\",\r\n  \"primaryDarken\": \"#90c8d8\",\r\n  \"secondary\": \"#70b8c8\",\r\n  \"secondaryDarken\": \"#98d0dc\",\r\n  \"background\": \"#080f14\",\r\n  \"surface\": \"#101e28\",\r\n  \"appbarBackground\": \"#0d1a22\",\r\n  \"appbarText\": \"#d8eef5\",\r\n  \"textPrimary\": \"#d8eef5\",\r\n  \"textSecondary\": \"#6090a8\",\r\n  \"divider\": \"#1a3a50\",\r\n  \"error\": \"#ef9a9a\",\r\n  \"success\": \"#a5d6a7\",\r\n  \"warning\": \"#ffcc80\",\r\n  \"info\": \"#6aafc5\"\r\n}", true, "{\r\n  \"primary\": \"#28546C\",\r\n  \"primaryDarken\": \"#1a3d52\",\r\n  \"secondary\": \"#3A8FA3\",\r\n  \"secondaryDarken\": \"#2d7388\",\r\n  \"background\": \"#eef5f8\",\r\n  \"surface\": \"#ffffff\",\r\n  \"appbarBackground\": \"#28546C\",\r\n  \"appbarText\": \"#ffffff\",\r\n  \"textPrimary\": \"#0e2030\",\r\n  \"textSecondary\": \"#5a7a8a\",\r\n  \"divider\": \"#c8dfe8\",\r\n  \"error\": \"#c62828\",\r\n  \"success\": \"#2e7d32\",\r\n  \"warning\": \"#ef6c00\",\r\n  \"info\": \"#1a3d52\"\r\n}", "Ocean", "#28546C" },
                    { 3, new DateTime(2026, 4, 30, 14, 29, 1, 769, DateTimeKind.Utc).AddTicks(8424), "{\r\n  \"primary\": \"#d4956a\",\r\n  \"primaryDarken\": \"#e8b898\",\r\n  \"secondary\": \"#aab870\",\r\n  \"secondaryDarken\": \"#c8d090\",\r\n  \"background\": \"#140e06\",\r\n  \"surface\": \"#221608\",\r\n  \"appbarBackground\": \"#1c1205\",\r\n  \"appbarText\": \"#faebd7\",\r\n  \"textPrimary\": \"#faebd7\",\r\n  \"textSecondary\": \"#b09070\",\r\n  \"divider\": \"#3c2810\",\r\n  \"error\": \"#ef9a9a\",\r\n  \"success\": \"#a5d6a7\",\r\n  \"warning\": \"#ffcc80\",\r\n  \"info\": \"#d4956a\"\r\n}", true, "{\r\n  \"primary\": \"#AA6C39\",\r\n  \"primaryDarken\": \"#8a5228\",\r\n  \"secondary\": \"#7A8A3A\",\r\n  \"secondaryDarken\": \"#606e2c\",\r\n  \"background\": \"#faf4ec\",\r\n  \"surface\": \"#ffffff\",\r\n  \"appbarBackground\": \"#AA6C39\",\r\n  \"appbarText\": \"#ffffff\",\r\n  \"textPrimary\": \"#2e1c0a\",\r\n  \"textSecondary\": \"#8a6a50\",\r\n  \"divider\": \"#e8d8c0\",\r\n  \"error\": \"#c62828\",\r\n  \"success\": \"#558b2f\",\r\n  \"warning\": \"#e65100\",\r\n  \"info\": \"#8a5228\"\r\n}", "Amber", "#AA6C39" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_app_cron_executions_CronJobId",
                table: "app_cron_executions",
                column: "CronJobId");

            migrationBuilder.CreateIndex(
                name: "IX_app_roles_Name",
                table: "app_roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_app_users_MicrosoftOid",
                table: "app_users",
                column: "MicrosoftOid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_establishments_Name",
                table: "establishments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_raspberry_pis_EstablishmentId",
                table: "raspberry_pis",
                column: "EstablishmentId");

            migrationBuilder.CreateIndex(
                name: "IX_user_preferences_SelectedThemeId",
                table: "user_preferences",
                column: "SelectedThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_AppRoleId",
                table: "user_roles",
                column: "AppRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_cron_executions");

            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "raspberry_pis");

            migrationBuilder.DropTable(
                name: "user_preferences");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "app_cron_jobs");

            migrationBuilder.DropTable(
                name: "establishments");

            migrationBuilder.DropTable(
                name: "app_themes");

            migrationBuilder.DropTable(
                name: "app_roles");

            migrationBuilder.DropTable(
                name: "app_users");
        }
    }
}
