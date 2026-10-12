using dotnet_kanban.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dotnet_kanban.Pages;

public class IndexModel : PageModel
{
    public Board? Board { get; set; }

    public void OnGet()
    {
        var json = System.IO.File.ReadAllText("Data/example-board.json");
        Board = BoardJson.Import(json);
    }
}