// using System.Text.Json;
// using System.Text.Json.Serialization;

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
    public int Position { get; set; }            // >= 0 order of groups on the board
    public string Name { get; set; } = "";
    public string Colour { get; set; } = "#808080"; // hex string
    public List<TaskItem> Tasks { get; set; } = new();
    public int TaskCount => Tasks.Count;
}

// TaskItem because "Task" clashes with System.Threading.Tasks.Task.
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

/* public static class BoardJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Export(Board board) =>
        JsonSerializer.Serialize(board, Options);

    public static Board? Import(string json) =>
        JsonSerializer.Deserialize<Board>(json, Options);
} */