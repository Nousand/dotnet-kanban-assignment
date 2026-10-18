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
        group.Position = await db.Groups.CountAsync(g => g.BoardId == group.BoardId);

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

    public async Task<bool> ReorderGroupsAsync(Guid boardId, IReadOnlyList<Guid> groupIds)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var groups = await db.Groups.Where(group => group.BoardId == boardId).ToListAsync();
        var requestedIds = groupIds.ToHashSet();
        if (requestedIds.Count != groupIds.Count || requestedIds.Count != groups.Count ||
            groups.Any(group => !requestedIds.Contains(group.Id)))
        {
            return false;
        }

        var groupsById = groups.ToDictionary(group => group.Id);
        for (var position = 0; position < groupIds.Count; position++)
        {
            groupsById[groupIds[position]].Position = position;
        }

        await db.SaveChangesAsync();
        return true;
    }

    // Delete Group
    public async Task DeleteGroupAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var group = await db.Groups.FindAsync(id);
        if (group == null) throw new KeyNotFoundException("Could not find group.");

        var later = await db.Groups.Where(g => g.BoardId == group.BoardId && g.Position > group.Position).ToListAsync();

        foreach (var g in later)
        {
            g.Position--;
        }

        db.Groups.Remove(group);
        await db.SaveChangesAsync();
    }


    //Load one group by id
    public async Task<Group?> GetGroupAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Groups
            .Include(g => g.Tasks.OrderBy(t => t.Position)).ThenInclude(t => t.Tags)
            .FirstOrDefaultAsync(g => g.Id == id);
    }
}