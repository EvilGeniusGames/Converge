using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Converge.ViewModels;
using System;
using Converge.Data;

namespace Converge.Views;

public partial class ConnectionEditorWindow : Window
{
    public ConnectionEditorWindow(Connection? connection = null)
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

        DataContext = new ConnectionEditorViewModel(this, connection);
    }
    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ConnectionEditorViewModel vm)
        {
            this.Close(vm.Connection);
        }
    }


}