using System.Collections.Generic;
using System.Threading.Tasks;
using SharpPortfolioBackend.Models.DTOs;

namespace SharpPortfolioBackend.Services.Interfaces;

public interface IAudioService
{
    Task<AudioDto> CreateAudioAsync(CreateAudioDto createDto);
    Task<AudioDto?> GetAudioMetadataAsync(string fileIdentifier);
    Task<(IEnumerable<AudioDto> Items, int TotalCount)> SearchAudioAsync(string? query, List<string>? vibes, string? key, float? bpm, int page, int pageSize);
    Task UpdateAudioAsync(string fileIdentifier, UpdateAudioDto updateDto);
    Task DeleteAudioAsync(string fileIdentifier);
    Task<byte[]?> GetAudioFileAsync(string fileIdentifier, string extension);
    Task<byte[]?> DownloadMultipleAsync(List<string> fileIdentifiers);
}