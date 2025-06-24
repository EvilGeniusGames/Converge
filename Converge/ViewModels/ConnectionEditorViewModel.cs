using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Converge.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Converge.ViewModels;

public partial class ConnectionEditorViewModel : ObservableObject
{
    private readonly Window _owner;
    private readonly bool _isEditMode;

    public Connection Connection { get; }
    public List<string> ProtocolOptions { get; } = new() { "SSH", "RDP" ,"VNC" }; // Add "VNC" if needed
    public List<string> AuthOptions { get; } = new() { "Password", "Key File" };
    public bool ShowAuthTypeSelector => Protocol == "SSH";

    public ConnectionEditorViewModel(Window owner, Connection? connection = null)
    {
        _owner = owner;
        _isEditMode = connection != null;

        Connection = connection != null
            ? new Connection
            {
                Id = connection.Id,
                Name = connection.Name,
                Host = connection.Host,
                Port = connection.Port,
                Protocol = connection.Protocol,
                Username = connection.Username,
                AuthType = connection.AuthType,
                Password = connection.Password,
                KeyFilePath = connection.KeyFilePath,
                Notes = connection.Notes,
                FolderId = connection.FolderId,
                Order = connection.Order
            }
            : new Connection { Protocol = "SSH", AuthType = "Password", Port = 22 };
    }

    public string Name
    {
        get => Connection.Name;
        set => SetProperty(Connection.Name, value, Connection, (c, v) => c.Name = v);
    }

    public string Host
    {
        get => Connection.Host;
        set => SetProperty(Connection.Host, value, Connection, (c, v) => c.Host = v);
    }

    public int Port
    {
        get => Connection.Port;
        set => SetProperty(Connection.Port, value, Connection, (c, v) => c.Port = v);
    }

    public string Protocol
    {
        get => Connection.Protocol;
        set
        {
            if (SetProperty(Connection.Protocol, value, Connection, (c, v) => c.Protocol = v))
            {
                // Auto-set AuthType if not SSH
                if (value != "SSH")
                    AuthType = "Password";

                OnPropertyChanged(nameof(ShowAuthTypeSelector));
                OnPropertyChanged(nameof(IsPasswordAuth));
                OnPropertyChanged(nameof(IsKeyAuth));
            }
        }
    }

    public string Username
    {
        get => Connection.Username;
        set => SetProperty(Connection.Username, value, Connection, (c, v) => c.Username = v);
    }

    public string AuthType
    {
        get => Connection.AuthType;
        set
        {
            if (SetProperty(Connection.AuthType, value, Connection, (c, v) => c.AuthType = v))
            {
                OnPropertyChanged(nameof(IsPasswordAuth));
                OnPropertyChanged(nameof(IsKeyAuth));
            }
        }
    }

    public string? Password
    {
        get => Connection.Password;
        set => SetProperty(Connection.Password, value, Connection, (c, v) => c.Password = v);
    }

    public string? KeyFilePath
    {
        get => Connection.KeyFilePath;
        set => SetProperty(Connection.KeyFilePath, value, Connection, (c, v) => c.KeyFilePath = v);
    }

    public string? KeyPassphrase
    {
        get => Connection.Notes;
        set => SetProperty(Connection.Notes, value, Connection, (c, v) => c.Notes = v); // reused Notes for simplicity
    }

    public string? Notes
    {
        get => Connection.Notes;
        set => SetProperty(Connection.Notes, value, Connection, (c, v) => c.Notes = v);
    }

    public bool IsPasswordAuth => AuthType == "Password";
    public bool IsKeyAuth => AuthType == "Key File";

    [RelayCommand]
    [Obsolete]
    private async Task BrowseKeyFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Private Key File",
            AllowMultiple = false
        };
        var result = await dialog.ShowAsync(_owner);
        if (result != null && result.Length > 0)
            KeyFilePath = result[0];
    }

    [RelayCommand]
    private void Save()
    {
        Connection.LastUpdated = DateTime.UtcNow;
        _owner.Close(Connection);
    }

    private bool SetProperty<T>(T oldValue, T newValue, Connection connection, Action<Connection, T> setter, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            setter(connection, newValue);
            OnPropertyChanged(propertyName);
            return true;
        }
        return false;
    }
}
