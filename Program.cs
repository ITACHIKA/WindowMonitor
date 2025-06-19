using Avalonia;
using System;
using System.Threading;
using System.Runtime.InteropServices;
using Avalonia.ReactiveUI;
using Tmds.DBus.Protocol;

namespace windowLogger;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
    private static Mutex SingleInstanceMutex = new Mutex(true, "WindowLoggerAppMutex");
    [STAThread]
    public static void Main(string[] args)
    {
        if (!SingleInstanceMutex.WaitOne(TimeSpan.Zero, true))
        {
            MessageBox(IntPtr.Zero, "An instance is already running", "Warning", 0x30);
            return;
        }
        BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

        SingleInstanceMutex.ReleaseMutex();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .UseReactiveUI()
            .LogToTrace();
}
