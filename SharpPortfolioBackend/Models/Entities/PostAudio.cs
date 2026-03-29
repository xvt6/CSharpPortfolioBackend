namespace SharpPortfolioBackend.Models.Entities;

public class PostAudio
{
    public long PostId { get; set; }
    public long AudioFileId { get; set; }

    public virtual Post Post { get; set; } = null!;
    public virtual AudioFile AudioFile { get; set; } = null!;
}