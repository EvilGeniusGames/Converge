using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Converge.Data;

namespace Converge
{
    internal sealed class Program
    {
        public static IServiceProvider Services { get; private set; } = default!;

        [STAThread]
        public static void Main(string[] args)
        {
            var services = new ServiceCollection();

            services.AddDbContext<ConvergeDbContext>(options =>
                options.UseSqlite("Data Source=converge.db"));

            Services = services.BuildServiceProvider();

            // Run migrations at startup
            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ConvergeDbContext>();
                try
                {
                    db.Database.Migrate();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Migration failed: " + ex);
                }
                System.Diagnostics.Debug.WriteLine($"Working Directory: {Environment.CurrentDirectory}");
            }

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
