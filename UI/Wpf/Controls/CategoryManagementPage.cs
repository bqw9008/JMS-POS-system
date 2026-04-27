using System.Collections.ObjectModel;
using System.Windows.Controls;
using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;
using POS_system_cs.UI.Wpf.Localization;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed partial class CategoryManagementPage : WpfUserControl
{
    private readonly ICategoryService _service;
    private readonly ObservableCollection<Category> _rows = [];
    private Category? _selected;

    public CategoryManagementPage(ICategoryService service)
    {
        _service = service;
        InitializeComponent();
        ApplyLocalization();
        BuildList();
        Clear();
        Loaded += async (_, _) => await LoadAsync();
    }

    private void ApplyLocalization()
    {
        TitleText.Text = Localizer.T("Category.Title");
        DescriptionText.Text = Localizer.T("Category.Desc");
        FormTitleText.Text = Localizer.T("Category.Form");
        NameLabel.Text = Localizer.T("Field.Name");
        DescriptionLabel.Text = Localizer.T("Field.Description");
        ActiveCheckBox.Content = Localizer.T("Field.Active");
        EditButton.Content = Localizer.T("Action.Edit");
        NewButton.Content = Localizer.T("Action.New");
        DeleteButton.Content = Localizer.T("Action.Delete");
        RefreshButton.Content = Localizer.T("Action.Refresh");
    }

    private void BuildList()
    {
        CategoryGrid.ItemsSource = _rows;
        CategoryGrid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Name"), nameof(Category.Name), star: true));
        CategoryGrid.Columns.Add(WpfUi.TextColumn(Localizer.T("Field.Description"), nameof(Category.Description), 240));
        CategoryGrid.Columns.Add(WpfUi.CheckColumn(Localizer.T("Field.Active"), nameof(Category.IsActive), 80));
    }

    private async Task LoadAsync()
    {
        try { _rows.ReplaceWith(await _service.GetAllAsync()); }
        catch (Exception ex) { WpfUi.Error(this, ex); }
    }

    private async Task SaveAsync(Category category)
    {
        try
        {
            await _service.SaveAsync(category);
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
        if (CategoryGrid.SelectedItem is not Category row) return;
        _selected = row;
        NameBox.Text = row.Name;
        DescriptionBox.Text = row.Description ?? string.Empty;
        ActiveCheckBox.IsChecked = row.IsActive;
        EditButton.IsEnabled = true;
        DeleteButton.IsEnabled = true;
    }

    private void Clear()
    {
        _selected = null;
        CategoryGrid.SelectedItem = null;
        NameBox.Clear();
        DescriptionBox.Clear();
        ActiveCheckBox.IsChecked = true;
        EditButton.IsEnabled = false;
        DeleteButton.IsEnabled = false;
    }

    private async Task OpenEditorAsync(Category? category)
    {
        var dialog = new CategoryEditorWindow(category)
        {
            Owner = System.Windows.Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            await SaveAsync(dialog.Result);
        }
    }

    private void CategoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => Bind();

    private async void EditButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_selected is not null)
        {
            await OpenEditorAsync(_selected);
        }
    }

    private async void NewButton_Click(object sender, System.Windows.RoutedEventArgs e) => await OpenEditorAsync(null);

    private async void DeleteButton_Click(object sender, System.Windows.RoutedEventArgs e) => await DeleteAsync();

    private async void RefreshButton_Click(object sender, System.Windows.RoutedEventArgs e) => await LoadAsync();
}
