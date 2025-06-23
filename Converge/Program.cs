using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Converge.Data;
using Converge.Data.Services;
using Converge.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Converge
{
    internal sealed class Program
    {
        public static IServiceProvider Services { get; private set; } = default!;

        [STAThread]
        public static void Main(string[] args)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, async lifetime =>
            {
                var services = new ServiceCollection();

                services.AddDbContext<ConvergeDbContext>(options =>
                    options.UseSqlite("Data Source=converge.db"));

                Services = services.BuildServiceProvider();

                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ConvergeDbContext>();
                db.Database.Migrate();

                var unlockWindow = new MainWindow
                {
                    Width = 0,
                    Height = 0,
                    ShowInTaskbar = false,
                    Opacity = 0,
                    IsEnabled = false
                };
                unlockWindow.Show();

                var saltSetting = db.SiteSettings.FirstOrDefault(s => s.Key == "EncryptionSalt");

                if (saltSetting == null)
                {
                    var create = new CreatePasswordWindow(false);
                    var result = await create.ShowDialog<bool?>(unlockWindow);

                    if (result != true || string.IsNullOrWhiteSpace(create.EnteredPassword))
                        Environment.Exit(0);

                    var salt = CryptoUtils.GenerateSalt();
                    db.SiteSettings.Add(new SiteSetting { Key = "EncryptionSalt", Value = salt });

                    var derivedKey = CryptoUtils.DeriveKey(create.EnteredPassword, salt);
                    CryptoVault.Key = derivedKey;

                    var testValue = CryptoUtils.Encrypt("CONVERGE-TEST", derivedKey);
                    db.SiteSettings.Add(new SiteSetting { Key = "EncryptionCheck", Value = testValue });

                    db.SaveChanges();
                }
                else
                {
                    var enter = new EnterPasswordWindow();
                    var result = await enter.ShowDialog<bool?>(unlockWindow);

                    if (result != true || string.IsNullOrWhiteSpace(enter.EnteredPassword))
                        Environment.Exit(0);

                    var derivedKey = CryptoUtils.DeriveKey(enter.EnteredPassword, saltSetting.Value);

                    var check = db.SiteSettings.FirstOrDefault(s => s.Key == "EncryptionCheck");
                    if (check == null || CryptoUtils.Decrypt(check.Value, derivedKey) != "CONVERGE-TEST")
                    {
                        await new MessageBoxWindow("Invalid password.").ShowDialog(unlockWindow);
                        Environment.Exit(0);
                    }

                    CryptoVault.Key = derivedKey;
                }

                unlockWindow.Close();

                if (lifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = new MainWindow();
                }
            });
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
