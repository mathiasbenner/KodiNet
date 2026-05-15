using System.Net.Mail;
using System.Text;
using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KodiNet.Infrastructure.Services;

public sealed class MailNotificationService(
    IDbContextFactory<AppDbContext> dbFactory,
    IAppSettingService settingSvc,
    ILogger<MailNotificationService> logger) : IMailNotificationService
{
    public async Task SendCronReportAsync(CronJobResultDto result, CancellationToken ct = default)
    {
        // Retrieve subscribers for cron notifications
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var subscribers = await db.Users
            .AsNoTracking()
            .Where(p => p.CronNotificationsEnabled)
            .ToListAsync(ct);

        if (subscribers.Count == 0) return;

        var smtp = await BuildSmtpClientAsync(ct);
        var from = await settingSvc.GetAsync("Mail:From", ct) ?? "";
        var fromName = await settingSvc.GetAsync("Mail:FromName", ct) ?? "KodiNet";
        var body   = BuildHtmlBody(result);

        foreach (var sub in subscribers)
        {
            var to = sub.NotificationEmail ?? sub.Email;
            if (string.IsNullOrWhiteSpace(to)) continue;

            try
            {
                using var msg = new MailMessage
                {
                    From       = new MailAddress(from, fromName),
                    Subject    = $"[KodiNet] Cron Report — {result.ExecutedAt:dd/MM/yyyy HH:mm} UTC",
                    Body       = body,
                    IsBodyHtml = true
                };
                msg.To.Add(to);
                await smtp.SendMailAsync(msg, ct);
                logger.LogInformation("Mail cron sent to {Email}", to);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send cron mail to {Email}", to);
            }
        }
    }

    private async Task<SmtpClient> BuildSmtpClientAsync(CancellationToken ct)
    {
        var host     = await settingSvc.GetAsync("Mail:SmtpHost", ct) ?? "localhost";
        var port     = int.Parse(await settingSvc.GetAsync("Mail:SmtpPort", ct) ?? "587");
        var ssl      = bool.Parse(await settingSvc.GetAsync("Mail:EnableSsl", ct) ?? "true");
        var username = await settingSvc.GetAsync("Mail:Username", ct);
        var password = await settingSvc.GetAsync("Mail:Password", ct);
        var client = new SmtpClient(host, port) { EnableSsl = ssl };
        if (!string.IsNullOrEmpty(username))
            client.Credentials = new System.Net.NetworkCredential(username, password);
        return client;
    }

    private static string BuildHtmlBody(CronJobResultDto r)
    {
        var sb = new StringBuilder();
        sb.Append("<h2>KodiNet Execution Report</h2>");
        sb.Append($"<p><b>Executed on:</b> {r.ExecutedAt:dd/MM/yyyy HH:mm} UTC</p>");
        sb.Append($"<p>Pis checked: <b>{r.PisChecked}</b> — "
                + $"Restarted: <b style='color:green'>{r.PisRestarted}</b> — "
                + $"Errors: <b style='color:red'>{r.PisFailed}</b></p>");
        sb.Append("<table border='1' cellpadding='6' style='border-collapse:collapse'>");
        sb.Append("<tr><th>Pi</th><th>Previous Status</th><th>Restarted</th><th>Remark</th></tr>");
        foreach (var d in r.Details)
        {
            var icon = d.Restarted ? "✅" : (d.ErrorMessage is null ? "▶️" : "❌");
            sb.Append($"<tr><td>{d.PiName}</td><td>{d.PreviousStatus}</td>"
                    + $"<td>{icon}</td><td>{d.ErrorMessage ?? "—"}</td></tr>");
        }
        sb.Append("</table>");
        return sb.ToString();
    }
}