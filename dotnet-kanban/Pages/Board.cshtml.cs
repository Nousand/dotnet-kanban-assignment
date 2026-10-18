using dotnet_kanban.Models;
using dotnet_kanban.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dotnet_kanban.Pages;

public class BoardModel : PageModel
{
    private readonly BoardService _boards;
    private readonly GroupService _groups;
    private readonly TaskService _tasks;
    private readonly TagService _tags;

    public BoardModel(BoardService boards, GroupService groups, TaskService tasks, TagService tags)
    {
        _boards = boards;
        _groups = groups;
        _tasks = tasks;
        _tags = tags;
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

    public async Task<IActionResult> OnPostCreateTaskAsync(Guid id, Guid groupId)
    {
        var group = await _groups.GetGroupAsync(groupId);
        if (group is null || group.BoardId != id) return NotFound();

        var task = new TaskItem
        {
            GroupId = group.Id,
            Title = "Untitled",
            Position = group.Tasks.Count
        };
        await _tasks.AddTaskAsync(task);

        group = await _groups.GetGroupAsync(groupId);
        task = await _tasks.GetTaskAsync(task.Id);
        if (task is null || group is null) return NotFound();

        task.Group = group;
        ViewData["TaskCount"] = group.TaskCount;
        Response.Headers["HX-Trigger"] = "openTaskView";
        return Partial("_TaskCreated", task);
    }

    public async Task<IActionResult> OnGetViewTaskAsync(Guid id, Guid taskId)
    {
        var task = await GetTaskOnBoardAsync(id, taskId);
        if (task is null) return NotFound();

        Response.Headers["HX-Trigger"] = "openTaskView";
        return Partial("_TaskDetails", task);
    }

    public async Task<IActionResult> OnGetEditTaskAsync(Guid id, Guid taskId)
    {
        var task = await GetTaskOnBoardAsync(id, taskId);
        if (task is null) return NotFound();

        var boardTags = await _tags.GetTagsAsync(task.Group!.BoardId);
        return Partial("_TaskEdit", new TaskEditViewModel(task, boardTags));
    }

    public async Task<IActionResult> OnPostAddTaskTagAsync(
        Guid id,
        Guid taskId,
        string? title,
        string? colour)
    {
        var task = await GetTaskOnBoardAsync(id, taskId);
        if (task is null) return NotFound();

        var tagTitle = (title ?? "").Trim();
        if (tagTitle.Length is 0 or > 50 || !IsValidColour(colour))
            return BadRequest();

        var tag = new Tag
        {
            BoardId = task.Group!.BoardId,
            Title = tagTitle,
            Colour = colour!
        };
        await _tags.AddTagAsync(tag);
        return Partial("_TaskTagAdded", new TaskTagAddedViewModel(tag, taskId));
    }

    public async Task<IActionResult> OnPostUpdateTaskAsync(
        Guid id,
        Guid taskId,
        string? title,
        string? description,
        int priority,
        DateTime? dueDate,
        List<Guid>? tagIds)
    {
        var task = await GetTaskOnBoardAsync(id, taskId);
        if (task is null) return NotFound();

        var taskTitle = (title ?? "").Trim();
        if (taskTitle.Length is 0 or > 200 || !Enum.IsDefined((Priority)priority))
            return BadRequest();

        var requestedTagIds = tagIds?.Distinct().ToList() ?? [];
        var boardTags = await _tags.GetTagsAsync(task.Group!.BoardId);
        var selectedTags = boardTags.Where(tag => requestedTagIds.Contains(tag.Id)).ToList();
        if (selectedTags.Count != requestedTagIds.Count) return BadRequest();

        task.Title = taskTitle;
        task.Description = description ?? "";
        task.Priority = (Priority)priority;
        task.DueDate = dueDate;
        task.Tags = selectedTags;
        await _tasks.UpdateTaskAsync(task);

        task = await _tasks.GetTaskAsync(taskId);
        if (task is null) return NotFound();

        task.Group = await _groups.GetGroupAsync(task.GroupId);
        return Partial("_TaskUpdated", task);
    }

    public async Task<IActionResult> OnPostDeleteTaskAsync(Guid id, Guid taskId)
    {
        var task = await GetTaskOnBoardAsync(id, taskId);
        if (task is null) return NotFound();

        var groupId = task.GroupId;
        await _tasks.DeleteTaskAsync(taskId);

        var group = await _groups.GetGroupAsync(groupId);
        if (group is null) return NotFound();

        Response.Headers["HX-Trigger"] = "closeTaskView";
        return Partial("_TaskDeleted", group);
    }

    private async Task<TaskItem?> GetTaskOnBoardAsync(Guid boardId, Guid taskId)
    {
        var task = await _tasks.GetTaskAsync(taskId);
        return task?.Group?.BoardId == boardId ? task : null;
    }

    // Name must be 1-100 chars, colour must look like #rrggbb.
    // The colour goes into a style attribute
    private static bool IsValid(string name, string? colour) =>
        name.Length is > 0 and <= 100 &&
        IsValidColour(colour);

    private static bool IsValidColour(string? colour) =>
        colour is not null &&
        System.Text.RegularExpressions.Regex.IsMatch(colour, "^#[0-9a-fA-F]{6}$");
}