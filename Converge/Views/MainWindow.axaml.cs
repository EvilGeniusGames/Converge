using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Reactive;
using Converge.Data;
using Converge.Data.Services;
using Converge.Models;
using Converge.ViewModels;
using Converge.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Converge.Views
{
    public partial class MainWindow : Window
    {
        // Constructor initializes the main window and sets up the data context
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Closing += OnWindowClosing;
        }
        // Event handler for when the window is opened
        protected override async void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            // Set window icon
            if (OperatingSystem.IsWindows())
            {
                using var stream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://Converge/Assets/icon.ico"));
                this.Icon = new WindowIcon(stream);
            }
            else
            {
                this.Icon = new WindowIcon("avares://Converge/Assets/icon.png");
            }

            // Ensure DB and vault
            var db = Program.Services.GetRequiredService<ConvergeDbContext>();
            db.Database.Migrate();
            await VerifyOrCreateVaultAsync();

            // Get ViewModel early
            var vm = DataContext as MainWindowViewModel;

            // Load width from settings
            var setting = db.SiteSettings.FirstOrDefault(s => s.Key == "LastPaneWidth");
            if (setting != null && double.TryParse(setting.Value, out var storedWidth) && storedWidth > 42)
            {
                vm!.LeftPaneWidth = new GridLength(storedWidth);
            }

            // Load connections
            if (vm != null)
            {
                await vm.LoadConnectionsAsync(db);
            }

            var grid = this.FindControl<Grid>("MainLayoutGrid");
            var leftColumn = grid.ColumnDefinitions[0];

            

            double lastKnownWidth = -1;

            grid.LayoutUpdated += (_, _) =>
            {
                var currentWidth = leftColumn.ActualWidth;

                if (currentWidth != lastKnownWidth)
                {
                    lastKnownWidth = currentWidth;

                    // Update width tracking
                    if (vm?.IsPaneOpen == true)
                    {
                        vm.LeftPaneWidth = new GridLength(currentWidth);
                        vm.UpdateLastExpandedWidth(currentWidth);
                    }

                    // Show/hide filter row
                    this.FindControl<TextBox>("FilterBox").IsVisible = leftColumn.ActualWidth > 120;
                    this.FindControl<Button>("ClearFilterButton").IsVisible = leftColumn.ActualWidth > 120;
                }
            };
        }
        // Method to re-encrypt stored connection passwords with a new password
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
        // Event handler for the "New Connection" menu item
        private async void NewConnection_Click(object? sender, RoutedEventArgs e)
        {
            var editor = new ConnectionEditorWindow();
            var result = await editor.ShowDialog<Connection?>(this);

            if (result != null && DataContext is MainWindowViewModel vm)
            {
                result.LastUpdated = DateTime.UtcNow;

                if (vm.SelectedItem is { Connection: null, FolderId: not null } selectedFolder)
                {
                    result.FolderId = selectedFolder.FolderId;
                }

                var db = Program.Services.GetRequiredService<ConvergeDbContext>();
                db.Connections.Add(result);
                await db.SaveChangesAsync();

                await vm.LoadConnectionsAsync(db);
            }
        }
        // Event handler for the "Edit Connection" menu item
        private async void EditConnection_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm &&
                vm.SelectedItem?.Connection is Connection original)
            {
                var editor = new ConnectionEditorWindow(original);
                var result = await editor.ShowDialog<Connection?>(this);

                if (result != null)
                {
                    result.Id = original.Id;
                    result.LastUpdated = DateTime.UtcNow;

                    var db = Program.Services.GetRequiredService<ConvergeDbContext>();
                    db.Entry(original).CurrentValues.SetValues(result);
                    await db.SaveChangesAsync();

                    await vm.LoadConnectionsAsync(db);
                }
            }
        }
        // TODO: Replace individual DeleteConnection_Click and DeleteFolder_Click with a unified DeleteSelectedItem_Click method
        //       that checks the SelectedItem context and performs the appropriate deletion.

        // Event handler for the "Delete Connection" menu item
        private async void DeleteConnection_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm || vm.SelectedItem?.Connection is not Connection conn)
                return;

            var confirm = await MessageBoxConfirm($"Delete connection '{conn.Name}'?");
            if (!confirm) return;

            var db = Program.Services.GetRequiredService<ConvergeDbContext>();
            db.Connections.Remove(conn);
            await db.SaveChangesAsync();

            await vm.LoadConnectionsAsync(db);
        }
        // Event handler for the "Close" menu item
        private void CloseApplicationMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }
        // Event handler for the "Change Password" menu item
        private async void ChangePasswordMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            var changeWindow = new CreatePasswordWindow(requireOldPassword: true);
            var result = await changeWindow.ShowDialog<bool?>(this);

            if (result == true)
                await MessageBox("Password changed successfully.");
        }
        // Method to show a simple message box with a message
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
        // Method to verify or create the vault encryption settings
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
                var check = db.SiteSettings.FirstOrDefault(s => s.Key == "EncryptionCheck")?.Value;

                while (true)
                {
                    var enter = new EnterPasswordWindow(saltSetting.Value, check ?? "");
                    var result = await enter.ShowDialog<bool?>(this);

                    if (result == true && !string.IsNullOrWhiteSpace(enter.EnteredPassword))
                    {
                        var derivedKey = CryptoUtils.DeriveKey(enter.EnteredPassword, saltSetting.Value);
                        CryptoVault.Key = derivedKey;
                        break;
                    }

                    Environment.Exit(0);
                }
            }
            return true;
        }
        // Event handler for window closing to save the last pane width
        private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                var db = Program.Services.GetRequiredService<ConvergeDbContext>();
                var width = vm.LeftPaneWidth.Value.ToString();

                var existing = db.SiteSettings.FirstOrDefault(s => s.Key == "LastPaneWidth");
                if (existing != null)
                {
                    existing.Value = width;
                }
                else
                {
                    db.SiteSettings.Add(new SiteSetting { Key = "LastPaneWidth", Value = width });
                }

                db.SaveChanges();
            }
        }
        // Event handler for the "Create Folder" menu item
        private void CreateFolder_Click(object? sender, RoutedEventArgs e)
        {
            _ = CreateNewFolderAsync();
        }
        // Event handler for the "Delete Folder" menu item
        private void DeleteFolder_Click(object? sender, RoutedEventArgs e)
        {
            _ = DeleteSelectedFolderAsync();
        }
        // Event handler for the "Filter" button click
        private void ClearFilter_Click(object? sender, RoutedEventArgs e)
        {
            this.FindControl<TextBox>("FilterBox").Text = string.Empty;
        }
        // Event handlers for Cut, Copy, and Paste actions (currently not implemented)
        private void Cut_Click(object? sender, RoutedEventArgs e) { /* TODO Cut_Click */ }
        private void Copy_Click(object? sender, RoutedEventArgs e) { /* TODO Copy_Click */ }
        private void Paste_Click(object? sender, RoutedEventArgs e) { /* TODO Paste_Click */ }
        // Event handler for the "New Folder" button click
        private async Task CreateNewFolderAsync()
        {
            var prompt = new PromptDialog("Create Folder", "Enter folder name:");
            var result = await prompt.ShowDialog<bool?>(this);

            if (result != true || string.IsNullOrWhiteSpace(prompt.Result))
                return;

            var folderName = prompt.Result.Trim();

            var db = Program.Services.GetRequiredService<ConvergeDbContext>();
            var vm = DataContext as MainWindowViewModel;

            try
            {
                int? parentId = null;
                if (vm?.SelectedItem is { Connection: null, FolderId: not null } selectedFolder)
                {
                    parentId = selectedFolder.FolderId;
                }

                var maxOrder = await db.Folders
                    .Where(f => f.ParentId == parentId)
                    .Select(f => (int?)f.Order)
                    .MaxAsync() ?? 0;

                var order = maxOrder + 1;

                db.Folders.Add(new Folder
                {
                    Name = folderName,
                    ParentId = parentId,
                    Order = order
                });

                await db.SaveChangesAsync();
                await vm!.LoadConnectionsAsync(db);
            }
            catch (Exception ex)
            {
                await MessageBox($"Error: {ex.Message}");
            }
        }
        // Method to Delete and confirm deletion of a folder
        private async Task DeleteSelectedFolderAsync()
        {
            var vm = DataContext as MainWindowViewModel;
            if (vm?.SelectedItem is not { Connection: null } selectedFolder)
                return;

            var confirm = await MessageBoxConfirm($"Delete folder '{selectedFolder.Name}' and all contents?");
            if (!confirm)
                return;

            var db = Program.Services.GetRequiredService<ConvergeDbContext>();
            if (selectedFolder.FolderId is null)
                return;

            var folder = await db.Folders
                .Include(f => f.Children)
                .Include(f => f.Connections)
                .FirstOrDefaultAsync(f => f.Id == selectedFolder.FolderId);


            if (folder == null)
                return;

            if (folder.Children.Any() || folder.Connections.Any())
            {
                db.Connections.RemoveRange(folder.Connections);
                db.Folders.RemoveRange(folder.Children);
            }

            db.Folders.Remove(folder);
            await db.SaveChangesAsync();

            await vm.LoadConnectionsAsync(db);
        }
        // Method to show a confirmation message box
        private async Task<bool> MessageBoxConfirm(string message)
        {
            var tcs = new TaskCompletionSource<bool>();

            var dialog = new Window
            {
                Width = 300,
                Height = 140,
                Title = "Confirm",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            var yes = new Button { Content = "Yes", Width = 80 };
            var no = new Button { Content = "No", Width = 80 };

            yes.Click += (_, _) => { tcs.SetResult(true); dialog.Close(); };
            no.Click += (_, _) => { tcs.SetResult(false); dialog.Close(); };

            dialog.Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 10,
                Children =
        {
            new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Spacing = 8,
                Children = { yes, no }
            }
        }
            };

            await dialog.ShowDialog(this);
            return await tcs.Task;
        }

    }
}
