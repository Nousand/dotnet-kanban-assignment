using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dotnet_kanban.Models;

namespace dotnet_kanban.Services;

// Contract for task storage, including moving a task between groups.
public interface ITaskService
{
    Task<TaskItem?> GetTaskAsync(Guid id);
    Task AddTaskAsync(TaskItem task);
    Task UpdateTaskAsync(TaskItem task);
    Task<bool> MoveTaskAsync(Guid boardId, Guid taskId, Guid destinationGroupId, int destinationPosition);
    Task DeleteTaskAsync(Guid id);
}
