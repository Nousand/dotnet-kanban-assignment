using dotnet_kanban.Models;
using dotnet_kanban.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

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

    public async Task<IActionResult> OnPostImportAsync(IFormFile? file)
    {
        var (board, error) = await ReadBoardAsync(file);
        if (board is null)
        {
            Response.Headers["HX-Retarget"] = "#import-error";
            Response.Headers["HX-Reswap"] = "innerHTML";
            return Content(System.Net.WebUtility.HtmlEncode(error ?? "Import failed."), "text/html");
        }

        await _boards.AddBoardAsync(board);
        return Partial("_BoardCard", board);
    }

    private static async Task<(Board? Board, string? Error)> ReadBoardAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0) return (null, "Choose a .json file to import.");
        if (file.Length > BoardImporter.MaxFileBytes) return (null, "That file is too large (limit 1 MB).");

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var json = await reader.ReadToEndAsync();
            return BoardImporter.Build(BoardJson.Import(json));
        }
        catch (JsonException)
        {
            return (null, "That file isn't valid board JSON.");
        }
    }
}
