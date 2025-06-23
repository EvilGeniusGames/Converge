using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using System;

namespace Converge.Views;

public partial class ConnectionEditorWindow : Window
{
    public ConnectionEditorWindow()
    {
        InitializeComponent();
        CancelButton.Click += (_, _) => this.Close();
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
    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

}