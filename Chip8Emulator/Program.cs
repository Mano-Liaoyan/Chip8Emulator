using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Logging;

namespace Chip8Emulator;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        Trace.Listeners.Add(new ConsoleTraceListener());
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .With(new X11PlatformOptions
            {
                // Force whitelist llvmpipe for virtual machines
                GlxRendererBlacklist = []
            })
            .With(new Win32PlatformOptions
                { RenderingMode = new Collection<Win32RenderingMode> { Win32RenderingMode.Wgl } })
            .LogToTrace(LogEventLevel.Verbose);
    }
}