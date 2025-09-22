using System;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using bulkDURLoader.Logging;

namespace bulkDURLoader.Database
{
    public class DatabaseConfig
    {
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;

        public DatabaseConfig(IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = Logger.Instance;
        }

        public string ConnectionString => _configuration.GetConnectionString("OracleConnection") ??
            throw new InvalidOperationException("Oracle connection string not found in configuration");

        public int CommandTimeout => _configuration.GetValue<int>("DatabaseSettings:CommandTimeout", 60);
        public int MaxRetries => _configuration.GetValue<int>("DatabaseSettings:MaxRetries", 3);
        public int RetryDelay => _configuration.GetValue<int>("DatabaseSettings:RetryDelay", 1000);

        public async Task<bool> TestConnectionAsync()
        {
            var retryCount = 0;
            while (retryCount <= MaxRetries)
            {
                try
                {
                    _logger.Info($"Testing Oracle database connection (attempt {retryCount + 1}/{MaxRetries + 1})");

                    using var connection = new OracleConnection(ConnectionString);
                    await connection.OpenAsync();

                    using var command = new OracleCommand("SELECT 1 FROM DUAL", connection);
                    command.CommandTimeout = CommandTimeout;

                    var result = await command.ExecuteScalarAsync();

                    _logger.Info("Oracle database connection successful");
                    return true;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.Warning($"Database connection attempt {retryCount} failed: {ex.Message}");

                    if (retryCount <= MaxRetries)
                    {
                        _logger.Info($"Retrying in {RetryDelay}ms...");
                        await Task.Delay(RetryDelay);
                    }
                    else
                    {
                        _logger.Error("All database connection attempts failed", ex);
                        return false;
                    }
                }
            }
            return false;
        }

        public async Task<OracleConnection> GetConnectionAsync()
        {
            try
            {
                var connection = new OracleConnection(ConnectionString);
                await connection.OpenAsync();
                _logger.Debug("New Oracle database connection opened");
                return connection;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to open database connection", ex);
                throw;
            }
        }

        public void LogConnectionInfo()
        {
            try
            {
                var builder = new OracleConnectionStringBuilder(ConnectionString);
                _logger.Info($"Database Configuration:");
                _logger.Info($"  Data Source: {builder.DataSource}");
                _logger.Info($"  User ID: {builder.UserID}");
                _logger.Info($"  Connection Timeout: {builder.ConnectionTimeout}");
                _logger.Info($"  Command Timeout: {CommandTimeout}");
                _logger.Info($"  Max Retries: {MaxRetries}");
            }
            catch (Exception ex)
            {
                _logger.Warning($"Could not parse connection string for logging: {ex.Message}");
            }
        }
    }
}