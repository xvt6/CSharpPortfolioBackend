using Dapper;
using SharpPortfolioBackend.Data;
using Microsoft.Extensions.Logging;
using DotNetEnv;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapGet("/test-db", async (IDbConnectionFactory factory, ILogger<DbConnectionFactory> logger) =>
{
    try
    {
        using var connection = factory.Create();
        connection.Open();
        var result = await connection.QueryAsync<String>("SELECT 'Hello Oracle XE' FROM Dual");
        return Results.Ok(result);
    }
    catch (Exception e)
    {
        logger.LogError(e, "Error connecting to database");
        return Results.InternalServerError();
    }
});

app.Run();