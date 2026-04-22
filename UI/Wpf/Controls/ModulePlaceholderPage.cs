using System.Windows;
using System.Windows.Controls;
using POS_system_cs.Application.Navigation;
using POS_system_cs.UI.Wpf.Localization;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed class ModulePlaceholderPage : WpfUserControl
{
    public ModulePlaceholderPage(ModuleDefinition module)
    {
        var root = new Grid { Margin = new Thickness(28) };
        root.Children.Add(WpfUi.Card(WpfUi.Header(Localizer.ModuleTitle(module.Key), Localizer.ModuleDescription(module.Key, module.Description)), new Thickness(0)));
        Content = root;
    }
}
