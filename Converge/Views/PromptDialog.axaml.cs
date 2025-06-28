using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Converge.Views;

public partial class PromptDialog : Window
{
    public string? Result { get; private set; }

    public PromptDialog(string title, string prompt)
    {
        InitializeComponent();
        DataContext = this;
        Title = title;
        PromptText = prompt;
    }

    private string _promptText = string.Empty;
    public string PromptText
    {
        get => _promptText;
        set
        {
            _promptText = value;
            this.FindControl<TextBlock>("PromptTextBlock").Text = value;
        }
    }


    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        var input = this.FindControl<TextBox>("InputBox").Text?.Trim();
        if (!string.IsNullOrEmpty(input))
        {
            Result = input;
            Close(true);
        }
    }
    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
