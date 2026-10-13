using dotnet_kanban.Data;
using dotnet_kanban.Models;
using Microsoft.EntityFrameworkCore;

namespace dotnet_kanban.Services;

public class GroupService
{
    private readonly IDbContextFactory<AppDBContext> _factory;
    public GroupService(IDbContextFactory<AppDBContext> factory) => _factory = factory;

    // Create group using name and colour
    public async Task AddGroupAsync(Group group)
    {
        await using var db = await _factory.CreateDbContextAsync();

        if (!await db.Boards.AnyAsync(b => b.Id == group.BoardId)) throw new KeyNotFoundException("Could not find Board.");

        db.Groups.Add(group);
        await db.SaveChangesAsync();
    }

    // Update group
    public async Task UpdateGroupAsync(Group group)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var existing = await db.Groups.FindAsync(group.Id);
        
        if (existing != null)
        {
            existing.Name = group.Name;
            existing.Colour = group.Colour;
            existing.Position = group.Position;
            await db.SaveChangesAsync();
        } else throw new KeyNotFoundException("Could not find group.");
    }

    // Delete Group
    public async Task DeleteGroupAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var group = await db.Groups.FindAsync(id);
        if (group == null) throw new KeyNotFoundException("Could not find group.");

        db.Groups.Remove(group);
        await db.SaveChangesAsync();
    }
}