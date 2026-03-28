using System.Reflection;
using Dapper;
using DbUp;
using DbUp.Oracle;
using SharpPortfolioBackend.Data;
using Microsoft.Extensions.Logging;
using DotNetEnv;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;
using SharpPortfolioBackend.Services.Implementations;
using SharpPortfolioBackend.Services.Interfaces;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<IAudioService, AudioService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();
var logger = app.Logger;

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var factory = app.Services.GetRequiredService<IDbConnectionFactory>();
var connection = ((DbConnectionFactory)factory).GetConnectionString();


var upgrader = DeployChanges.To.
    OracleDatabaseWithDefaultDelimiter(connection).
    WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), s=> s.EndsWith(".sql"))
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    logger.LogError("Error updating database");
    throw new Exception("Error updating database");   
}
logger.LogInformation("Database upgrade completed successfully");

app.UseStaticFiles();
app.MapControllers();

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


app.MapGet("/tables", async (IDbConnectionFactory factory, ILogger<DbConnectionFactory> logger) =>
{
    try
    {
        using var connection = factory.Create();
        connection.Open();
        var tables = await connection.QueryAsync<String>("SELECT table_name FROM user_tables");
        return Results.Ok(tables);
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Error connecting to database");
        return Results.InternalServerError();
    }
});
app.Run();