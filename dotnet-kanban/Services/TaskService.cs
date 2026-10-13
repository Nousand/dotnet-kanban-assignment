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

        return await db.Tasks.Include(t => t.Tags).FirstOrDefaultAsync(t => t.Id == id);
    }


    public async Task AddTaskAsync(TaskItem task)
    {
        await using var db = await _factory.CreateDbContextAsync();
        
        task.Group = null;

        db.AttachRange(task.Tags);
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

    public async Task DeleteTaskAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        
        var task = await db.Tasks.FindAsync(id);

        if (task != null)
        {
            db.Tasks.Remove(task);
            await db.SaveChangesAsync();
        }
    }
}