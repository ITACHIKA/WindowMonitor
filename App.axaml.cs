using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using windowLogger.ViewModels;
using System.Runtime.InteropServices;
using CommunityToolkit.WinUI.Notifications;

namespace windowLogger;


public partial class App : Application
{
    public static MainVm MainVm { get; set; } = new MainVm();
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Views.MainWindow();
            desktop.MainWindow.AttachDevTools();
            DataContext = MainVm;

            new ToastContentBuilder()
                .AddText("windowLogger is running in background")
                .Show();

        }

        base.OnFrameworkInitializationCompleted();
    }
    private void TrayQuit_OnClick(object? other, System.EventArgs args)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }
    private void TrayShow_OnClick(object? other, System.EventArgs args)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow!.Show();
        }
    }
        private void TrayHide_OnClick(object? other, System.EventArgs args)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow!.Hide();
        }
    }
}