using Dapper;
using Npgsql;

namespace UrlShortening.Api.Services
{
    public class DatabaseInitializer(NpgsqlDataSource dataSource, 
                                     IConfiguration configuration,
                                     ILogger<DatabaseInitializer> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await CreateDatabaseIfNotExists(stoppingToken);
                await InitializeSchema(stoppingToken);

                logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error initializing database");
                throw;
            }
        }

        private async Task CreateDatabaseIfNotExists(CancellationToken stoppingToken)
        {
            var connectionString = configuration.GetConnectionString("url-shortener");
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            string? databaseName = builder.Database;
            builder.Database = "postgres";

            await using var connection = new NpgsqlConnection(builder.ToString());
            await connection.OpenAsync(stoppingToken);

            bool databaseExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname = @databaseName);",
                new { databaseName }
            );

            if (!databaseExists)
            {
                logger.LogInformation("Creating database {DatabaseName}", databaseName);
                await connection.ExecuteAsync($"CREATE DATABASE \"{databaseName}\"");
            }
        }

        private async Task InitializeSchema(CancellationToken stoppingToken)
        {
            const string createTableSql =
                """
                CREATE TABLE IF NOT EXISTS shortened_urls (
                    id SERIAL PRIMARY KEY,
                    short_code VARCHAR(10) UNIQUE NOT NULL,
                    original_url TEXT NOT NULL,
                    --created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                    created_at TIMESTAMPTZ DEFAULT now()
                );
                CREATE INDEX IF NOT EXISTS idx_shortened_urls_short_code ON shortened_urls(short_code);

                CREATE TABLE IF NOT EXISTS url_visits (
                    id SERIAL PRIMARY KEY,
                    short_code VARCHAR(10) NOT NULL,
                    --visited_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                    visited_at TIMESTAMPTZ DEFAULT now(),
                    user_agent TEXT,
                    referer TEXT,
                    FOREIGN KEY (short_code) REFERENCES shortened_urls(short_code)
                );
                CREATE INDEX IF NOT EXISTS idx_url_visits_short_code ON url_visits(short_code);
                """;

            await using var command = dataSource.CreateCommand(createTableSql);
            await command.ExecuteNonQueryAsync(stoppingToken);
        }
    }
}
                