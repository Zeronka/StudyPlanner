namespace StudyPlanner.Models;

public sealed record StudyTask(int Id, string Title, string Subject,
    DateOnly? DueDate, bool IsCompleted);
public sealed record TaskInput(string? Title, string? Subject,
    DateOnly? DueDate, bool IsCompleted = false);
