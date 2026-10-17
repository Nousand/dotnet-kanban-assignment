using dotnet_kanban.Models;
using dotnet_kanban.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dotnet_kanban.Pages;

public class BoardModel : PageModel
{
    private readonly BoardService _boards;
    private readonly GroupService _groups;

    public BoardModel(BoardService boards, GroupService groups)
    {
        _boards = boards;
        _groups = groups;
    }

    public Board? Board { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Board = await _boards.GetBoardByIdAsync(id);
        if (Board is null) return NotFound();
        return Page();
    }

    // "id" is the board id from the URL
    public async Task<IActionResult> OnPostCreateGroupAsync(Guid id, string? name, string? colour)
    {
        var groupName = (name ?? "").Trim();
        if (!IsValid(groupName, colour)) return BadRequest();

        var group = new Group { BoardId = id, Name = groupName, Colour = colour! };
        await _groups.AddGroupAsync(group);

        return Partial("_Group", group);
    }

    public async Task<IActionResult> OnPostEditGroupAsync(Guid groupId, string? name, string? colour)
    {
        var groupName = (name ?? "").Trim();
        if (!IsValid(groupName, colour)) return BadRequest();

        var group = await _groups.GetGroupAsync(groupId);
        if (group is null) return NotFound();

        group.Name = groupName;
        group.Colour = colour!;
        await _groups.UpdateGroupAsync(group);

        return Partial("_Group", group);
    }

    public async Task<IActionResult> OnPostDeleteGroupAsync(Guid groupId)
    {
        await _groups.DeleteGroupAsync(groupId);
        return new EmptyResult();
    }

    // Name must be 1-100 chars, colour must look like #rrggbb.
    // The colour goes into a style attribute
    private static bool IsValid(string name, string? colour) =>
        name.Length is > 0 and <= 100 &&
        colour is not null &&
        System.Text.RegularExpressions.Regex.IsMatch(colour, "^#[0-9a-fA-F]{6}$");
}