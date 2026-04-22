using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;
using POS_system_cs.UI.Wpf.Localization;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed class CategoryManagementPage : WpfUserControl
{
    private readonly ICategoryService _service;
    private readonly ObservableCollection<Category> _rows = [];
    private readonly WpfDataGrid _grid = WpfUi.Grid();
    private readonly WpfTextBox _name = WpfUi.TextBox();
    private readonly WpfTextBox _description = WpfUi.TextBox(multiline: true);
    private readonly WpfCheckBox _active = new() { Content = Localizer.T("Field.Active"), IsChecked = true };
    private Category? _selected;

    public CategoryManagementPage(ICategoryService service)
    {
        _service = service;
        Content = WpfUi.SplitPage(Localizer.T("Category.Title"), Localizer.T("Category.Desc"), out var list, out var form);
        BuildList(list);
        BuildForm(form);
        Loaded += async (_, _) => await LoadAsync();
    }

    private void BuildList(Border host)
    {
        _grid.ItemsSource = _rows;
        _grid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Name"), nameof(Category.Name), star: true));
        _grid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Description"), nameof(Category.Description), 240));
        _grid.Columns.Add(WpfUi.CheckColumn(Localizer.T("Field.Active"), nameof(Category.IsActive), 80));
        _grid.SelectionChanged += (_, _) => Bind();
        host.Child = _grid;
    }

    private void BuildForm(Border host)
    {
        var form = WpfUi.Form();
        form.Children.Add(WpfUi.Title(Localizer.T("Category.Form")));
        form.Children.Add(WpfUi.Field(Localizer.T("Field.Name"), _name));
        form.Children.Add(WpfUi.Field(Localizer.T("Field.Description"), _description));
        form.Children.Add(_active);
        form.Children.Add(WpfUi.Primary(Localizer.T("Action.Save"), async (_, _) => await SaveAsync()));
        form.Children.Add(WpfUi.Secondary(Localizer.T("Action.New"), (_, _) => Clear()));
        form.Children.Add(WpfUi.Danger(Localizer.T("Action.Delete"), async (_, _) => await DeleteAsync()));
        form.Children.Add(WpfUi.Secondary(Localizer.T("Action.Refresh"), async (_, _) => await LoadAsync()));
        host.Child = new ScrollViewer { Content = form };
    }

    private async Task LoadAsync()
    {
        try { _rows.ReplaceWith(await _service.GetAllAsync()); }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private async Task SaveAsync()
    {
        try
        {
            var row = _selected ?? new Category();
            row.Name = _name.Text.Trim();
            row.Description = _description.Text.Trim();
            row.IsActive = _active.IsChecked == true;
            await _service.SaveAsync(row);
            await LoadAsync();
            Clear();
        }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private async Task DeleteAsync()
    {
        if (_selected is null || !WpfUi.Confirm(this, Localizer.T("Category.DeleteConfirm"))) return;
        try
        {
            await _service.DeleteAsync(_selected.Id);
            await LoadAsync();
            Clear();
        }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private void Bind()
    {
        if (_grid.SelectedItem is not Category row) return;
        _selected = row;
        _name.Text = row.Name;
        _description.Text = row.Description ?? string.Empty;
        _active.IsChecked = row.IsActive;
    }

    private void Clear()
    {
        _selected = null;
        _grid.SelectedItem = null;
        _name.Clear();
        _description.Clear();
        _active.IsChecked = true;
    }
}
