using POS_system_cs.Application.Navigation;
using POS_system_cs.UI.Wpf.Localization;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed partial class ModulePlaceholderPage : WpfUserControl
{
    public ModulePlaceholderPage(ModuleDefinition module)
    {
        InitializeComponent();
        TitleText.Text = Localizer.ModuleTitle(module.Key);
        DescriptionText.Text = Localizer.ModuleDescription(module.Key, module.Description);
    }
}
