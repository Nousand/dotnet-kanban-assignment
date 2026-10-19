using dotnet_kanban.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace dotnet_kanban.Tests;

// A throwaway in-memory SQLite database for tests.
// The connection has to stay open: an in-memory database disappears when its last connection closes.
// Every test creates its own factory, so tests can never affect each other.
public sealed class TestDbFactory : IDbContextFactory<AppDBContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDBContext> _options;

    public TestDbFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDBContext>()
            .UseSqlite(_connection)
            .Options;

        // Build the tables from the model (the same tables the real app gets from migrations)
        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    public AppDBContext CreateDbContext() => new(_options);

    // The interface provides this as a default method, which can only be called through the interface.
    // Declaring it here lets the tests call it directly on TestDbFactory.
    public Task<AppDBContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());

    public void Dispose() => _connection.Dispose();
}
