namespace SharpPortfolioBackend.Models.Entities;

public class Post
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<AudioFile> AudioFiles { get; set; } = new();
}