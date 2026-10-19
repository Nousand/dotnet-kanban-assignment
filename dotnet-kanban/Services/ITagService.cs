using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dotnet_kanban.Models;

namespace dotnet_kanban.Services;

// Contract for tag storage.
public interface ITagService
{
    Task<List<Tag>> GetTagsAsync(Guid boardId);
    Task AddTagAsync(Tag tag);
    Task UpdateTagAsync(Tag tag);
    Task DeleteTagAsync(Guid id);
}
