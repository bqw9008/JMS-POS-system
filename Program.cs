using POS_system_cs.Infrastructure;

namespace POS_system_cs;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

        AppCompositionRoot.InitializeAsync().GetAwaiter().GetResult();

        var app = new System.Windows.Application();
        app.Run(AppCompositionRoot.CreateMainWindow());
    }
}
