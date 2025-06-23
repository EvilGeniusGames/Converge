using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Converge.Data;
using Converge.Data.Services;
using Converge.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Converge.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (OperatingSystem.IsWindows())
            {
                using var stream = AssetLoader.Open(new Uri("avares://Converge/Assets/icon.ico"));
                this.Icon = new WindowIcon(stream);
            }
            else
            {
                this.Icon = new WindowIcon("avares://Converge/Assets/icon.png");
            }
        }

        private void NewConnection_Click(object? sender, RoutedEventArgs e)
        {
            var editor = new ConnectionEditorWindow();
            editor.ShowDialog(this);
        }

        private void CloseApplicationMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void ChangePasswordMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            var db = Program.Services.GetRequiredService<ConvergeDbContext>();
            var saltSetting = db.SiteSettings.FirstOrDefault(s => s.Key == "EncryptionSalt");

            if (saltSetting == null)
            {
                await MessageBox("Encryption salt not found. Cannot change password.");
                return;
            }

            var changeWindow = new CreatePasswordWindow(requireOldPassword: true);
            var result = await changeWindow.ShowDialog<bool?>(this);

            if (result != true || string.IsNullOrWhiteSpace(changeWindow.EnteredPassword))
                return;

            var newSalt = CryptoUtils.GenerateSalt();
            db.SiteSettings.Remove(saltSetting);
            db.SiteSettings.Add(new SiteSetting { Key = "EncryptionSalt", Value = newSalt });

            var testValue = CryptoUtils.Encrypt("CONVERGE-TEST", CryptoUtils.DeriveKey(changeWindow.EnteredPassword, newSalt));
            db.SiteSettings.Add(new SiteSetting { Key = "EncryptionCheck", Value = testValue });
            db.SaveChanges();

            CryptoVault.Key = CryptoUtils.DeriveKey(changeWindow.EnteredPassword, newSalt);
            await MessageBox("Password changed successfully.");
        }

        private async Task MessageBox(string message)
        {
            var dialog = new Window
            {
                Width = 300,
                Height = 120,
                Title = "Notice",
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var okButton = new Button
            {
                Content = "OK",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            okButton.Click += (_, _) => dialog.Close();

            dialog.Content = new StackPanel
            {
                Margin = new Thickness(20),
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    okButton
                }
            };

            await dialog.ShowDialog(this);
        }
    }
}
