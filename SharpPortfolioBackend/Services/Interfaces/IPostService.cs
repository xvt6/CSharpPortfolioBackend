using SharpPortfolioBackend.Models.DTOs;

namespace SharpPortfolioBackend.Services.Interfaces;

public interface IPostService
{
    Task<PostResponseDto?> GetPostByIdAsync(long id);
    Task<IEnumerable<PostResponseDto>> GetAllPostsAsync();
    Task<PostResponseDto> CreatePostAsync(CreatePostDto createPostDto);
    Task<PostResponseDto?> UpdatePostAsync(long id, UpdatePostDto updatePostDto);
    Task<bool> DeletePostAsync(long id);
}