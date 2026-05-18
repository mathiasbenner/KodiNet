namespace KodiNet.Domain.Entities;

public sealed class AppSetting
{
    public string Key { get; set; } = "";  // "Mail:SmtpHost", etc.
    public string Value { get; set; } = "";  // encrypted if IsSensitive
    public bool IsSensitive { get; set; }                  // true = AES via DataProtection
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}