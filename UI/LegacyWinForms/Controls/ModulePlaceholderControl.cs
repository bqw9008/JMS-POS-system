using POS_system_cs.Application.Navigation;

namespace POS_system_cs.UI.Controls;

public sealed class ModulePlaceholderControl : UserControl
{
    public ModulePlaceholderControl(ModuleDefinition module)
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(247, 249, 252);
        Padding = new Padding(28);

        var title = new Label
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 41, 55),
            Text = module.Title
        };

        var description = new Label
        {
            AutoSize = false,
            Font = new Font("Microsoft YaHei UI", 10.5F),
            ForeColor = Color.FromArgb(75, 85, 99),
            Height = 48,
            Dock = DockStyle.Top,
            Text = module.Description
        };

        var actionPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 16, 0, 0),
            WrapContents = true
        };

        foreach (var action in module.PrimaryActions)
        {
            actionPanel.Controls.Add(CreateActionButton(action));
        }

        var emptyState = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 10F),
            ForeColor = Color.FromArgb(107, 114, 128),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "模块页面骨架已预留，下一步接入具体业务界面和服务实现。"
        };

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(24)
        };

        content.Controls.Add(emptyState);
        content.Controls.Add(actionPanel);
        content.Controls.Add(description);
        content.Controls.Add(title);

        Controls.Add(content);
    }

    private static Button CreateActionButton(string text)
    {
        var button = new Button
        {
            AutoSize = true,
            BackColor = Color.FromArgb(37, 99, 235),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei UI", 9.5F),
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 10, 10),
            Padding = new Padding(14, 6, 14, 6),
            Text = text,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }
}
