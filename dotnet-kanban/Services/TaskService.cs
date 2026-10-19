using dotnet_kanban.Data;
using dotnet_kanban.Models;
using Microsoft.EntityFrameworkCore;

namespace dotnet_kanban.Services;

public class TaskService
{
    private readonly IDbContextFactory<AppDBContext> _factory;
    public TaskService(IDbContextFactory<AppDBContext> factory) => _factory = factory;

    public async Task<TaskItem?> GetTaskAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Tasks.Include(t => t.Tags).Include(t => t.Group)
            .FirstOrDefaultAsync(t => t.Id == id);
    }


    public async Task AddTaskAsync(TaskItem task)
    {
        await using var db = await _factory.CreateDbContextAsync();

        if (!await db.Groups.AnyAsync(g => g.Id == task.GroupId))
            throw new KeyNotFoundException("Could not find group.");

        task.Position = await db.Tasks.CountAsync(t => t.GroupId == task.GroupId);
        task.Group = null;

        db.Tasks.Add(task);
        await db.SaveChangesAsync();
    }

    public async Task UpdateTaskAsync(TaskItem task)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.Tasks.Include(t => t.Tags).FirstOrDefaultAsync(t => t.Id == task.Id);

        if (existing != null)
        {
            var tagIds = task.Tags.Select(t => t.Id).ToList();
            var tags = await db.Tags.Where(t => tagIds.Contains(t.Id)).ToListAsync();

            existing.Title = task.Title;
            existing.Description = task.Description;
            existing.Priority = task.Priority;
            existing.DueDate = task.DueDate;
            existing.Position = task.Position;

            existing.Tags.Clear();
            existing.Tags.AddRange(tags);

            await db.SaveChangesAsync();
        } else throw new KeyNotFoundException("Could not find task");
    }

    public async Task<bool> MoveTaskAsync(Guid boardId, Guid taskId, Guid destinationGroupId, int destinationPosition)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var task = await db.Tasks.Include(item => item.Group)
            .FirstOrDefaultAsync(item => item.Id == taskId);
        var destinationGroup = await db.Groups
            .FirstOrDefaultAsync(group => group.Id == destinationGroupId && group.BoardId == boardId);
        if (task?.Group is null || task.Group.BoardId != boardId || destinationGroup is null)
            return false;

        var sourceGroupId = task.GroupId;
        var sourceTasks = await db.Tasks
            .Where(item => item.GroupId == sourceGroupId)
            .OrderBy(item => item.Position)
            .ToListAsync();
        sourceTasks.Remove(task);

        List<TaskItem> destinationTasks;
        if (sourceGroupId == destinationGroupId)
        {
            destinationTasks = sourceTasks;
        }
        else
        {
            destinationTasks = await db.Tasks
                .Where(item => item.GroupId == destinationGroupId)
                .OrderBy(item => item.Position)
                .ToListAsync();
        }

        if (destinationPosition < 0 || destinationPosition > destinationTasks.Count)
            return false;

        task.GroupId = destinationGroupId;
        task.Group = destinationGroup;
        destinationTasks.Insert(destinationPosition, task);

        for (var position = 0; position < sourceTasks.Count; position++)
        {
            sourceTasks[position].Position = position;
        }

        for (var position = 0; position < destinationTasks.Count; position++)
        {
            destinationTasks[position].Position = position;
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var task = await db.Tasks.FindAsync(id);
        if (task is null) throw new KeyNotFoundException("Could not find task.");

        var laterTasks = await db.Tasks
            .Where(t => t.GroupId == task.GroupId && t.Position > task.Position)
            .ToListAsync();
        foreach (var laterTask in laterTasks)
        {
            laterTask.Position--;
        }

        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
    }
}