using SharpPortfolioBackend.Models.DTOs;

namespace SharpPortfolioBackend.Services.Interfaces;

public interface IPostService
{
    Task<PostResponseDto?> GetPostByIdAsync(long id);
    Task<IEnumerable<PostResponseDto>> GetAllPostsAsync();
    Task<(IEnumerable<PostResponseDto> Items, int TotalCount)> SearchPostsAsync(string? query, List<string>? vibes, int page, int pageSize);
    Task<PostResponseDto> CreatePostAsync(CreatePostDto createPostDto);
    Task<PostResponseDto?> UpdatePostAsync(long id, UpdatePostDto updatePostDto);
    Task<bool> DeletePostAsync(long id);
}