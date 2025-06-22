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
