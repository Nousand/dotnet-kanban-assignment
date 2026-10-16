using dotnet_kanban.Models;
using dotnet_kanban.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dotnet_kanban.Pages;

public class BoardModel : PageModel
{
    private readonly BoardService _boards;

    public BoardModel(BoardService boards) => _boards = boards;

    public Board? Board { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Board = await _boards.GetBoardByIdAsync(id);

        if (Board is null) return NotFound();

        return Page();
    }
}
