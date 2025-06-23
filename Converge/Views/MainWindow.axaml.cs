using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Converge.Data;
using Converge.Data.Services;
using Converge.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Converge.ViewModels;

namespace Converge.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        protected override async void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            // Set the window icon based on the operating system
            if (OperatingSystem.IsWindows())
            {
                using var stream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://Converge/Assets/icon.ico"));
                this.Icon = new WindowIcon(stream);
            }
            else
            {
                this.Icon = new WindowIcon("avares://Converge/Assets/icon.png");
            }

            // Ensure the database is migrated and the vault is verified or created
            var db = Program.Services.GetRequiredService<ConvergeDbContext>();
            db.Database.Migrate();

            // Verify or create the vault
            await VerifyOrCreateVaultAsync();

            // Load the connections into the view model
            if (DataContext is MainWindowViewModel vm)
            {
                await vm.LoadConnectionsAsync(db);
            }
        }

        private async Task ReencryptStoredConnectionPasswordsAsync(string oldPassword, string newPassword)
        {
            var db = Program.Services.GetRequiredService<ConvergeDbContext>();

            var saltSetting = db.SiteSettings.FirstOrDefault(s => s.Key == "EncryptionSalt");
            if (saltSetting == null)
                return;

            var oldKey = CryptoUtils.DeriveKey(oldPassword, saltSetting.Value);
            var newSalt = CryptoUtils.GenerateSalt();
            var newKey = CryptoUtils.DeriveKey(newPassword, newSalt);

            var connections = db.Connections.ToList();

            foreach (var conn in connections)
            {
                if (!string.IsNullOrEmpty(conn.Password))
                {
                    try
                    {
                        var decrypted = CryptoUtils.Decrypt(conn.Password, oldKey);
                        conn.Password = CryptoUtils.Encrypt(decrypted, newKey);
                    }
                    catch
                    {
                        continue; // Skip corrupted or mismatched entries
                    }
                }
            }

            var checkSetting = db.SiteSettings.FirstOrDefault(s => s.Key == "EncryptionCheck");
            if (checkSetting != null) db.SiteSettings.Remove(checkSetting);
            db.SiteSettings.Remove(saltSetting);

            db.SiteSettings.Add(new SiteSetting { Key = "EncryptionSalt", Value = newSalt });
            db.SiteSettings.Add(new SiteSetting
            {
                Key = "EncryptionCheck",
                Value = CryptoUtils.Encrypt("CONVERGE-TEST", newKey)
            });

            await db.SaveChangesAsync();

            CryptoVault.Key = newKey;
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
            // Ensure the database context is available
            var db = Program.Services.GetRequiredService<ConvergeDbContext>();
            var saltSetting = db.SiteSettings.FirstOrDefault(s => s.Key == "EncryptionSalt");
            // If no salt setting exists, prompt the user to create a new password
            if (saltSetting == null)
            {
                await MessageBox("Encryption salt not found. Cannot change password.");
                return;
            }
            // Prompt the user for the old password if it exists
            var changeWindow = new CreatePasswordWindow(requireOldPassword: true);
            var result = await changeWindow.ShowDialog<bool?>(this);
            // If the user cancels or doesn't enter a password, exit
            if (result != true || string.IsNullOrWhiteSpace(changeWindow.EnteredPassword))
                return;
            // Verify the old password
            var newSalt = CryptoUtils.GenerateSalt();
            db.SiteSettings.Remove(saltSetting);
            db.SiteSettings.Add(new SiteSetting { Key = "EncryptionSalt", Value = newSalt });
            // Encrypt a test value to verify the new password
            var testValue = CryptoUtils.Encrypt("CONVERGE-TEST", CryptoUtils.DeriveKey(changeWindow.EnteredPassword, newSalt));
            db.SiteSettings.Add(new SiteSetting { Key = "EncryptionCheck", Value = testValue });
            db.SaveChanges();
            // Reencrypt all stored connection passwords with the new password
            await ReencryptStoredConnectionPasswordsAsync(changeWindow.OldPassword!, changeWindow.EnteredPassword!);
            // Update the CryptoVault key with the new derived key
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
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
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

        private async Task<bool> VerifyOrCreateVaultAsync()
        {
            var db = Program.Services.GetRequiredService<ConvergeDbContext>();
            var saltSetting = db.SiteSettings.FirstOrDefault(s => s.Key == "EncryptionSalt");

            if (saltSetting == null)
            {
                var create = new CreatePasswordWindow(requireOldPassword: false);
                var result = await create.ShowDialog<bool?>(this);

                if (result != true || string.IsNullOrWhiteSpace(create.EnteredPassword))
                {
                    Environment.Exit(0);
                }

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
                var result = await enter.ShowDialog<bool?>(this);

                if (result != true || string.IsNullOrWhiteSpace(enter.EnteredPassword))
                {
                    Environment.Exit(0);
                }

                var derivedKey = CryptoUtils.DeriveKey(enter.EnteredPassword, saltSetting.Value);

                var check = db.SiteSettings.FirstOrDefault(s => s.Key == "EncryptionCheck");
                if (check == null || CryptoUtils.Decrypt(check.Value, derivedKey) != "CONVERGE-TEST")
                {
                    Environment.Exit(0);
                }

                CryptoVault.Key = derivedKey;
            }

            return true;
        }

    }
}
