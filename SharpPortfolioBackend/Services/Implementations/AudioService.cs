using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using FFMpegCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SharpPortfolioBackend.Data;
using SharpPortfolioBackend.Models.DTOs;
using SharpPortfolioBackend.Models.Entities;
using SharpPortfolioBackend.Services.Interfaces;

namespace SharpPortfolioBackend.Services.Implementations;

public class AudioService : IAudioService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AudioService> _logger;
    private readonly string _audioRootPath;

    public AudioService(IDbConnectionFactory dbConnectionFactory, IWebHostEnvironment env, ILogger<AudioService> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _env = env;
        _logger = logger;
        _audioRootPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "audio");
        if (!Directory.Exists(_audioRootPath))
        {
            Directory.CreateDirectory(_audioRootPath);
        }
    }

    public async Task<AudioDto> CreateAudioAsync(CreateAudioDto createDto)
    {
        if (createDto.File == null || !createDto.File.FileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only .wav files are allowed.");
        }

        using var validationConnection = _dbConnectionFactory.Create();
        if (createDto.Vibes != null && createDto.Vibes.Any())
        {
            var existingVibes = (await validationConnection.QueryAsync<string>("SELECT Name FROM Vibes")).Select(v => v.ToUpper()).ToList();
            var invalidVibes = createDto.Vibes.Where(v => !existingVibes.Contains(v.ToUpper())).ToList();
            if (invalidVibes.Any())
            {
                throw new ArgumentException($"The following vibes are not allowed: {string.Join(", ", invalidVibes)}");
            }
        }

        var fileIdentifier = Guid.NewGuid().ToString();
        var folderPath = Path.Combine(_audioRootPath, fileIdentifier);
        Directory.CreateDirectory(folderPath);

        var wavFileName = $"{fileIdentifier}.wav";
        var mp3FileName = $"{fileIdentifier}.mp3";
        var wavPath = Path.Combine(folderPath, wavFileName);
        var mp3Path = Path.Combine(folderPath, mp3FileName);

        using (var stream = new FileStream(wavPath, FileMode.Create))
        {
            await createDto.File.CopyToAsync(stream);
        }

        try
        {
            await FFMpegArguments
                .FromFileInput(wavPath)
                .OutputToFile(mp3Path, false, options => options.WithAudioBitrate(192))
                .ProcessAsynchronously();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting wav to mp3 for file {FileIdentifier}", fileIdentifier);
        }

        using var connection = _dbConnectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var displayName = createDto.File.FileName;

        try
        {
            const string insertAudioSql = @"
                INSERT INTO AudioFiles (DisplayName, DESCRIPTION, FileIdentifier, BPM, MusicKey)
                VALUES (:DisplayName, :Description, :FileIdentifier, :BPM, :MusicKey)
                RETURNING Id INTO :InsertedId";

            var parameters = new DynamicParameters();
            parameters.Add("DisplayName", displayName);
            parameters.Add("Description", createDto.Description);
            parameters.Add("FileIdentifier", fileIdentifier);
            parameters.Add("BPM", createDto.BPM);
            parameters.Add("MusicKey", createDto.MusicKey);
            parameters.Add("InsertedId", dbType: DbType.Int64, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(insertAudioSql, parameters, transaction);
            var audioId = parameters.Get<long>("InsertedId");

            if (createDto.Vibes != null && createDto.Vibes.Any())
            {
                await SyncVibesAsync(connection, transaction, audioId, createDto.Vibes);
            }

            transaction.Commit();
            return new AudioDto(audioId, displayName, createDto.Description, fileIdentifier, createDto.BPM, createDto.MusicKey, createDto.Vibes ?? new List<string>());
        }
        catch
        {
            transaction.Rollback();
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }
            throw;
        }
    }

    public async Task<AudioDto?> GetAudioMetadataAsync(string fileIdentifier)
    {
        using var connection = _dbConnectionFactory.Create();
        const string sql = @"
            SELECT af.Id, af.DisplayName, af.DESCRIPTION as Description, af.FileIdentifier, af.BPM, af.MusicKey, v.Name as VibeName
            FROM AudioFiles af
            LEFT JOIN AudioFilesVibes afv ON af.Id = afv.AudioFileId
            LEFT JOIN Vibes v ON afv.VibeId = v.Id
            WHERE af.FileIdentifier = :FileIdentifier";

        AudioDto? audioDto = null;
        var vibes = new List<string>();

        await connection.QueryAsync<dynamic, string, dynamic>(
            sql,
            (af, vibeName) =>
            {
                if (audioDto == null)
                {
                    audioDto = new AudioDto(
                        Convert.ToInt64(af.ID),
                        (string)af.DISPLAYNAME,
                        (string)af.DESCRIPTION,
                        (string)af.FILEIDENTIFIER,
                        af.BPM != null ? Convert.ToSingle(af.BPM) : null,
                        (string?)af.MUSICKEY,
                        vibes
                    );
                }
                if (vibeName != null)
                {
                    vibes.Add(vibeName);
                }
                return af;
            },
            new { FileIdentifier = fileIdentifier },
            splitOn: "VibeName"
        );

        return audioDto;
    }

    public async Task<(IEnumerable<AudioDto> Items, int TotalCount)> SearchAudioAsync(string? query, List<string>? vibes, string? key, float? bpm, int page, int pageSize)
    {
        using var connection = _dbConnectionFactory.Create();
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query))
        {
            whereClauses.Add("(UPPER(DisplayName) LIKE UPPER(:Query) OR UPPER(DESCRIPTION) LIKE UPPER(:Query))");
            parameters.Add("Query", $"%{query}%");
        }

        if (vibes != null && vibes.Any())
        {
            whereClauses.Add(@"EXISTS (
                SELECT 1 FROM AudioFilesVibes afv 
                JOIN Vibes v ON afv.VibeId = v.Id 
                WHERE afv.AudioFileId = af.Id AND UPPER(v.Name) IN :VibeNames
            )");
            parameters.Add("VibeNames", vibes.Select(v => v.ToUpper()).ToArray());
        }

        if (!string.IsNullOrWhiteSpace(key))
        {
            whereClauses.Add("MusicKey = :Key");
            parameters.Add("Key", key);
        }

        if (bpm.HasValue)
        {
            whereClauses.Add("BPM = :Bpm");
            parameters.Add("Bpm", bpm.Value);
        }

        var whereSql = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";
        
        var countSql = $"SELECT COUNT(*) FROM AudioFiles af {whereSql}";
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        var pagedSql = $@"
            SELECT af.Id, af.DisplayName, af.DESCRIPTION as Description, af.FileIdentifier, af.BPM, af.MusicKey 
            FROM AudioFiles af
            {whereSql}
            ORDER BY af.CreatedAt DESC
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY";

        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var audioFiles = await connection.QueryAsync<AudioFile>(pagedSql, parameters);
        
        var results = new List<AudioDto>();
        foreach (var af in audioFiles)
        {
            const string vibeSql = @"
                SELECT v.Name FROM Vibes v
                JOIN AudioFilesVibes afv ON v.Id = afv.VibeId
                WHERE afv.AudioFileId = :Id";
            var afVibes = (await connection.QueryAsync<string>(vibeSql, new { Id = af.Id })).ToList();

            results.Add(new AudioDto(
                af.Id,
                af.DisplayName,
                af.Description,
                af.FileIdentifier,
                af.BPM,
                af.MusicKey,
                afVibes
            ));
        }

        return (results, totalCount);
    }

    public async Task UpdateAudioAsync(string fileIdentifier, UpdateAudioDto updateDto)
    {
        using var connection = _dbConnectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            const string updateSql = @"
                UPDATE AudioFiles SET 
                    DESCRIPTION = :Description, 
                    BPM = :BPM, 
                    MusicKey = :MusicKey,
                    UpdatedAt = SYSTIMESTAMP
                WHERE FileIdentifier = :FileIdentifier";

            var affected = await connection.ExecuteAsync(updateSql, new
            {
                updateDto.Description,
                updateDto.BPM,
                updateDto.MusicKey,
                FileIdentifier = fileIdentifier
            }, transaction);

            if (affected == 0)
            {
                throw new KeyNotFoundException($"Audio file with identifier {fileIdentifier} not found.");
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task DeleteAudioAsync(string fileIdentifier)
    {
        var audio = await GetAudioMetadataAsync(fileIdentifier);
        if (audio == null) return;

        using var connection = _dbConnectionFactory.Create();
        await connection.ExecuteAsync("DELETE FROM AudioFiles WHERE FileIdentifier = :FileIdentifier", new { FileIdentifier = fileIdentifier });

        var folderPath = Path.Combine(_audioRootPath, fileIdentifier);
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, true);
        }
    }

    public async Task<byte[]?> GetAudioFileAsync(string fileIdentifier, string extension)
    {
        var filePath = Path.Combine(_audioRootPath, fileIdentifier, $"{fileIdentifier}{extension}");
        if (!File.Exists(filePath)) return null;
        return await File.ReadAllBytesAsync(filePath);
    }

    public async Task<byte[]?> DownloadMultipleAsync(List<string> fileIdentifiers)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var fileIdentifier in fileIdentifiers)
            {
                var folderPath = Path.Combine(_audioRootPath, fileIdentifier);
                if (Directory.Exists(folderPath))
                {
                    var audioMetadata = await GetAudioMetadataAsync(fileIdentifier);
                    var displayName = audioMetadata?.DisplayName ?? fileIdentifier;

                    var wavFile = Path.Combine(folderPath, $"{fileIdentifier}.wav");
                    var mp3File = Path.Combine(folderPath, $"{fileIdentifier}.mp3");

                    if (File.Exists(wavFile))
                    {
                        archive.CreateEntryFromFile(wavFile, $"{displayName}/{displayName}.wav");
                    }
                    if (File.Exists(mp3File))
                    {
                        archive.CreateEntryFromFile(mp3File, $"{displayName}/{displayName}.mp3");
                    }
                }
            }
        }
        return memoryStream.ToArray();
    }

    private async Task SyncVibesAsync(IDbConnection connection, IDbTransaction transaction, long audioId, List<string> vibeNames)
    {
        foreach (var vibeName in vibeNames)
        {
            const string getVibeSql = "SELECT Id FROM Vibes WHERE UPPER(Name) = UPPER(:Name)";
            var vibeId = await connection.ExecuteScalarAsync<long?>(getVibeSql, new { Name = vibeName }, transaction);

            if (vibeId == null)
            {
                throw new ArgumentException($"Vibe {vibeName} does not exist.");
            }

            const string linkSql = "INSERT INTO AudioFilesVibes (AudioFileId, VibeId) VALUES (:AudioId, :VibeId)";
            await connection.ExecuteAsync(linkSql, new { AudioId = audioId, VibeId = vibeId }, transaction);
        }
    }
}