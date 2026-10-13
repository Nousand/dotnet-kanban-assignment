using dotnet_kanban.Data;
using dotnet_kanban.Models;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace dotnet_kanban.Services;

public class GroupService
{
    private readonly IDbContextFactory<AppDBContext> _factory;
    public GroupService(IDbContextFactory<AppDBContext> factory) => _factory = factory;

    // Create group using name and colour
    public async Task<Group> CreateGroupAsync(Guid boardId, string name, string colour)
    {
        await using var db = await _factory.CreateDbContextAsync();

        if (!await db.Boards.AnyAsync(b => b.Id == boardId)) throw new KeyNotFoundException("Could not find Board.");

        var lastGroup = await db.Groups.Where(g => g.BoardId == boardId).MaxAsync(g => (int?)g.Position) ?? -1;

        var group = new Group { BoardId = boardId, Name = name, Colour = colour, Position = lastGroup + 1 };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }

    // Update group
    public async Task UpdateGroupAsync(Guid id, string name, string colour)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var group = await db.Groups.FindAsync(id);
        
        if (group != null)
        {
            group.Name = name;
            group.Colour = colour;
            await db.SaveChangesAsync();
        } else throw new KeyNotFoundException("Could not find group.");
    }

    // Change group position
    public async Task UpdateGroupPositionAsync(Guid id, int pos)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var group = await db.Groups.FindAsync(id);
        
        if (group != null)
        {
            group.Position = pos;
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