using System.Text.Json;
using dotnet_kanban.Models;
using NUnit.Framework;

namespace dotnet_kanban.Tests;

[TestFixture]
public class BoardJsonTests
{
    private static readonly DateTime DueDate = new(2026, 10, 20, 9, 30, 0);

    // A board with one tag, one group and one task that uses the tag
    private static Board SampleBoard()
    {
        var tag = new Tag { Title = "Bug", Colour = "#ef4444" };
        var task = new TaskItem
        {
            Title = "Fix crash",
            Description = "Happens on export",
            Priority = Priority.High,
            DueDate = DueDate,
            Tags = { tag }
        };
        var group = new Group { Name = "To do", Colour = "#3b82f6", Tasks = { task } };

        return new Board { Name = "Sprint board", Tags = { tag }, Groups = { group } };
    }

    [Test]
    public void ExportThenImport_KeepsTheContent()
    {
        var board = SampleBoard();

        var imported = BoardJson.Import(BoardJson.Export(board));

        Assert.That(imported, Is.Not.Null);
        Assert.That(imported!.Name, Is.EqualTo("Sprint board"));
        Assert.That(imported.Tags.Select(t => t.Title), Is.EqualTo(new[] { "Bug" }));
        Assert.That(imported.Groups.Select(g => g.Name), Is.EqualTo(new[] { "To do" }));

        var task = imported.Groups[0].Tasks[0];
        Assert.That(task.Title, Is.EqualTo("Fix crash"));
        Assert.That(task.Description, Is.EqualTo("Happens on export"));
        Assert.That(task.Priority, Is.EqualTo(Priority.High));
        Assert.That(task.DueDate, Is.EqualTo(DueDate));
        Assert.That(task.TagIds, Is.EqualTo(new[] { board.Tags[0].Id }));
    }

    [Test]
    public void Export_WritesPriorityAsText()
    {
        var json = BoardJson.Export(SampleBoard());

        Assert.That(json, Does.Contain("\"priority\": \"High\""));
    }

    [Test]
    public void Export_ListsGroupsInPositionOrder()
    {
        var board = new Board { Name = "Order test" };
        board.Groups.Add(new Group { Name = "Second", Position = 1 });
        board.Groups.Add(new Group { Name = "First", Position = 0 });

        var imported = BoardJson.Import(BoardJson.Export(board));

        Assert.That(imported!.Groups.Select(g => g.Name), Is.EqualTo(new[] { "First", "Second" }));
    }

    [Test]
    public void Import_TextThatIsNotJson_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => BoardJson.Import("this is not json"));
    }

    [Test]
    public void Import_UnknownPriorityName_ThrowsJsonException()
    {
        const string json = """
            { "name": "x", "groups": [ { "name": "g", "colour": "#111111",
              "tasks": [ { "title": "t", "priority": "Urgent" } ] } ] }
            """;

        Assert.Throws<JsonException>(() => BoardJson.Import(json));
    }
}
