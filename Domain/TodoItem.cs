namespace ModernTodo.Domain;

public sealed class TodoItem
{
    private TodoItem()
    {
    }

    internal TodoItem(
        string title,
        string? notes,
        TodoPriority priority,
        DateOnly? dueDate,
        bool isCompleted,
        DateTime nowUtc)
    {
        Title = title;
        Notes = notes;
        Priority = priority;
        DueDate = dueDate;
        IsCompleted = isCompleted;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        CompletedAtUtc = isCompleted ? nowUtc : null;
    }

    public int Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Notes { get; private set; }

    public TodoPriority Priority { get; private set; }

    public DateOnly? DueDate { get; private set; }

    public bool IsCompleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    internal void Update(
        string title,
        string? notes,
        TodoPriority priority,
        DateOnly? dueDate,
        bool isCompleted,
        DateTime nowUtc)
    {
        Title = title;
        Notes = notes;
        Priority = priority;
        DueDate = dueDate;

        if (IsCompleted != isCompleted)
        {
            IsCompleted = isCompleted;
            CompletedAtUtc = isCompleted ? nowUtc : null;
        }

        UpdatedAtUtc = nowUtc;
    }

    internal bool SetCompleted(bool isCompleted, DateTime nowUtc)
    {
        if (IsCompleted == isCompleted)
        {
            return false;
        }

        IsCompleted = isCompleted;
        CompletedAtUtc = isCompleted ? nowUtc : null;
        UpdatedAtUtc = nowUtc;
        return true;
    }
}
