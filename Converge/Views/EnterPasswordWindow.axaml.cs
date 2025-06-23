using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Converge.Views;

public partial class EnterPasswordWindow : Window
{
    public string? EnteredPassword { get; private set; }

    public EnterPasswordWindow()
    {
        InitializeComponent();
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        var password = this.FindControl<TextBox>("PasswordBox").Text;
        var errorText = this.FindControl<TextBlock>("ErrorText");

        if (string.IsNullOrWhiteSpace(password))
        {
            errorText.Text = "Password cannot be empty.";
            return;
        }

        EnteredPassword = password;
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
