namespace SharpPortfolioBackend.Models.DTOs;


public record CreatePostDto(string Title, string Content, List<long> AudioFileIds);

public record UpdatePostDto(string Title, string Content, List<long> AudioFileIds);

public record PostResponseDto(long Id, string Title, string Content, DateTime CreatedAt, DateTime UpdatedAt, List<AudioDto> AudioFiles);
