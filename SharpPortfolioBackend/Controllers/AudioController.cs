using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SharpPortfolioBackend.Models.DTOs;
using SharpPortfolioBackend.Services.Interfaces;

namespace SharpPortfolioBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AudioController : ControllerBase
{
    private readonly IAudioService _audioService;

    public AudioController(IAudioService audioService)
    {
        _audioService = audioService;
    }

    [HttpPost]
    public async Task<ActionResult<AudioDto>> Create([FromForm] CreateAudioDto createDto)
    {
        try
        {
            var result = await _audioService.CreateAudioAsync(createDto);
            return CreatedAtAction(nameof(GetMetadata), new { fileIdentifier = result.FileIdentifier }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{fileIdentifier}/metadata")]
    public async Task<ActionResult<AudioDto>> GetMetadata(string fileIdentifier)
    {
        var result = await _audioService.GetAudioMetadataAsync(fileIdentifier);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AudioDto>>> Search(
        [FromQuery] string? query, 
        [FromQuery] List<string>? vibes, 
        [FromQuery] string? key,
        [FromQuery] float? bpm,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        var (items, totalCount) = await _audioService.SearchAudioAsync(query, vibes, key, bpm, page, pageSize);
        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        return Ok(items);
    }

    [HttpPut("{fileIdentifier}")]
    public async Task<IActionResult> Update(string fileIdentifier, [FromBody] UpdateAudioDto updateDto)
    {
        try
        {
            await _audioService.UpdateAudioAsync(fileIdentifier, updateDto);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("identifiers")]
    public async Task<ActionResult<IEnumerable<string>>> GetAllIdentifiers()
    {
        var identifiers = await _audioService.GetAllIdentifiersAsync();
        return Ok(identifiers);
    }

    [HttpDelete("{fileIdentifier}")]
    public async Task<IActionResult> Delete(string fileIdentifier)
    {
        await _audioService.DeleteAudioAsync(fileIdentifier);
        return NoContent();
    }

    [HttpDelete("all")]
    public async Task<IActionResult> DeleteAll()
    {
        await _audioService.DeleteAllAudioAsync();
        return NoContent();
    }

    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] List<string> fileIdentifiers)
    {
        await _audioService.BulkDeleteAudioAsync(fileIdentifiers);
        return NoContent();
    }

    [HttpPost("download-multiple")]
    public async Task<IActionResult> DownloadMultiple([FromBody] List<string> fileIdentifiers)
    {
        var zipBytes = await _audioService.DownloadMultipleAsync(fileIdentifiers);
        if (zipBytes == null || zipBytes.Length == 0) return NotFound();
        return File(zipBytes, "application/zip", "audio_files.zip");
    }

    [HttpGet("{fileIdentifier}/mp3")]
    public async Task<IActionResult> GetMp3(string fileIdentifier)
    {
        var result = await _audioService.GetAudioFileAsync(fileIdentifier, ".mp3");
        if (result == null) return NotFound();
        return File(result.Bytes, "audio/mpeg", $"{result.DisplayName}.mp3");
    }

    [HttpGet("{fileIdentifier}/wav")]
    public async Task<IActionResult> GetWav(string fileIdentifier)
    {
        var result = await _audioService.GetAudioFileAsync(fileIdentifier, ".wav");
        if (result == null) return NotFound();
        return File(result.Bytes, "audio/wav", $"{result.DisplayName}.wav");
    }
}