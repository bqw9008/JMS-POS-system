using System.Windows;
using POS_system_cs.Domain.Entities;
using POS_system_cs.UI.Wpf.Localization;

namespace POS_system_cs.UI.Wpf.Controls;

public sealed partial class CategoryEditorWindow : Window
{
    private readonly Category _category;

    public CategoryEditorWindow(Category? category)
    {
        _category = category is null ? new Category() : Clone(category);

        InitializeComponent();
        Title = category is null ? Localizer.T("Action.New") : Localizer.T("Action.Edit");
        ApplyLocalization();
        Bind();

        Loaded += (_, _) =>
        {
            NameBox.Focus();
            NameBox.SelectAll();
        };
    }

    public Category Result { get; private set; } = new();

    private void ApplyLocalization()
    {
        NameLabel.Text = Localizer.T("Field.Name");
        DescriptionLabel.Text = Localizer.T("Field.Description");
        ActiveCheckBox.Content = Localizer.T("Field.Active");
        SaveButton.Content = Localizer.T("Action.Save");
        CancelButton.Content = Localizer.T("Action.Cancel");
    }

    private void Bind()
    {
        NameBox.Text = _category.Name;
        DescriptionBox.Text = _category.Description ?? string.Empty;
        ActiveCheckBox.IsChecked = _category.IsActive;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _category.Name = NameBox.Text.Trim();
        _category.Description = DescriptionBox.Text.Trim();
        _category.IsActive = ActiveCheckBox.IsChecked == true;

        Result = _category;
        DialogResult = true;
    }

    private static Category Clone(Category category)
    {
        return new Category
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}
