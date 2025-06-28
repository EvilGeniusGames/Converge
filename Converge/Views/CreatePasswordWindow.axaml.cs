using Avalonia.Controls;
using Avalonia.Interactivity;
using Converge.Data;
using Converge.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Linq;
namespace Converge.Views;

public partial class CreatePasswordWindow : Window
{
    public string? EnteredPassword { get; private set; }
    public string? OldPassword { get; private set; }

    private readonly bool _requireOldPassword;

    public CreatePasswordWindow(bool requireOldPassword)
    {
        InitializeComponent();
        // store the requirement for old password
        _requireOldPassword = requireOldPassword;
        
        // password box is only visible if old password is required
        this.FindControl<TextBox>("PasswordBox").IsVisible = requireOldPassword;
        this.FindControl<TextBlock>("ErrorText").Text = "";

        // Hide label too if old password not required
        this.FindControl<TextBlock>("OldPasswordLabel").IsVisible = requireOldPassword;
        this.FindControl<TextBlock>("EnterOldText").IsVisible = requireOldPassword;

        this.Opened += (_, _) =>
        {
            this.FindControl<TextBox>("PasswordBox")?.Focus();
        };
    }
    // Event handler for the OK button to validate and set the entered password
    private async void Ok_Click(object? sender, RoutedEventArgs e)
    {
        var success = await ProcessPasswordChangeAsync();

        if (success)
            Close(true);
    }
    // Event handler for the Cancel button to close the window without setting a password
    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
    // Method to create a password, optionally requiring an old password for verification
    public bool CreatePassword(bool requireOldPassword, string? existingSalt = null, string? encryptedCheck = null)
    {
        var oldPassword = this.FindControl<TextBox>("PasswordBox").Text;
        var newPassword = this.FindControl<TextBox>("PasswordBox1").Text;
        var confirmPassword = this.FindControl<TextBox>("PasswordBox2").Text;
        var errorText = this.FindControl<TextBlock>("ErrorText");

        if (requireOldPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(existingSalt) || string.IsNullOrWhiteSpace(encryptedCheck))
            {
                errorText.Text = "Old password is required.";
                return false;
            }

            var testKey = CryptoUtils.DeriveKey(oldPassword, existingSalt);
            try
            {
                var check = CryptoUtils.Decrypt(encryptedCheck, testKey);
                if (check != "CONVERGE-TEST")
                    throw new InvalidOperationException();

                CryptoVault.Key = testKey;
            }
            catch
            {
                errorText.Text = "Old password is incorrect.";
                return false;
            }

            OldPassword = oldPassword;
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            errorText.Text = "New password cannot be empty.";
            return false;
        }

        if (newPassword != confirmPassword)
        {
            errorText.Text = "Passwords do not match.";
            return false;
        }

        var newSalt = CryptoUtils.GenerateSalt();
        var newKey = CryptoUtils.DeriveKey(newPassword, newSalt);
        CryptoVault.Key = newKey;
        EnteredPassword = newPassword;

        return true;
    }

    private async Task<bool> ProcessPasswordChangeAsync()
    {
        var oldPass = this.FindControl<TextBox>("PasswordBox").Text;
        var newPass = this.FindControl<TextBox>("PasswordBox1").Text;
        var confirmPass = this.FindControl<TextBox>("PasswordBox2").Text;
        var errorText = this.FindControl<TextBlock>("ErrorText");

        if (string.IsNullOrWhiteSpace(newPass))
        {
            errorText.Text = "New password cannot be empty.";
            return false;
        }

        if (newPass != confirmPass)
        {
            errorText.Text = "Passwords do not match.";
            return false;
        }

        var db = Program.Services.GetRequiredService<ConvergeDbContext>();
        var saltSetting = db.SiteSettings.FirstOrDefault(s => s.Key == "EncryptionSalt");
        var checkSetting = db.SiteSettings.FirstOrDefault(s => s.Key == "EncryptionCheck");

        if (_requireOldPassword)
        {
            if (saltSetting == null || checkSetting == null)
            {
                errorText.Text = "Unable to verify old password.";
                return false;
            }

            try
            {
                var oldKey = CryptoUtils.DeriveKey(oldPass, saltSetting.Value);
                var test = CryptoUtils.Decrypt(checkSetting.Value, oldKey);
                if (test != "CONVERGE-TEST")
                    throw new InvalidOperationException();

                OldPassword = oldPass;
            }
            catch
            {
                errorText.Text = "Old password is incorrect.";
                return false;
            }
        }

        // All checks passed – change password
        var newSalt = CryptoUtils.GenerateSalt();
        var newKey = CryptoUtils.DeriveKey(newPass, newSalt);

        // Re-encrypt existing stored passwords
        var connections = db.Connections.ToList();

        foreach (var conn in connections)
        {
            if (!string.IsNullOrEmpty(conn.Password))
            {
                try
                {
                    var decrypted = CryptoUtils.Decrypt(conn.Password, CryptoVault.Key);
                    conn.Password = CryptoUtils.Encrypt(decrypted, newKey);
                }
                catch
                {
                    continue; // skip invalid
                }
            }
        }

        // Overwrite salt and check
        if (saltSetting != null) db.SiteSettings.Remove(saltSetting);
        if (checkSetting != null) db.SiteSettings.Remove(checkSetting);

        db.SiteSettings.Add(new SiteSetting { Key = "EncryptionSalt", Value = newSalt });
        db.SiteSettings.Add(new SiteSetting
        {
            Key = "EncryptionCheck",
            Value = CryptoUtils.Encrypt("CONVERGE-TEST", newKey)
        });

        await db.SaveChangesAsync();

        CryptoVault.Key = newKey;
        EnteredPassword = newPass;

        return true;
    }

}
