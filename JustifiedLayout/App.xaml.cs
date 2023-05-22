using Microsoft.UI.Xaml;

namespace JustifiedLayout;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.AppWindow.Title = nameof(JustifiedLayout);
        _window.Activate();
    }

    private Window _window = null!;
}
