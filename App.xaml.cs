using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Aquatir;

public partial class App : Application
{
    public bool ReturnToMainMenuAfterAddingProduct { get; set; } = false;

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        bool isAuthorized = Preferences.Get("IsAuthorized", false);

        Page startPage = isAuthorized ? new AppShell() : new NavigationPage(new PasswordPage());

        return new Window(startPage);
    }
}
