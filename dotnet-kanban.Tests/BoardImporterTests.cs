using dotnet_kanban.Models;
using dotnet_kanban.Services;
using NUnit.Framework;

namespace dotnet_kanban.Tests;

// BoardImporter.Build is a pure function (objects in, objects out, no database),
// so these tests are fast and need no setup.
[TestFixture]
public class BoardImporterTests
{
    // A small valid board that each test can tweak before importing it
    private static ExportBoard ValidBoard()
    {
        var bugTagId = Guid.NewGuid();

        return new ExportBoard
        {
            Id = Guid.NewGuid(),
            Name = "Sprint board",
            Tags =
            {
                new ExportTag { Id = bugTagId, Title = "Bug", Colour = "#ef4444" }
            },
            Groups =
            {
                new ExportGroup
                {
                    Id = Guid.NewGuid(),
                    Position = 0,
                    Name = "To do",
                    Colour = "#3b82f6",
                    Tasks =
                    {
                        new ExportTask
                        {
                            Id = Guid.NewGuid(), Position = 0, Title = "Fix crash",
                            TagIds = { bugTagId }, Priority = Priority.High
                        },
                        new ExportTask { Id = Guid.NewGuid(), Position = 1, Title = "Write docs" }
                    }
                }
            }
        };
    }

    // Imports the source and checks it was rejected with a message containing the given text
    private static void AssertFails(ExportBoard? source, string expectedText)
    {
        var (board, error) = BoardImporter.Build(source);

        Assert.That(board, Is.Null);
        Assert.That(error, Does.Contain(expectedText));
    }

    // ---------- Valid boards ----------

    [Test]
    public void Build_ValidBoard_KeepsNamesAndStructure()
    {
        var (board, error) = BoardImporter.Build(ValidBoard());

        Assert.That(error, Is.Null);
        Assert.That(board, Is.Not.Null);
        Assert.That(board!.Name, Is.EqualTo("Sprint board"));
        Assert.That(board.Tags.Select(t => t.Title), Is.EqualTo(new[] { "Bug" }));
        Assert.That(board.Groups.Select(g => g.Name), Is.EqualTo(new[] { "To do" }));
        Assert.That(board.Groups[0].Tasks.Select(t => t.Title), Is.EqualTo(new[] { "Fix crash", "Write docs" }));
        Assert.That(board.Groups[0].Tasks[0].Priority, Is.EqualTo(Priority.High));
    }

    [Test]
    public void Build_BoardNameWithSpaces_IsTrimmed()
    {
        var source = ValidBoard();
        source.Name = "  Spaced out  ";

        var (board, _) = BoardImporter.Build(source);

        Assert.That(board!.Name, Is.EqualTo("Spaced out"));
    }

    [Test]
    public void Build_BoardWithNoGroupsOrTags_IsValid()
    {
        var (board, error) = BoardImporter.Build(new ExportBoard { Name = "Empty board" });

        Assert.That(error, Is.Null);
        Assert.That(board!.Groups, Is.Empty);
        Assert.That(board.Tags, Is.Empty);
    }

    [Test]
    public void Build_GivesEverythingFreshIds()
    {
        var source = ValidBoard();

        var (board, _) = BoardImporter.Build(source);

        Assert.That(board!.Id, Is.Not.EqualTo(source.Id));
        Assert.That(board.Groups[0].Id, Is.Not.EqualTo(source.Groups[0].Id));
        Assert.That(board.Groups[0].Tasks[0].Id, Is.Not.EqualTo(source.Groups[0].Tasks[0].Id));
        Assert.That(board.Tags[0].Id, Is.Not.EqualTo(source.Tags[0].Id));
    }

    [Test]
    public void Build_RemapsTaskTagsToTheNewTags()
    {
        var source = ValidBoard();

        var (board, _) = BoardImporter.Build(source);

        var task = board!.Groups[0].Tasks[0];
        Assert.That(task.Tags, Has.Count.EqualTo(1));
        Assert.That(task.Tags[0], Is.SameAs(board.Tags[0]));
        Assert.That(task.Tags[0].Id, Is.Not.EqualTo(source.Tags[0].Id));
    }

    [Test]
    public void Build_TagListedTwiceOnATask_IsAddedOnce()
    {
        var source = ValidBoard();
        var tagId = source.Tags[0].Id;
        source.Groups[0].Tasks[0].TagIds.Add(tagId);

        var (board, _) = BoardImporter.Build(source);

        Assert.That(board!.Groups[0].Tasks[0].Tags, Has.Count.EqualTo(1));
    }

    [Test]
    public void Build_SortsGroupsByPositionAndRenumbersFromZero()
    {
        var source = ValidBoard();
        source.Groups[0].Position = 9; // "To do" is last in the file's own numbering
        source.Groups.Add(new ExportGroup { Name = "Done", Colour = "#22c55e", Position = 2 });

        var (board, _) = BoardImporter.Build(source);

        Assert.That(board!.Groups.Select(g => g.Name), Is.EqualTo(new[] { "Done", "To do" }));
        Assert.That(board.Groups.Select(g => g.Position), Is.EqualTo(new[] { 0, 1 }));
    }

    [Test]
    public void Build_SortsTasksByPositionAndRenumbersFromZero()
    {
        var source = ValidBoard();
        source.Groups[0].Tasks[0].Position = 5;
        source.Groups[0].Tasks[1].Position = 1;

        var (board, _) = BoardImporter.Build(source);

        var tasks = board!.Groups[0].Tasks;
        Assert.That(tasks.Select(t => t.Title), Is.EqualTo(new[] { "Write docs", "Fix crash" }));
        Assert.That(tasks.Select(t => t.Position), Is.EqualTo(new[] { 0, 1 }));
    }

    // ---------- Invalid boards ----------

    [Test]
    public void Build_NullBoard_ReturnsError()
    {
        AssertFails(null, "empty");
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Build_BlankBoardName_ReturnsError(string name)
    {
        var source = ValidBoard();
        source.Name = name;

        AssertFails(source, "board name");
    }

    [Test]
    public void Build_BoardNameOver100Characters_ReturnsError()
    {
        var source = ValidBoard();
        source.Name = new string('x', 101);

        AssertFails(source, "board name");
    }

    [TestCase("red")]
    [TestCase("#12345")]
    [TestCase("#1234567")]
    [TestCase("#GGGGGG")]
    [TestCase("")]
    [TestCase(null)]
    public void Build_InvalidGroupColour_ReturnsError(string? colour)
    {
        var source = ValidBoard();
        source.Groups[0].Colour = colour!;

        AssertFails(source, "colour");
    }

    [TestCase("blue")]
    [TestCase("#abc")]
    [TestCase(null)]
    public void Build_InvalidTagColour_ReturnsError(string? colour)
    {
        var source = ValidBoard();
        source.Tags[0].Colour = colour!;

        AssertFails(source, "colour");
    }

    [Test]
    public void Build_BlankGroupName_ReturnsError()
    {
        var source = ValidBoard();
        source.Groups[0].Name = "  ";

        AssertFails(source, "group needs a name");
    }

    [Test]
    public void Build_BlankTaskTitle_ReturnsError()
    {
        var source = ValidBoard();
        source.Groups[0].Tasks[0].Title = "";

        AssertFails(source, "task needs a title");
    }

    [Test]
    public void Build_TaskTitleOver200Characters_ReturnsError()
    {
        var source = ValidBoard();
        source.Groups[0].Tasks[0].Title = new string('x', 201);

        AssertFails(source, "task needs a title");
    }

    [Test]
    public void Build_TaskUsesATagThatIsNotInTheFile_ReturnsError()
    {
        var source = ValidBoard();
        source.Groups[0].Tasks[0].TagIds.Add(Guid.NewGuid());

        AssertFails(source, "not listed");
    }

    [Test]
    public void Build_TwoTagsWithTheSameId_ReturnsError()
    {
        var source = ValidBoard();
        source.Tags.Add(new ExportTag { Id = source.Tags[0].Id, Title = "Clone", Colour = "#112233" });

        AssertFails(source, "used twice");
    }

    [Test]
    public void Build_UnknownPriorityNumber_ReturnsError()
    {
        var source = ValidBoard();
        source.Groups[0].Tasks[0].Priority = (Priority)99;

        AssertFails(source, "unknown priority");
    }

    [Test]
    public void Build_TooManyGroups_ReturnsError()
    {
        var source = ValidBoard();
        for (var i = 0; i < 101; i++)
        {
            source.Groups.Add(new ExportGroup { Name = $"Group {i}", Colour = "#112233" });
        }

        AssertFails(source, "Too many groups");
    }
}
