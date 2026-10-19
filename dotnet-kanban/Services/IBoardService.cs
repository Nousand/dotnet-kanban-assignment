using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dotnet_kanban.Models;

namespace dotnet_kanban.Services;

// Contract for board storage. Page models depend on this interface rather than
// the concrete BoardService, so the implementation can be swapped (e.g. in tests).
public interface IBoardService
{
    Task<List<Board>> GetBoardsAsync();
    Task<List<BoardTaskCount>> GetTaskCountsAsync();
    Task<Board?> GetBoardByIdAsync(Guid id);
    Task AddBoardAsync(Board board);
    Task UpdateBoardAsync(Board board);
    Task DeleteBoardAsync(Guid id);
}
