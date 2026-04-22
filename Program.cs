using POS_system_cs.Infrastructure;

namespace POS_system_cs;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        AppCompositionRoot.InitializeAsync().GetAwaiter().GetResult();
        System.Windows.Forms.Application.Run(AppCompositionRoot.CreateMainForm());
    }
}
