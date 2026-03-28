using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace SharpPortfolioBackend.Models.DTOs;

public record AudioDto(
    long Id,
    string DisplayName,
    string Description,
    string FileIdentifier,
    float? BPM,
    string? MusicKey,
    List<string> Vibes
);

public record CreateAudioDto(
    string Description,
    float? BPM,
    string? MusicKey,
    List<string> Vibes,
    IFormFile File
);

public record UpdateAudioDto(
    string Description,
    float? BPM,
    string? MusicKey
);