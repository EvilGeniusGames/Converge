using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Converge.Views
{
    // TODO: Investigate why MessageBoxWindow does not auto-resize correctly despite SizeToContent="WidthAndHeight".
    //       May require forcing InvalidateMeasure() or delaying layout updates.

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
