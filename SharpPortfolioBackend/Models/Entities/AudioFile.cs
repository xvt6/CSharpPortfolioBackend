namespace SharpPortfolioBackend.Models.Entities;
using System.Collections.Generic;

public class AudioFile
{
    public long Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FileIdentifier { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public float? BPM { get; set; }
    public string? MusicKey { get; set; }
    public virtual ICollection<Vibe> Vibes { get; set; } = new List<Vibe>();
}