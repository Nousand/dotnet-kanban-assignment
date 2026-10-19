using dotnet_kanban.Models;
using dotnet_kanban.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace dotnet_kanban.Tests;

// These tests run the real GroupService against a real (in-memory) SQLite database.
[TestFixture]
public class GroupServiceTests
{
    private TestDbFactory _factory = null!;
    private GroupService _groups = null!;
    private Guid _boardId;

    // SetUp runs before every test, so each test starts with a fresh empty board
    [SetUp]
    public void SetUp()
    {
        _factory = new TestDbFactory();
        _groups = new GroupService(_factory);

        using var db = _factory.CreateDbContext();
        var board = new Board { Name = "Test board" };
        db.Boards.Add(board);
        db.SaveChanges();
        _boardId = board.Id;
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    // ---------- Helpers ----------

    private async Task<Group> AddGroupAsync(string name)
    {
        var group = new Group { BoardId = _boardId, Name = name };
        await _groups.AddGroupAsync(group);
        return group;
    }

    private async Task<List<string>> NamesInOrderAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Groups
            .Where(g => g.BoardId == _boardId)
            .OrderBy(g => g.Position)
            .Select(g => g.Name)
            .ToListAsync();
    }

    private async Task<List<int>> PositionsInOrderAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Groups
            .Where(g => g.BoardId == _boardId)
            .OrderBy(g => g.Position)
            .Select(g => g.Position)
            .ToListAsync();
    }

    // ---------- AddGroupAsync ----------

    [Test]
    public async Task AddGroup_PutsEachNewGroupAtTheEnd()
    {
        await AddGroupAsync("A");
        await AddGroupAsync("B");
        await AddGroupAsync("C");

        Assert.That(await NamesInOrderAsync(), Is.EqualTo(new[] { "A", "B", "C" }));
        Assert.That(await PositionsInOrderAsync(), Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public void AddGroup_UnknownBoard_ThrowsKeyNotFound()
    {
        var group = new Group { BoardId = Guid.NewGuid(), Name = "Orphan" };

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _groups.AddGroupAsync(group));
    }

    // ---------- UpdateGroupAsync ----------

    [Test]
    public async Task UpdateGroup_ChangesNameAndColour()
    {
        var added = await AddGroupAsync("Old name");

        var group = (await _groups.GetGroupAsync(added.Id))!;
        group.Name = "New name";
        group.Colour = "#22c55e";
        await _groups.UpdateGroupAsync(group);

        var reloaded = (await _groups.GetGroupAsync(added.Id))!;
        Assert.That(reloaded.Name, Is.EqualTo("New name"));
        Assert.That(reloaded.Colour, Is.EqualTo("#22c55e"));
    }

    [Test]
    public void UpdateGroup_UnknownGroup_ThrowsKeyNotFound()
    {
        var group = new Group { Id = Guid.NewGuid(), Name = "Ghost" };

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _groups.UpdateGroupAsync(group));
    }

    // ---------- DeleteGroupAsync ----------

    [Test]
    public async Task DeleteGroup_ClosesTheGapInPositions()
    {
        await AddGroupAsync("A");
        var b = await AddGroupAsync("B");
        await AddGroupAsync("C");

        await _groups.DeleteGroupAsync(b.Id);

        Assert.That(await NamesInOrderAsync(), Is.EqualTo(new[] { "A", "C" }));
        Assert.That(await PositionsInOrderAsync(), Is.EqualTo(new[] { 0, 1 }));
    }

    [Test]
    public void DeleteGroup_UnknownGroup_ThrowsKeyNotFound()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _groups.DeleteGroupAsync(Guid.NewGuid()));
    }

    // ---------- ReorderGroupsAsync ----------

    [Test]
    public async Task ReorderGroups_ValidList_UpdatesPositions()
    {
        var a = await AddGroupAsync("A");
        var b = await AddGroupAsync("B");
        var c = await AddGroupAsync("C");

        var ok = await _groups.ReorderGroupsAsync(_boardId, new[] { c.Id, a.Id, b.Id });

        Assert.That(ok, Is.True);
        Assert.That(await NamesInOrderAsync(), Is.EqualTo(new[] { "C", "A", "B" }));
        Assert.That(await PositionsInOrderAsync(), Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public async Task ReorderGroups_ListMissingAGroup_ReturnsFalseAndChangesNothing()
    {
        var a = await AddGroupAsync("A");
        var b = await AddGroupAsync("B");
        await AddGroupAsync("C");

        var ok = await _groups.ReorderGroupsAsync(_boardId, new[] { b.Id, a.Id });

        Assert.That(ok, Is.False);
        Assert.That(await NamesInOrderAsync(), Is.EqualTo(new[] { "A", "B", "C" }));
    }

    [Test]
    public async Task ReorderGroups_RepeatedId_ReturnsFalseAndChangesNothing()
    {
        var a = await AddGroupAsync("A");
        var b = await AddGroupAsync("B");
        await AddGroupAsync("C");

        var ok = await _groups.ReorderGroupsAsync(_boardId, new[] { a.Id, a.Id, b.Id });

        Assert.That(ok, Is.False);
        Assert.That(await NamesInOrderAsync(), Is.EqualTo(new[] { "A", "B", "C" }));
    }

    // ---------- GetGroupAsync ----------

    [Test]
    public async Task GetGroup_LoadsItsTasksInPositionOrder()
    {
        var group = await AddGroupAsync("To do");

        // Save the tasks in the "wrong" order to prove the service sorts them
        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.Tasks.Add(new TaskItem { GroupId = group.Id, Title = "Second", Position = 1 });
            db.Tasks.Add(new TaskItem { GroupId = group.Id, Title = "First", Position = 0 });
            await db.SaveChangesAsync();
        }

        var loaded = await _groups.GetGroupAsync(group.Id);

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Tasks.Select(t => t.Title), Is.EqualTo(new[] { "First", "Second" }));
    }

    [Test]
    public async Task GetGroup_UnknownGroup_ReturnsNull()
    {
        Assert.That(await _groups.GetGroupAsync(Guid.NewGuid()), Is.Null);
    }
}
