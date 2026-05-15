namespace KodiNet.Domain.Entities;

/// <summary>
/// Represents a Raspberry Pi registered in the organization.
/// A Pi is only usable by KodiNet if it runs LibreELEC + Kodi.
/// </summary>
public sealed class RaspberryPi
{
    public int    Id            { get; set; }
    public string Name         { get; set; } = "";
    public string IpAddress    { get; set; } = "";
    public int EstablishmentId { get; set; }
    public Establishment Establishment { get; set; } = null!;
    public string Location     { get; set; } = "";
    public string Model        { get; set; } = "";

    // Kodi credentials encrypted (AES via DataProtection API)
    public string KodiUserEncrypted     { get; set; } = "";
    public string KodiPasswordEncrypted { get; set; } = "";
    public int    KodiPort              { get; set; } = 8080;

    // SSH/SFTP credentials encrypted
    public string SshUserEncrypted     { get; set; } = "";
    public string SshPasswordEncrypted { get; set; } = "";
    public int    SshPort              { get; set; } = 22;

    // Path to the video folder on the Pi
    public string VideoFolderPath { get; set; } = "/storage/videos";

    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}