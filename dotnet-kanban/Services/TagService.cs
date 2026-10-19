using dotnet_kanban.Data;
using dotnet_kanban.Models;
using Microsoft.EntityFrameworkCore;

namespace dotnet_kanban.Services;

public class TagService : ITagService
{
    private readonly IDbContextFactory<AppDBContext> _factory;
    public TagService(IDbContextFactory<AppDBContext> factory) => _factory = factory;

    // Get a list of all tags on the specific board
    public async Task<List<Tag>> GetTagsAsync(Guid boardId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Tags.Where(t => t.BoardId == boardId).ToListAsync();
    }

    // Create tag with title and colour
    public async Task AddTagAsync(Tag tag)
    {
        await using var db = await _factory.CreateDbContextAsync();

        if (!await db.Boards.AnyAsync(b => b.Id == tag.BoardId)) throw new KeyNotFoundException("Could nout find board.");

        db.Tags.Add(tag);
        await db.SaveChangesAsync();
    }

    // Update tag name and colour
    public async Task UpdateTagAsync(Tag tag)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var existing = await db.Tags.FindAsync(tag.Id);
        if (existing != null)
        {
            existing.Title = tag.Title;
            existing.Colour = tag.Colour;
            await db.SaveChangesAsync();
        } else throw new KeyNotFoundException("Could not find board.");
    }

    // Delete tag
    public async Task DeleteTagAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var tag = await db.Tags.FindAsync(id);
        if (tag != null)
        {
            db.Tags.Remove(tag);
            await db.SaveChangesAsync();
        } throw new KeyNotFoundException("Could not find board.");
    }
}