using dotnet_kanban.Models;
using dotnet_kanban.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace dotnet_kanban.Tests;

[TestFixture]
public class TaskServiceTests
{
    private TestDbFactory _factory = null!;
    private TaskService _tasks = null!;
    private Guid _boardId;
    private Guid _todoId;
    private Guid _doneId;

    // Before every test: a fresh board with two groups, "To do" and "Done"
    [SetUp]
    public void SetUp()
    {
        _factory = new TestDbFactory();
        _tasks = new TaskService(_factory);

        using var db = _factory.CreateDbContext();
        var board = new Board { Name = "Test board" };
        var todo = new Group { Name = "To do", Position = 0 };
        var done = new Group { Name = "Done", Position = 1 };
        board.Groups.Add(todo);
        board.Groups.Add(done);
        db.Boards.Add(board);
        db.SaveChanges();

        _boardId = board.Id;
        _todoId = todo.Id;
        _doneId = done.Id;
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    // ---------- Helpers ----------

    private async Task<TaskItem> AddTaskAsync(Guid groupId, string title)
    {
        var task = new TaskItem { GroupId = groupId, Title = title };
        await _tasks.AddTaskAsync(task);
        return task;
    }

    private async Task<List<string>> TitlesAsync(Guid groupId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Tasks
            .Where(t => t.GroupId == groupId)
            .OrderBy(t => t.Position)
            .Select(t => t.Title)
            .ToListAsync();
    }

    private async Task<List<int>> PositionsAsync(Guid groupId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Tasks
            .Where(t => t.GroupId == groupId)
            .OrderBy(t => t.Position)
            .Select(t => t.Position)
            .ToListAsync();
    }

    // ---------- AddTaskAsync ----------

    [Test]
    public async Task AddTask_PutsEachTaskAtTheEndOfItsOwnGroup()
    {
        await AddTaskAsync(_todoId, "A");
        await AddTaskAsync(_todoId, "B");
        await AddTaskAsync(_doneId, "C");

        Assert.That(await TitlesAsync(_todoId), Is.EqualTo(new[] { "A", "B" }));
        Assert.That(await PositionsAsync(_todoId), Is.EqualTo(new[] { 0, 1 }));
        Assert.That(await PositionsAsync(_doneId), Is.EqualTo(new[] { 0 }));
    }

    [Test]
    public void AddTask_UnknownGroup_ThrowsKeyNotFound()
    {
        var task = new TaskItem { GroupId = Guid.NewGuid(), Title = "Orphan" };

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _tasks.AddTaskAsync(task));
    }

    // ---------- UpdateTaskAsync ----------

    [Test]
    public async Task UpdateTask_ChangesFieldsAndTags_AndKeepsPosition()
    {
        await AddTaskAsync(_todoId, "First");
        var added = await AddTaskAsync(_todoId, "Second");

        Tag bug;
        await using (var db = await _factory.CreateDbContextAsync())
        {
            bug = new Tag { BoardId = _boardId, Title = "Bug", Colour = "#ef4444" };
            db.Tags.Add(bug);
            await db.SaveChangesAsync();
        }

        var task = (await _tasks.GetTaskAsync(added.Id))!;
        task.Title = "Renamed";
        task.Priority = Priority.High;
        task.DueDate = new DateTime(2026, 10, 20, 9, 30, 0);
        task.Tags.Add(bug);
        await _tasks.UpdateTaskAsync(task);

        var reloaded = (await _tasks.GetTaskAsync(added.Id))!;
        Assert.That(reloaded.Title, Is.EqualTo("Renamed"));
        Assert.That(reloaded.Priority, Is.EqualTo(Priority.High));
        Assert.That(reloaded.DueDate, Is.EqualTo(new DateTime(2026, 10, 20, 9, 30, 0)));
        Assert.That(reloaded.Tags.Select(t => t.Title), Is.EqualTo(new[] { "Bug" }));
        Assert.That(reloaded.Position, Is.EqualTo(1));

        // Removing every tag in the edit form should remove them from the task
        reloaded.Tags.Clear();
        await _tasks.UpdateTaskAsync(reloaded);

        Assert.That((await _tasks.GetTaskAsync(added.Id))!.Tags, Is.Empty);
    }

    [Test]
    public void UpdateTask_UnknownTask_ThrowsKeyNotFound()
    {
        var task = new TaskItem { Id = Guid.NewGuid(), Title = "Ghost" };

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _tasks.UpdateTaskAsync(task));
    }

    // ---------- DeleteTaskAsync ----------

    [Test]
    public async Task DeleteTask_ClosesTheGapInPositions()
    {
        await AddTaskAsync(_todoId, "A");
        var b = await AddTaskAsync(_todoId, "B");
        await AddTaskAsync(_todoId, "C");

        await _tasks.DeleteTaskAsync(b.Id);

        Assert.That(await TitlesAsync(_todoId), Is.EqualTo(new[] { "A", "C" }));
        Assert.That(await PositionsAsync(_todoId), Is.EqualTo(new[] { 0, 1 }));
    }

    [Test]
    public void DeleteTask_UnknownTask_ThrowsKeyNotFound()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _tasks.DeleteTaskAsync(Guid.NewGuid()));
    }

    // ---------- MoveTaskAsync ----------

    [Test]
    public async Task MoveTask_WithinTheSameGroup_ReordersTasks()
    {
        var a = await AddTaskAsync(_todoId, "A");
        await AddTaskAsync(_todoId, "B");
        await AddTaskAsync(_todoId, "C");

        // Position is counted after the task has been taken out, so 2 means "at the end"
        var ok = await _tasks.MoveTaskAsync(_boardId, a.Id, _todoId, 2);

        Assert.That(ok, Is.True);
        Assert.That(await TitlesAsync(_todoId), Is.EqualTo(new[] { "B", "C", "A" }));
        Assert.That(await PositionsAsync(_todoId), Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public async Task MoveTask_ToAnotherGroup_RenumbersBothGroups()
    {
        await AddTaskAsync(_todoId, "A");
        var b = await AddTaskAsync(_todoId, "B");
        await AddTaskAsync(_todoId, "C");
        await AddTaskAsync(_doneId, "D");

        var ok = await _tasks.MoveTaskAsync(_boardId, b.Id, _doneId, 0);

        Assert.That(ok, Is.True);
        Assert.That(await TitlesAsync(_todoId), Is.EqualTo(new[] { "A", "C" }));
        Assert.That(await PositionsAsync(_todoId), Is.EqualTo(new[] { 0, 1 }));
        Assert.That(await TitlesAsync(_doneId), Is.EqualTo(new[] { "B", "D" }));
        Assert.That(await PositionsAsync(_doneId), Is.EqualTo(new[] { 0, 1 }));
    }

    [TestCase(-1)]
    [TestCase(5)]
    public async Task MoveTask_PositionOutOfRange_ReturnsFalseAndChangesNothing(int position)
    {
        var a = await AddTaskAsync(_todoId, "A");
        await AddTaskAsync(_todoId, "B");

        var ok = await _tasks.MoveTaskAsync(_boardId, a.Id, _doneId, position);

        Assert.That(ok, Is.False);
        Assert.That(await TitlesAsync(_todoId), Is.EqualTo(new[] { "A", "B" }));
        Assert.That(await TitlesAsync(_doneId), Is.Empty);
    }

    [Test]
    public async Task MoveTask_ToAGroupOnAnotherBoard_ReturnsFalseAndChangesNothing()
    {
        var a = await AddTaskAsync(_todoId, "A");

        Guid otherGroupId;
        await using (var db = await _factory.CreateDbContextAsync())
        {
            var otherBoard = new Board { Name = "Other board" };
            var otherGroup = new Group { Name = "Elsewhere" };
            otherBoard.Groups.Add(otherGroup);
            db.Boards.Add(otherBoard);
            await db.SaveChangesAsync();
            otherGroupId = otherGroup.Id;
        }

        var ok = await _tasks.MoveTaskAsync(_boardId, a.Id, otherGroupId, 0);

        Assert.That(ok, Is.False);
        Assert.That(await TitlesAsync(_todoId), Is.EqualTo(new[] { "A" }));
        Assert.That(await TitlesAsync(otherGroupId), Is.Empty);
    }

    [Test]
    public async Task MoveTask_UnknownTask_ReturnsFalse()
    {
        var ok = await _tasks.MoveTaskAsync(_boardId, Guid.NewGuid(), _doneId, 0);

        Assert.That(ok, Is.False);
    }
}
