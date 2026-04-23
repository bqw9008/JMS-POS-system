using POS_system_cs.Application.Services;
using POS_system_cs.Domain.Entities;

namespace POS_system_cs.UI.Controls;

public sealed class CategoryManagementControl : UserControl
{
    private readonly ICategoryService _categoryService;
    private readonly DataGridView _grid = new();
    private readonly TextBox _nameTextBox = new();
    private readonly TextBox _descriptionTextBox = new();
    private readonly CheckBox _isActiveCheckBox = new() { Text = "启用", Checked = true, AutoSize = true };
    private Category? _selectedCategory;

    public CategoryManagementControl(ICategoryService categoryService)
    {
        _categoryService = categoryService;
        Dock = DockStyle.Fill;
        BuildLayout();
        _ = LoadCategoriesAsync();
    }

    private void BuildLayout()
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "分类名称", DataPropertyName = nameof(Category.Name), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "说明", DataPropertyName = nameof(Category.Description), Width = 220 });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "启用", DataPropertyName = nameof(Category.IsActive), Width = 70 });
        _grid.SelectionChanged += (_, _) => BindSelectedCategory();

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 9
        };
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label { Text = "分类信息", AutoSize = true, Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold) };
        _nameTextBox.Dock = DockStyle.Top;
        _descriptionTextBox.Dock = DockStyle.Top;
        _descriptionTextBox.Multiline = true;
        _descriptionTextBox.Height = 90;

        var newButton = CreateButton("新增", (_, _) => ClearForm());
        var saveButton = CreateButton("保存", async (_, _) => await SaveAsync());
        var deleteButton = CreateButton("删除", async (_, _) => await DeleteAsync());
        var refreshButton = CreateButton("刷新", async (_, _) => await LoadCategoriesAsync());

        form.Controls.Add(title);
        form.Controls.Add(CreateLabel("分类名称"));
        form.Controls.Add(_nameTextBox);
        form.Controls.Add(CreateLabel("说明"));
        form.Controls.Add(_descriptionTextBox);
        form.Controls.Add(_isActiveCheckBox);
        form.Controls.Add(newButton);
        form.Controls.Add(saveButton);
        form.Controls.Add(deleteButton);
        form.Controls.Add(refreshButton);

        var formHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };
        formHost.Controls.Add(form);

        content.Controls.Add(_grid, 0, 0);
        content.Controls.Add(formHost, 1, 0);
        Controls.Add(content);
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            _grid.DataSource = (await _categoryService.GetAllAsync()).ToList();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            var category = _selectedCategory ?? new Category();
            category.Name = _nameTextBox.Text;
            category.Description = _descriptionTextBox.Text;
            category.IsActive = _isActiveCheckBox.Checked;
            await _categoryService.SaveAsync(category);
            await LoadCategoriesAsync();
            ClearForm();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task DeleteAsync()
    {
        if (_selectedCategory is null)
        {
            return;
        }

        if (MessageBox.Show("确认删除当前分类？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            await _categoryService.DeleteAsync(_selectedCategory.Id);
            await LoadCategoriesAsync();
            ClearForm();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void BindSelectedCategory()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Category category)
        {
            return;
        }

        _selectedCategory = category;
        _nameTextBox.Text = category.Name;
        _descriptionTextBox.Text = category.Description ?? string.Empty;
        _isActiveCheckBox.Checked = category.IsActive;
    }

    private void ClearForm()
    {
        _selectedCategory = null;
        _grid.ClearSelection();
        _nameTextBox.Clear();
        _descriptionTextBox.Clear();
        _isActiveCheckBox.Checked = true;
        _nameTextBox.Focus();
    }

    private static Label CreateLabel(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(0, 10, 0, 4) };

    private static Button CreateButton(string text, EventHandler onClick)
    {
        var button = new Button { Text = text, Dock = DockStyle.Top, Height = 36, Margin = new Padding(0, 8, 0, 0) };
        button.Click += onClick;
        return button;
    }

    private static void ShowError(Exception ex)
    {
        MessageBox.Show(ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
