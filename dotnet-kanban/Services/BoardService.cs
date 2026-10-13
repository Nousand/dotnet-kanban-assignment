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
    public async Task<Board?> GetBoardsAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Boards.Include(b => b.Tags)
            .Include(b => b.Groups.OrderBy(g => g.Position)).ThenInclude(g => g.Tasks
            .OrderBy(t => t.Position)).ThenInclude(t => t.Tags).FirstOrDefaultAsync(b => b.Id == id);
    }

    // Creates new board using string name
    public async Task<Board> CreateBoardAsync(string name)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var board = new Board { Name = name };
        db.Boards.Add(board);
        await db.SaveChangesAsync();
        return board;
    }

    // Rename board
    public async Task RenameBoardAsync(Guid id, string name)
    {
        await using var db = await _factory.CreateDbContextAsync();
        
        var board = await db.Boards.FindAsync(id);
        
        if (board != null)
        {
            board.Name = name;
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