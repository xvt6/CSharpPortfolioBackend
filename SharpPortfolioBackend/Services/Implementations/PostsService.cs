using System.Data;
using Dapper;
using SharpPortfolioBackend.Data;
using SharpPortfolioBackend.Models.DTOs;
using SharpPortfolioBackend.Models.Entities;
using SharpPortfolioBackend.Services.Interfaces;

namespace SharpPortfolioBackend.Services.Implementations;

public class PostsService : IPostService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public PostsService(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<PostResponseDto?> GetPostByIdAsync(long id)
    {
        using var connection = _dbConnectionFactory.Create();
        const string postSql = "SELECT Id, Title, Content, CreatedAt, UpdatedAt FROM Posts WHERE Id = :Id";
        var post = await connection.QuerySingleOrDefaultAsync<Post>(postSql, new { Id = id });

        if (post == null) return null;

        const string audioWithVibesSql = @"
            SELECT af.Id, af.DisplayName, af.DESCRIPTION as Description, af.FileIdentifier, af.BPM, af.MusicKey,
                   v.Name
            FROM AudioFiles af
            JOIN PostsAudio pa ON af.Id = pa.AudioFileId
            LEFT JOIN AudioFilesVibes afv ON af.Id = afv.AudioFileId
            LEFT JOIN Vibes v ON afv.VibeId = v.Id
            WHERE pa.PostId = :PostId";
        
        var audioLookup = new Dictionary<long, AudioDto>();
        await connection.QueryAsync<AudioFile, string, AudioFile>(
            audioWithVibesSql,
            (af, vibeName) =>
            {
                if (!audioLookup.TryGetValue(af.Id, out var audioDto))
                {
                    audioDto = new AudioDto(af.Id, af.DisplayName, af.Description, af.FileIdentifier, af.BPM, af.MusicKey, new List<string>());
                    audioLookup.Add(af.Id, audioDto);
                }
                if (!string.IsNullOrEmpty(vibeName))
                {
                    audioDto.Vibes.Add(vibeName);
                }
                return af;
            },
            new { PostId = id },
            splitOn: "Name");

        return new PostResponseDto(
            post.Id, 
            post.Title, 
            post.Content, 
            post.CreatedAt, 
            post.UpdatedAt, 
            audioLookup.Values.ToList());
    }

    public async Task<IEnumerable<PostResponseDto>> GetAllPostsAsync()
    {
        using var connection = _dbConnectionFactory.Create();
        const string postsSql = "SELECT Id, Title, Content, CreatedAt, UpdatedAt FROM Posts ORDER BY CreatedAt DESC";
        var posts = (await connection.QueryAsync<Post>(postsSql)).ToList();

        if (!posts.Any()) return Enumerable.Empty<PostResponseDto>();

        return await EnrichPostsWithAudioAsync(connection, posts);
    }

    public async Task<(IEnumerable<PostResponseDto> Items, int TotalCount)> SearchPostsAsync(string? query, List<string>? vibes, int page, int pageSize)
    {
        using var connection = _dbConnectionFactory.Create();
        var sql = "FROM Posts p";
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query))
        {
            whereClauses.Add("(LOWER(p.Title) LIKE :Query OR LOWER(p.Content) LIKE :Query)");
            parameters.Add("Query", $"%{query.ToLower()}%");
        }

        if (vibes != null && vibes.Any())
        {
            whereClauses.Add(@"EXISTS (
                SELECT 1 FROM PostsAudio pa 
                JOIN AudioFilesVibes afv ON pa.AudioFileId = afv.AudioFileId 
                JOIN Vibes v ON afv.VibeId = v.Id 
                WHERE pa.PostId = p.Id AND v.Name IN :Vibes
            )");
            parameters.Add("Vibes", vibes);
        }

        if (whereClauses.Any())
        {
            sql += " WHERE " + string.Join(" AND ", whereClauses);
        }

        var countSql = "SELECT COUNT(DISTINCT p.Id) " + sql;
        int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        if (totalCount == 0) return (Enumerable.Empty<PostResponseDto>(), 0);

        var itemsSql = "SELECT p.Id, p.Title, p.Content, p.CreatedAt, p.UpdatedAt " + sql + 
                       " ORDER BY p.CreatedAt DESC OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY";
        
        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var posts = (await connection.QueryAsync<Post>(itemsSql, parameters)).ToList();
        var enrichedPosts = await EnrichPostsWithAudioAsync(connection, posts);
        
        return (enrichedPosts, totalCount);
    }

    private async Task<IEnumerable<PostResponseDto>> EnrichPostsWithAudioAsync(IDbConnection connection, List<Post> posts)
    {
        if (!posts.Any()) return Enumerable.Empty<PostResponseDto>();

        const string audioWithVibesSql = @"
            SELECT pa.PostId, 
                   af.Id, af.DisplayName, af.DESCRIPTION as Description, af.FileIdentifier, af.BPM, af.MusicKey,
                   v.Name
            FROM AudioFiles af
            JOIN PostsAudio pa ON af.Id = pa.AudioFileId
            LEFT JOIN AudioFilesVibes afv ON af.Id = afv.AudioFileId
            LEFT JOIN Vibes v ON afv.VibeId = v.Id
            WHERE pa.PostId IN :PostIds";

        var postIds = posts.Select(p => p.Id).ToList();
        
        var rawData = await connection.QueryAsync<dynamic>(audioWithVibesSql, new { PostIds = postIds });
        
        var postsAudioMap = new Dictionary<long, Dictionary<long, AudioDto>>();

        foreach (var row in rawData)
        {
            long postId = Convert.ToInt64(row.POSTID);
            long audioId = Convert.ToInt64(row.ID);
            string? vibeName = row.NAME as string;

            if (!postsAudioMap.ContainsKey(postId))
                postsAudioMap[postId] = new Dictionary<long, AudioDto>();

            if (!postsAudioMap[postId].TryGetValue(audioId, out var audioDto))
            {
                audioDto = new AudioDto(
                    audioId,
                    (string)row.DISPLAYNAME,
                    (string)row.DESCRIPTION,
                    (string)row.FILEIDENTIFIER,
                    row.BPM != null ? Convert.ToSingle(row.BPM) : null,
                    (string)row.MUSICKEY,
                    new List<string>()
                );
                postsAudioMap[postId][audioId] = audioDto;
            }

            if (!string.IsNullOrEmpty(vibeName))
            {
                audioDto.Vibes.Add(vibeName);
            }
        }

        return posts.Select(p =>
        {
            var audioFiles = postsAudioMap.ContainsKey(p.Id) ? postsAudioMap[p.Id].Values.ToList() : new List<AudioDto>();
            return new PostResponseDto(
                p.Id,
                p.Title,
                p.Content,
                p.CreatedAt,
                p.UpdatedAt,
                audioFiles
            );
        }).ToList();
    }

    public async Task<PostResponseDto> CreatePostAsync(CreatePostDto createPostDto)
    {
        using var connection = _dbConnectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            const string insertPostSql = @"
                INSERT INTO Posts (Title, Content)
                VALUES (:Title, :Content)
                RETURNING Id INTO :InsertedId";

            var parameters = new DynamicParameters();
            parameters.Add("Title", createPostDto.Title);
            parameters.Add("Content", createPostDto.Content);
            parameters.Add("InsertedId", dbType: DbType.Int64, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(insertPostSql, parameters, transaction);
            var postId = parameters.Get<long>("InsertedId");

            if (createPostDto.AudioFileIdentifiers != null && createPostDto.AudioFileIdentifiers.Any())
            {
                const string getAudioIdsSql = "SELECT Id FROM AudioFiles WHERE FileIdentifier IN :Identifiers";
                var audioIds = await connection.QueryAsync<long>(getAudioIdsSql, new { Identifiers = createPostDto.AudioFileIdentifiers }, transaction);

                foreach (var audioId in audioIds)
                {
                    await connection.ExecuteAsync(
                        "INSERT INTO PostsAudio (PostId, AudioFileId) VALUES (:PostId, :AudioId)",
                        new { PostId = postId, AudioId = audioId },
                        transaction);
                }
            }

            transaction.Commit();
            return (await GetPostByIdAsync(postId))!;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<PostResponseDto?> UpdatePostAsync(long id, UpdatePostDto updatePostDto)
    {
        using var connection = _dbConnectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            const string updatePostSql = @"
                UPDATE Posts SET Title = :Title, Content = :Content, UpdatedAt = SYSTIMESTAMP
                WHERE Id = :Id";

            var affected = await connection.ExecuteAsync(updatePostSql, new
            {
                updatePostDto.Title,
                updatePostDto.Content,
                Id = id
            }, transaction);

            if (affected == 0)
            {
                transaction.Rollback();
                return null;
            }

            // Update audio associations
            await connection.ExecuteAsync("DELETE FROM PostsAudio WHERE PostId = :PostId", new { PostId = id }, transaction);
            
            if (updatePostDto.AudioFileIdentifiers != null && updatePostDto.AudioFileIdentifiers.Any())
            {
                const string getAudioIdsSql = "SELECT Id FROM AudioFiles WHERE FileIdentifier IN :Identifiers";
                var audioIds = await connection.QueryAsync<long>(getAudioIdsSql, new { Identifiers = updatePostDto.AudioFileIdentifiers }, transaction);

                foreach (var audioId in audioIds)
                {
                    await connection.ExecuteAsync(
                        "INSERT INTO PostsAudio (PostId, AudioFileId) VALUES (:PostId, :AudioId)",
                        new { PostId = id, AudioId = audioId },
                        transaction);
                }
            }

            transaction.Commit();
            return await GetPostByIdAsync(id);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> DeletePostAsync(long id)
    {
        using var connection = _dbConnectionFactory.Create();
        const string deleteSql = "DELETE FROM Posts WHERE Id = :Id";
        var affected = await connection.ExecuteAsync(deleteSql, new { Id = id });
        return affected > 0;
    }
}