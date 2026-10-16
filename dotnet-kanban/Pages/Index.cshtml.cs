using dotnet_kanban.Models;
using dotnet_kanban.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dotnet_kanban.Pages;

public class IndexModel : PageModel
{
    private readonly BoardService _boards;

    public IndexModel(BoardService boards) => _boards = boards;

    public List<Board> Boards { get; set; } = new();

    public async Task OnGetAsync() => Boards = await _boards.GetBoardsAsync();

    public async Task<IActionResult> OnPostCreateAsync(string? name)
    {
        var boardName = (name ?? "").Trim();
        if (boardName.Length == 0) return BadRequest();

        var board = new Board { Name = boardName };
        await _boards.AddBoardAsync(board);

        return Partial("_BoardCard", board);
    }

    public async Task<IActionResult> OnPostRenameAsync(Guid id, string? name)
    {
        var boardName = (name ?? "").Trim();
        if (boardName.Length == 0) return BadRequest();

        var board = new Board { Id = id, Name = boardName };
        await _boards.UpdateBoardAsync(board);

        return Partial("_BoardCard", board);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _boards.DeleteBoardAsync(id);

        return new EmptyResult();
    }
}
