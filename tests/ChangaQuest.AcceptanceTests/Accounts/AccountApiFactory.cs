using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChangaQuest.AcceptanceTests.Accounts;

public sealed class AccountApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"ChangaQuestAcceptance_{Guid.NewGuid():N}";
    private readonly CapturingLoggerProvider _loggerProvider = new();
    private readonly string _environmentName;

    public IReadOnlyCollection<string> LogEntries => _loggerProvider.Entries;

    public AccountApiFactory() : this("Development")
    {
    }

    private AccountApiFactory(string environmentName)
    {
        _environmentName = environmentName;
    }

    public static AccountApiFactory ForEnvironment(string environmentName) =>
        new(environmentName);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environmentName);
        builder.UseSetting(
            "ConnectionStrings:ChangaQuest",
            $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddProvider(_loggerProvider);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(
                "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"IF DB_ID(N'{_databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]; END";
            command.ExecuteNonQuery();
            _loggerProvider.Dispose();
        }
    }
}

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _entries = new();

    public IReadOnlyCollection<string> Entries => _entries.ToArray();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(_entries);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(ConcurrentQueue<string> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                entries.Enqueue(formatter(state, exception));
            }
        }
    }
}
