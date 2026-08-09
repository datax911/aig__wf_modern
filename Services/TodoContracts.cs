using ModernTodo.Domain;

namespace ModernTodo.Services;

public enum TodoStatusFilter
{
    All,
    Active,
    Completed
}

public enum TodoSortField
{
    IsCompleted,
    Title,
    Priority,
    DueDate,
    CreatedAt
}

public enum TodoSortDirection
{
    Ascending,
    Descending
}

public sealed record TodoQuery(
    string? Search = null,
    TodoStatusFilter Status = TodoStatusFilter.All,
    TodoPriority? Priority = null,
    TodoSortField SortBy = TodoSortField.CreatedAt,
    TodoSortDirection SortDirection = TodoSortDirection.Descending);

public sealed record TodoSaveRequest(
    string Title,
    string? Notes,
    TodoPriority Priority,
    DateOnly? DueDate,
    bool IsCompleted = false);

public sealed record TodoStatistics(
    int Total,
    int Active,
    int Completed,
    int Overdue);
