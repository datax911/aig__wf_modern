using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ModernTodo.Data;

public sealed class TodoDesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<TodoDbContext>
{
    public TodoDbContext CreateDbContext(string[] args)
    {
        var basePath = FindProjectDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var configuredConnectionString =
            configuration.GetConnectionString("TodoDatabase")
            ?? throw new InvalidOperationException(
                "La chaîne de connexion 'TodoDatabase' est absente de appsettings.json.");

        var connectionString = SqliteConnectionStringResolver.Resolve(
            configuredConnectionString,
            basePath);

        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new TodoDbContext(options);
    }

    private static string FindProjectDirectory()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        if (File.Exists(Path.Combine(currentDirectory, "appsettings.json")))
        {
            return currentDirectory;
        }

        var projectDirectory = Path.Combine(currentDirectory, "ModernTodo");
        if (File.Exists(Path.Combine(projectDirectory, "appsettings.json")))
        {
            return projectDirectory;
        }

        throw new DirectoryNotFoundException(
            "Le dossier du projet ModernTodo contenant appsettings.json est introuvable.");
    }
}
