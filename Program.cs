using POS_system_cs.Infrastructure;

namespace POS_system_cs;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

        var app = new System.Windows.Application();
        RegisterGlobalExceptionLogging(app);

        try
        {
            AppCompositionRoot.Logger.Info("Application started.");
            AppCompositionRoot.InitializeAsync().GetAwaiter().GetResult();
            app.Run(AppCompositionRoot.CreateMainWindow());
            AppCompositionRoot.Logger.Info("Application exited.");
        }
        catch (Exception ex)
        {
            AppCompositionRoot.Logger.Error("Fatal application error.", ex);
            System.Windows.MessageBox.Show(ex.Message, "Operation failed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private static void RegisterGlobalExceptionLogging(System.Windows.Application app)
    {
        app.DispatcherUnhandledException += (_, e) =>
        {
            AppCompositionRoot.Logger.Error("Unhandled dispatcher exception.", e.Exception);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                AppCompositionRoot.Logger.Error("Unhandled application domain exception.", ex);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            AppCompositionRoot.Logger.Error("Unobserved task exception.", e.Exception);
        };
    }
}
