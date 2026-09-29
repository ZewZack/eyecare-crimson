using System;
using System.Windows;

namespace eyecarebyzewzack;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            // Fail-safe global exception boundary
        };

        DispatcherUnhandledException += (s, args) =>
        {
            // Prevent unhandled UI crash
            args.Handled = true;
        };

        base.OnStartup(e);
    }
}
