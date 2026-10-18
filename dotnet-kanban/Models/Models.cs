using System.Text.Json;
using System.Text.Json.Serialization;

namespace dotnet_kanban.Models;

public enum Priority { None = 0, Low = 1, Medium = 2, High = 3 }

public class Board
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";

    // Tags live on the board, tasks only store their IDs.
    public List<Tag> Tags { get; set; } = new();
    public List<Group> Groups { get; set; } = new();
}

public class Group
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BoardId { get; set; } // foreign key
    public Board? Board { get; set; } // convenience reference (doesn't get stored in db)
    public int Position { get; set; } // >= 0 order of groups on the board
    public string Name { get; set; } = "";
    public string Colour { get; set; } = "#808080"; // hex string
    public List<TaskItem> Tasks { get; set; } = new();
    public int TaskCount => Tasks.Count;
}

public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; } // foreign key
    public Group? Group { get; set; } 
    public int Position { get; set; }          
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<Tag> Tags { get; set; } = new();
    public Priority Priority { get; set; } = Priority.None;
    public DateTime? DueDate { get; set; }
}

public class Tag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BoardId { get; set; } // foreign key
    public Board? Board { get; set; } 
    public string Title { get; set; } = "";
    public string Colour { get; set; } = "#808080";
}

public static class BoardJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Export(Board board) =>
        JsonSerializer.Serialize(ExportBoard.From(board), Options);

}

public class ExportBoard
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<ExportTag> Tags { get; set; } = new();
    public List<ExportGroup> Groups { get; set; } = new();

    public static ExportBoard From(Board board) => new()
    {
        Id = board.Id,
        Name = board.Name,
        Tags = board.Tags.Select(ExportTag.From).ToList(),
        Groups = board.Groups.OrderBy(g => g.Position).Select(ExportGroup.From).ToList()
    };
}

public class ExportGroup
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public string Name { get; set; } = "";
    public string Colour { get; set; } = "#808080";
    public List<ExportTask> Tasks { get; set; } = new();

    public static ExportGroup From(Group group) => new()
    {
        Id = group.Id,
        Position = group.Position,
        Name = group.Name,
        Colour = group.Colour,
        Tasks = group.Tasks.OrderBy(t => t.Position).Select(ExportTask.From).ToList()
    };
}

public class ExportTask
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<Guid> TagIds { get; set; } = new();
    public Priority Priority { get; set; } = Priority.None;
    public DateTime? DueDate { get; set; }

    public static ExportTask From(TaskItem task) => new()
    {
        Id = task.Id,
        Position = task.Position,
        Title = task.Title,
        Description = task.Description,
        TagIds = task.Tags.Select(t => t.Id).ToList(),
        Priority = task.Priority,
        DueDate = task.DueDate
    };
}

public class ExportTag
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Colour { get; set; } = "#808080";

    public static ExportTag From(Tag tag) => new()
    {
        Id = tag.Id,
        Title = tag.Title,
        Colour = tag.Colour
    };
}
