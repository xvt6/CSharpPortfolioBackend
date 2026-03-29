using Microsoft.AspNetCore.Mvc;
using SharpPortfolioBackend.Models.DTOs;
using SharpPortfolioBackend.Services.Interfaces;

namespace SharpPortfolioBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postsService;

    public PostsController(IPostService postsService)
    {
        _postsService = postsService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PostResponseDto>>> GetAll(
        [FromQuery] string? query, 
        [FromQuery] List<string>? vibes, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        var (items, totalCount) = await _postsService.SearchPostsAsync(query, vibes, page, pageSize);
        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PostResponseDto>> GetById(long id)
    {
        var result = await _postsService.GetPostByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PostResponseDto>> Create([FromBody] CreatePostDto createPostDto)
    {
        var result = await _postsService.CreatePostAsync(createPostDto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PostResponseDto>> Update(long id, [FromBody] UpdatePostDto updatePostDto)
    {
        var result = await _postsService.UpdatePostAsync(id, updatePostDto);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var deleted = await _postsService.DeletePostAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}