using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModernTodo.Data;
using ModernTodo.Domain;

namespace ModernTodo.Services;

public sealed class TodoService
{
    public const int MaximumTitleLength = 160;
    public const int MaximumNotesLength = 2_000;

    private readonly IDbContextFactory<TodoDbContext> _contextFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TodoService> _logger;

    public TodoService(
        IDbContextFactory<TodoDbContext> contextFactory,
        TimeProvider timeProvider,
        ILogger<TodoService> logger)
    {
        _contextFactory = contextFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TodoItem>> GetAsync(
        TodoQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<TodoItem> items = dbContext.TodoItems.AsNoTracking();

        items = query.Status switch
        {
            TodoStatusFilter.Active => items.Where(item => !item.IsCompleted),
            TodoStatusFilter.Completed => items.Where(item => item.IsCompleted),
            _ => items
        };

        if (query.Priority is { } priority)
        {
            items = items.Where(item => item.Priority == priority);
        }

        var normalizedSearch = query.Search?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            items = items.Where(item =>
                item.Title.ToLower().Contains(normalizedSearch)
                || (item.Notes != null
                    && item.Notes.ToLower().Contains(normalizedSearch)));
        }

        return await ApplyOrdering(items, query.SortBy, query.SortDirection)
            .ToListAsync(cancellationToken);
    }

    public async Task<TodoItem?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.TodoItems
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<TodoStatistics> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var total = await dbContext.TodoItems.CountAsync(cancellationToken);
        var active = await dbContext.TodoItems
            .CountAsync(item => !item.IsCompleted, cancellationToken);
        var completed = total - active;
        var overdue = await dbContext.TodoItems.CountAsync(
            item => !item.IsCompleted
                && item.DueDate != null
                && item.DueDate < today,
            cancellationToken);

        return new TodoStatistics(total, active, completed, overdue);
    }

    public async Task<TodoItem> CreateAsync(
        TodoSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeAndValidate(request);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var item = new TodoItem(
            normalized.Title,
            normalized.Notes,
            normalized.Priority,
            normalized.DueDate,
            normalized.IsCompleted,
            nowUtc);

        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.TodoItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tâche {TodoId} créée.", item.Id);
        return item;
    }

    public async Task<TodoItem?> UpdateAsync(
        int id,
        TodoSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeAndValidate(request);

        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var item = await dbContext.TodoItems
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (item is null)
        {
            return null;
        }

        item.Update(
            normalized.Title,
            normalized.Notes,
            normalized.Priority,
            normalized.DueDate,
            normalized.IsCompleted,
            _timeProvider.GetUtcNow().UtcDateTime);

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Tâche {TodoId} modifiée.", id);
        return item;
    }

    public async Task<bool> SetCompletedAsync(
        int id,
        bool isCompleted,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var item = await dbContext.TodoItems
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (item is null)
        {
            return false;
        }

        if (item.SetCompleted(isCompleted, _timeProvider.GetUtcNow().UtcDateTime))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Tâche {TodoId} marquée comme {TodoState}.",
                id,
                isCompleted ? "terminée" : "active");
        }

        return true;
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var item = await dbContext.TodoItems
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (item is null)
        {
            return false;
        }

        dbContext.TodoItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Tâche {TodoId} supprimée.", id);
        return true;
    }

    private static IOrderedQueryable<TodoItem> ApplyOrdering(
        IQueryable<TodoItem> items,
        TodoSortField sortBy,
        TodoSortDirection direction)
    {
        var orderedItems = (sortBy, direction) switch
        {
            (TodoSortField.IsCompleted, TodoSortDirection.Ascending) =>
                items.OrderBy(item => item.IsCompleted),

            (TodoSortField.IsCompleted, TodoSortDirection.Descending) =>
                items.OrderByDescending(item => item.IsCompleted),

            (TodoSortField.Title, TodoSortDirection.Ascending) =>
                items.OrderBy(item => item.Title.ToLower()),

            (TodoSortField.Title, TodoSortDirection.Descending) =>
                items.OrderByDescending(item => item.Title.ToLower()),

            (TodoSortField.Priority, TodoSortDirection.Ascending) =>
                items.OrderBy(item =>
                    item.Priority == TodoPriority.High
                        ? 3
                        : item.Priority == TodoPriority.Normal ? 2 : 1),

            (TodoSortField.Priority, TodoSortDirection.Descending) =>
                items.OrderByDescending(item =>
                    item.Priority == TodoPriority.High
                        ? 3
                        : item.Priority == TodoPriority.Normal ? 2 : 1),

            (TodoSortField.DueDate, TodoSortDirection.Ascending) =>
                items.OrderBy(item => item.DueDate),

            (TodoSortField.DueDate, TodoSortDirection.Descending) =>
                items.OrderByDescending(item => item.DueDate),

            (TodoSortField.CreatedAt, TodoSortDirection.Ascending) =>
                items.OrderBy(item => item.CreatedAtUtc),

            _ => items.OrderByDescending(item => item.CreatedAtUtc)
        };

        return orderedItems.ThenBy(item => item.Id);
    }

    private static NormalizedTodo NormalizeAndValidate(TodoSaveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var title = request.Title?.Trim() ?? string.Empty;
        var notes = string.IsNullOrWhiteSpace(request.Notes)
            ? null
            : request.Notes.Trim();
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);

        if (title.Length == 0)
        {
            errors[nameof(request.Title)] = "Le titre est obligatoire.";
        }
        else if (title.Length > MaximumTitleLength)
        {
            errors[nameof(request.Title)] =
                $"Le titre doit contenir au plus {MaximumTitleLength} caractères.";
        }

        if (notes?.Length > MaximumNotesLength)
        {
            errors[nameof(request.Notes)] =
                $"Les notes doivent contenir au plus {MaximumNotesLength} caractères.";
        }

        if (!Enum.IsDefined(request.Priority))
        {
            errors[nameof(request.Priority)] = "La priorité sélectionnée est invalide.";
        }

        if (errors.Count > 0)
        {
            throw new TodoValidationException(errors);
        }

        return new NormalizedTodo(
            title,
            notes,
            request.Priority,
            request.DueDate,
            request.IsCompleted);
    }

    private sealed record NormalizedTodo(
        string Title,
        string? Notes,
        TodoPriority Priority,
        DateOnly? DueDate,
        bool IsCompleted);
}
