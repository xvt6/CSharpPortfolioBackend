using System;
using System.Data;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;

namespace SharpPortfolioBackend.Data
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;
        private readonly ILogger<DbConnectionFactory> _logger;

        public DbConnectionFactory(ILogger<DbConnectionFactory> logger)
        {
            _logger = logger;
            var user = Environment.GetEnvironmentVariable("ORACLE_USER");
            var password = Environment.GetEnvironmentVariable("ORACLE_PASSWORD");
            var host = Environment.GetEnvironmentVariable("ORACLE_HOST");
            var port = Environment.GetEnvironmentVariable("ORACLE_PORT");
            var service = Environment.GetEnvironmentVariable("ORACLE_SERVICE");
            _connectionString = $"User Id={user};Password={password};Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port}))(CONNECT_DATA=(SERVICE_NAME={service})))";

            _logger.LogDebug("Oracle connection string constructed: {ConnectionString}", _connectionString);
        }

        public IDbConnection Create()
        {
            _logger.LogDebug("Creating Oracle connection");
            return new OracleConnection(_connectionString);
        }
        
        public string GetConnectionString() => _connectionString;
    }
}