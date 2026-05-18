namespace KodiNet.Domain.Entities;

/// <summary>An establishment of the organization to which a Pi is attached.</summary>
public sealed class Establishment
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RaspberryPi> RaspberryPis { get; set; } = [];
}