namespace ModernTodo.Services;

public sealed class TodoValidationException : Exception
{
    public TodoValidationException(IReadOnlyDictionary<string, string> errors)
        : base("Les données de la tâche ne sont pas valides.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string> Errors { get; }
}
