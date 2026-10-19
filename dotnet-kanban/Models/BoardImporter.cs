using System.Text.RegularExpressions;
using dotnet_kanban.Models;

namespace dotnet_kanban.Models ;

// Turns an uploaded (exported) board into a brand-new Board, or explains what is wrong with it.
// It is a pure function with no database access, so it is easy to unit test.
public static class BoardImporter
{
    public const long MaxFileBytes = 1_000_000;

    private const int MaxGroups = 100;
    private const int MaxTags = 200;
    private const int MaxTasks = 5000;
    private const int MaxDescriptionLength = 10_000;

    private static readonly Regex HexColour = new("^#[0-9a-fA-F]{6}$");

    public static (Board? Board, string? Error) Build(ExportBoard? source)
    {
        if (source is null) return Fail("The file is empty.");

        var name = (source.Name ?? "").Trim();
        if (name.Length is 0 or > 100) return Fail("The board name must be 1-100 characters.");

        var tagsIn = source.Tags ?? [];
        var groupsIn = source.Groups ?? [];

        if (tagsIn.Count > MaxTags) return Fail($"Too many tags (limit {MaxTags}).");
        if (groupsIn.Count > MaxGroups) return Fail($"Too many groups (limit {MaxGroups}).");
        if (tagsIn.Any(t => t is null) || groupsIn.Any(g => g is null))
            return Fail("The file contains an empty tag or group entry.");

        // A new Board gets a new Id automatically, and so does everything created below.
        var board = new Board { Name = name };

        // Old tag id (from the file) -> new Tag, so tasks can be re-pointed at the new tags.
        var tagMap = new Dictionary<Guid, Tag>();
        foreach (var tagIn in tagsIn)
        {
            var title = (tagIn.Title ?? "").Trim();
            if (title.Length is 0 or > 50) return Fail("Every tag needs a title of 1-50 characters.");
            if (!IsColour(tagIn.Colour)) return Fail($"Tag \"{title}\" has an invalid colour (use #rrggbb).");

            var tag = new Tag { Title = title, Colour = tagIn.Colour };
            if (!tagMap.TryAdd(tagIn.Id, tag)) return Fail($"Tag \"{title}\" has an id that is used twice.");
            board.Tags.Add(tag);
        }

        var taskTotal = 0;

        // Positions are rebuilt as 0, 1, 2... in the order given, whatever the file says.
        // OrderBy is stable, so equal positions keep their order in the file.
        var groupPosition = 0;
        foreach (var groupIn in groupsIn.OrderBy(g => g.Position))
        {
            var groupName = (groupIn.Name ?? "").Trim();
            if (groupName.Length is 0 or > 100) return Fail("Every group needs a name of 1-100 characters.");
            if (!IsColour(groupIn.Colour)) return Fail($"Group \"{groupName}\" has an invalid colour (use #rrggbb).");

            var group = new Group { Name = groupName, Colour = groupIn.Colour, Position = groupPosition++ };

            var tasksIn = groupIn.Tasks ?? [];
            taskTotal += tasksIn.Count;
            if (taskTotal > MaxTasks) return Fail($"Too many tasks (limit {MaxTasks}).");
            if (tasksIn.Any(t => t is null)) return Fail($"Group \"{groupName}\" contains an empty task entry.");

            var taskPosition = 0;
            foreach (var taskIn in tasksIn.OrderBy(t => t.Position))
            {
                var title = (taskIn.Title ?? "").Trim();
                if (title.Length is 0 or > 200) return Fail($"Every task needs a title of 1-200 characters (group \"{groupName}\").");

                var description = taskIn.Description ?? "";
                if (description.Length > MaxDescriptionLength)
                    return Fail($"Task \"{title}\" has a description over {MaxDescriptionLength} characters.");

                if (!Enum.IsDefined(taskIn.Priority)) return Fail($"Task \"{title}\" has an unknown priority.");

                var task = new TaskItem
                {
                    Title = title,
                    Description = description,
                    Priority = taskIn.Priority,
                    DueDate = taskIn.DueDate,
                    Position = taskPosition++
                };

                foreach (var oldTagId in (taskIn.TagIds ?? []).Distinct())
                {
                    if (!tagMap.TryGetValue(oldTagId, out var tag))
                        return Fail($"Task \"{title}\" uses a tag that is not listed in the file.");
                    task.Tags.Add(tag);
                }

                group.Tasks.Add(task);
            }

            board.Groups.Add(group);
        }

        return (board, null);
    }

    private static bool IsColour(string? colour) => colour is not null && HexColour.IsMatch(colour);

    private static (Board? Board, string? Error) Fail(string error) => (null, error);
}