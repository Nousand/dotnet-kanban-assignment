using dotnet_kanban.Models;
using dotnet_kanban.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dotnet_kanban.Pages;

public class IndexModel : PageModel
{
    private readonly BoardService _boards;

    public IndexModel(BoardService boards) => _boards = boards;

    [BindProperty]
    public string BoardName { get; set; } = "";

    // Survives the redirect after a create so the message shows once.
    [TempData]
    public string? Message { get; set; }

    // Only filled when the Print button is pressed.
    public List<Board>? Boards { get; set; }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(BoardName))
        {
            Message = "Enter a board name first.";
            return RedirectToPage();
        }


        var board = new Board { Name = BoardName.Trim() };
        await _boards.AddBoardAsync(board);
        Message = $"Created board: {board.Name} ({board.Id})";
        return RedirectToPage();
    }

    public async Task OnPostPrintAsync()
    {
        Boards = await _boards.GetBoardsAsync();
    }
}