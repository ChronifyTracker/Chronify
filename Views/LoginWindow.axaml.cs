using Avalonia.Controls;

namespace Chronify.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        Opened += (_, _) => EmailInput.Focus();
    }
}
