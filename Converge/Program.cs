using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Converge.Data;
using Converge.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

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

            // Register ConnectionWindowManager as a singleton
            services.AddSingleton<IConnectionWindowManager, Converge.Services.ConnectionWindowManager>();

            Services = services.BuildServiceProvider();

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