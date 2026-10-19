using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dotnet_kanban.Models;

namespace dotnet_kanban.Services;

// Contract for group (column) storage.
public interface IGroupService
{
    Task AddGroupAsync(Group group);
    Task UpdateGroupAsync(Group group);
    Task<bool> ReorderGroupsAsync(Guid boardId, IReadOnlyList<Guid> groupIds);
    Task DeleteGroupAsync(Guid id);
    Task<Group?> GetGroupAsync(Guid id);
}
