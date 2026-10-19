namespace dotnet_kanban.Models;

// One labelled row of a dashboard breakdown (a group, tag, priority...),
// with the colour used for its bar.
public sealed record CountRow(string Name, string Colour, int Count);

// The numbers shown on the board dashboard. Built straight from a loaded
// Board — a pure function with no database access (like BoardImporter).
public sealed record DashboardStats
{
    public int TotalTasks { get; init; }
    public int GroupCount { get; init; }
    public int TagCount { get; init; }
    public int Overdue { get; init; } // due date before today
    public int DueSoon { get; init; } // due within the next 7 days
    public int UntaggedTasks { get; init; }

    public List<CountRow> TasksPerGroup { get; init; } = [];
    public List<CountRow> TasksPerTag { get; init; } = [];
    public List<CountRow> TasksPerPriority { get; init; } = [];
    public List<CountRow> TasksPerDueDate { get; init; } = [];

    public static DashboardStats From(Board board)
    {
        var tasks = board.Groups.SelectMany(g => g.Tasks).ToList();

        var today = DateTime.Today;
        var soon = today.AddDays(7);

        var overdue = tasks.Count(t => t.DueDate is { } due && due.Date < today);
        var dueSoon = tasks.Count(t => t.DueDate is { } due && due.Date >= today && due.Date < soon);
        var later = tasks.Count(t => t.DueDate is { } due && due.Date >= soon);

        // Tags live on the board; count how many tasks carry each one.
        var tagCounts = tasks
            .SelectMany(t => t.Tags)
            .GroupBy(tag => tag.Id)
            .ToDictionary(group => group.Key, group => group.Count());

        var perTag = board.Tags
            .Select(tag => new CountRow(tag.Title, tag.Colour, tagCounts.GetValueOrDefault(tag.Id)))
            .OrderByDescending(row => row.Count)
            .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new DashboardStats
        {
            TotalTasks = tasks.Count,
            GroupCount = board.Groups.Count,
            TagCount = board.Tags.Count,
            Overdue = overdue,
            DueSoon = dueSoon,
            UntaggedTasks = tasks.Count(t => t.Tags.Count == 0),

            TasksPerGroup =
            [
                .. board.Groups
                    .OrderBy(g => g.Position)
                    .Select(g => new CountRow(g.Name, g.Colour, g.Tasks.Count))
            ],
            TasksPerTag = [.. perTag],

            TasksPerPriority =
            [
                new CountRow("High", "#ef4444", tasks.Count(t => t.Priority == Priority.High)),
                new CountRow("Medium", "#f59e0b", tasks.Count(t => t.Priority == Priority.Medium)),
                new CountRow("Low", "#3b82f6", tasks.Count(t => t.Priority == Priority.Low)),
                new CountRow("None", "#71717a", tasks.Count(t => t.Priority == Priority.None))
            ],

            TasksPerDueDate =
            [
                new CountRow("Overdue", "#ef4444", overdue),
                new CountRow("Next 7 days", "#f59e0b", dueSoon),
                new CountRow("Later", "#3b82f6", later),
                new CountRow("No due date", "#71717a", tasks.Count(t => t.DueDate is null))
            ]
        };
    }
}
