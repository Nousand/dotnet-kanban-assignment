using dotnet_kanban.Data;
using dotnet_kanban.Models;
using Microsoft.EntityFrameworkCore;

namespace dotnet_kanban.Services;

public class BoardService
{
    private readonly IDbContextFactory<AppDBContext> _factory;

    public BoardService(IDbContextFactory<AppDBContext> factory) => _factory = factory;

    // Gets all boards without the related groups, tasks and tags
    public async Task<List<Board>> GetBoardsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Boards.ToListAsync();
    }

    // Gets full board with groups tasks and tags
    public async Task<Board?> GetBoardByIdAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Boards.Include(b => b.Tags)
            .Include(b => b.Groups.OrderBy(g => g.Position)).ThenInclude(g => g.Tasks
            .OrderBy(t => t.Position)).ThenInclude(t => t.Tags).FirstOrDefaultAsync(b => b.Id == id);
    }

    // Add new board
    public async Task AddBoardAsync(Board board)
    {
        await using var db = await _factory.CreateDbContextAsync();

        db.Boards.Add(board);
        await db.SaveChangesAsync();
    }

    // Rename board
    public async Task UpdateBoardAsync(Board board)
    {
        await using var db = await _factory.CreateDbContextAsync();
        
        var existing = await db.Boards.FindAsync(board.Id);
        
        if (existing != null)
        {
            existing.Name = board.Name;
            await db.SaveChangesAsync();
        } else throw new KeyNotFoundException("Could not find board.");
    }

    // Delete board
    public async Task DeleteBoardAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        
        var board = await db.Boards.FindAsync(id);
        
        if (board != null)
        {
            db.Boards.Remove(board);
            await db.SaveChangesAsync();
        } else throw new KeyNotFoundException("Could not find board.");
    }
}