using Avalonia.Controls;
using Avalonia.Interactivity;
using Converge.Data.Services;

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
    }


    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        var pass1 = (this.FindControl<TextBox>("PasswordBox1")).Text;
        var pass2 = (this.FindControl<TextBox>("PasswordBox2")).Text;

        var errorText = this.FindControl<TextBlock>("ErrorText");

        if (string.IsNullOrWhiteSpace(pass1))
        {
            errorText.Text = "Password cannot be empty.";
            return;
        }

        if (pass1 != pass2)
        {
            errorText.Text = "Passwords do not match.";
            return;
        }

        if (_requireOldPassword)
        {
            OldPassword = PasswordBox.Text;
        }

        EnteredPassword = pass1;
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    public bool CreatePassword(bool requireOldPassword, string? existingSalt = null)
    {
        var oldPassword = this.FindControl<TextBox>("PasswordBox").Text;
        var newPassword = this.FindControl<TextBox>("PasswordBox1").Text;
        var confirmPassword = this.FindControl<TextBox>("PasswordBox2").Text;
        var errorText = this.FindControl<TextBlock>("ErrorText");

        if (requireOldPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(existingSalt))
            {
                errorText.Text = "Old password is required.";
                return false;
            }

            var testKey = CryptoUtils.DeriveKey(oldPassword, existingSalt);
            try
            {
                // Attempt to decrypt something (or use a test checksum later)
                // For now: just assign if you trust it
                CryptoVault.Key = testKey;
            }
            catch
            {
                errorText.Text = "Old password is incorrect.";
                return false;
            }
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

}
