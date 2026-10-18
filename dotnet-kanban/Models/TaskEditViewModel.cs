namespace dotnet_kanban.Models;

public sealed record TaskEditViewModel(TaskItem Task, List<Tag> BoardTags);
