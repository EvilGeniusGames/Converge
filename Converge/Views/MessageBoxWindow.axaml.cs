using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Converge.Views
{
    public partial class MessageBoxWindow : Window
    {
        public MessageBoxWindow(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        private void Ok_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
