using Avalonia.Controls;
using Avalonia.Interactivity;
using Converge.Data.Services;
using System;

namespace Converge.Views;

public partial class EnterPasswordWindow : Window
{   // This class represents a window that prompts the user to enter a password
    public string? EnteredPassword { get; private set; }
    private readonly string _salt;
    private readonly string _checkValue;

    // Constructor that initializes the window with the provided salt and check value
    public EnterPasswordWindow(string salt, string checkValue)
    {
        InitializeComponent();
        _salt = salt;
        _checkValue = checkValue;
        this.Opened += (_, _) =>
        {
            this.FindControl<TextBox>("PasswordBox")?.Focus();
        };
    }
    // Event handler for the window's Loaded event to set focus on the password box
    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        var passwordBox = this.FindControl<TextBox>("PasswordBox");
        var errorText = this.FindControl<TextBlock>("ErrorText");

        var password = passwordBox.Text;

        if (string.IsNullOrWhiteSpace(password))
        {
            errorText.Text = "Password cannot be empty.";
            return;
        }
        // Attempt to derive the key and decrypt the check value using the provided password and salt
        try
        {
            var key = CryptoUtils.DeriveKey(password, _salt);
            var decrypted = CryptoUtils.Decrypt(_checkValue, key);

            if (decrypted != "CONVERGE-TEST")
                throw new InvalidOperationException();

            EnteredPassword = password;
            Close(true);
        }
        catch
        {
            errorText.Text = "Incorrect password. Please try again.";
        }
    }
    // Event handler for the Cancel button to close the window without returning a password
    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
