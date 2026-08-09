using Microsoft.Data.Sqlite;

namespace ModernTodo.Data;

internal static class SqliteConnectionStringResolver
{
    public static string Resolve(string configuredConnectionString, string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        var builder = new SqliteConnectionStringBuilder(configuredConnectionString);
        var dataSource = Environment.ExpandEnvironmentVariables(builder.DataSource);

        if (string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            builder.DataSource = dataSource;
            return builder.ToString();
        }

        var absolutePath = Path.IsPathRooted(dataSource)
            ? Path.GetFullPath(dataSource)
            : Path.GetFullPath(Path.Combine(baseDirectory, dataSource));

        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        builder.DataSource = absolutePath;
        return builder.ToString();
    }
}
