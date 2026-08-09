using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModernTodo.Data;
using ModernTodo.Services;
using ModernTodo.UI;

namespace ModernTodo;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        IHost? host = null;

        try
        {
            host = CreateHostBuilder(args).Build();
            host.StartAsync().GetAwaiter().GetResult();

            InitializeDatabase(host.Services);
            Application.Run(host.Services.GetRequiredService<MainForm>());
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Modern Todo n'a pas pu démarrer.\n\n{exception.Message}",
                "Erreur de démarrage",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            if (host is not null)
            {
                host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                host.Dispose();
            }
        }
    }

    internal static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
            {
                var configuredConnectionString =
                    context.Configuration.GetConnectionString("TodoDatabase")
                    ?? throw new InvalidOperationException(
                        "La chaîne de connexion 'TodoDatabase' est absente de appsettings.json.");

                var connectionString = SqliteConnectionStringResolver.Resolve(
                    configuredConnectionString,
                    AppContext.BaseDirectory);

                services.AddDbContextFactory<TodoDbContext>(options =>
                    options.UseSqlite(connectionString));

                services.AddSingleton(TimeProvider.System);
                services.AddSingleton<TodoService>();
                services.AddSingleton<MainForm>();
            });

    private static void InitializeDatabase(IServiceProvider services)
    {
        var contextFactory = services.GetRequiredService<IDbContextFactory<TodoDbContext>>();

        using var dbContext = contextFactory.CreateDbContext();
        dbContext.Database.Migrate();
    }
}
