using System.ComponentModel;
using System.Globalization;
using Microsoft.Extensions.Logging;
using ModernTodo.Domain;
using ModernTodo.Services;

namespace ModernTodo.UI;

public sealed partial class MainForm : Form
{
    private readonly TodoService _todoService;
    private readonly ILogger<MainForm> _logger;
    private readonly CancellationTokenSource _lifetimeCancellation = new();

    private CancellationTokenSource? _refreshCancellation;
    private List<TodoItem> _loadedItems = [];
    private bool _isLoaded;
    private bool _isBinding;
    private bool _isEditing;
    private bool _isEditorDirty;
    private bool _isEditorBusy;
    private bool _isLoading;
    private bool _suppressEditorEvents;
    private bool _suppressFilterEvents;
    private bool _resourcesDisposed;
    private int? _editingId;
    private EmptyStateAction _emptyStateAction;
    private TodoSortField _sortField = TodoSortField.CreatedAt;
    private TodoSortDirection _sortDirection = TodoSortDirection.Descending;

    public MainForm(TodoService todoService, ILogger<MainForm> logger)
    {
        _todoService = todoService;
        _logger = logger;

        InitializeComponent();
        ConfigureOptions();
        RegisterEvents();
        UpdateSortGlyphs();
        UpdateCommandState();
    }

    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.N) && !_isEditing)
        {
            BeginAdd();
            return true;
        }

        if (keyData == (Keys.Control | Keys.S) && _isEditing)
        {
            _ = SaveEditorAsync();
            return true;
        }

        if (keyData == Keys.Escape && _isEditing)
        {
            CancelEditor();
            return true;
        }

        if (keyData == Keys.Delete && _todoGrid.ContainsFocus && !_isEditing)
        {
            _ = DeleteSelectedAsync();
            return true;
        }

        if (keyData == Keys.Enter && _todoGrid.ContainsFocus && !_isEditing)
        {
            BeginEditSelected();
            return true;
        }

        return base.ProcessCmdKey(ref message, keyData);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            base.Dispose(disposing);
            return;
        }

        if (_resourcesDisposed)
        {
            return;
        }

        _resourcesDisposed = true;
        _isBinding = true;
        _todoGrid.SelectionChanged -= TodoGrid_SelectionChanged;

        _lifetimeCancellation.Cancel();
        _refreshCancellation?.Cancel();
        _searchTimer.Stop();
        _refreshCancellation?.Dispose();
        _lifetimeCancellation.Dispose();
        _searchTimer.Dispose();
        _todoBindingSource.Dispose();
        _errorProvider.Dispose();
        _toolTip.Dispose();
        _baseFont.Dispose();
        _headingFont.Dispose();
        _sectionFont.Dispose();
        _completedFont.Dispose();
        _gridHeaderFont.Dispose();

        base.Dispose(disposing);
    }

    private void ConfigureOptions()
    {
        _suppressFilterEvents = true;

        _statusFilterComboBox.DisplayMember = "Text";
        _statusFilterComboBox.DataSource = new[]
        {
            new ComboOption<TodoStatusFilter>(TodoStatusFilter.All, "Toutes"),
            new ComboOption<TodoStatusFilter>(TodoStatusFilter.Active, "À faire"),
            new ComboOption<TodoStatusFilter>(TodoStatusFilter.Completed, "Terminées")
        };

        _priorityFilterComboBox.DisplayMember = "Text";
        _priorityFilterComboBox.DataSource = new[]
        {
            new ComboOption<TodoPriority?>(null, "Toutes"),
            new ComboOption<TodoPriority?>(TodoPriority.Low, "Basse"),
            new ComboOption<TodoPriority?>(TodoPriority.Normal, "Normale"),
            new ComboOption<TodoPriority?>(TodoPriority.High, "Haute")
        };

        _editorPriorityComboBox.DisplayMember = "Text";
        _editorPriorityComboBox.DataSource = new[]
        {
            new ComboOption<TodoPriority>(TodoPriority.Low, "Basse"),
            new ComboOption<TodoPriority>(TodoPriority.Normal, "Normale"),
            new ComboOption<TodoPriority>(TodoPriority.High, "Haute")
        };
        _editorPriorityComboBox.SelectedIndex = 1;

        _suppressFilterEvents = false;
    }

    private void RegisterEvents()
    {
        Shown += MainForm_Shown;
        FormClosing += MainForm_FormClosing;
        FormClosed += (_, _) =>
        {
            if (!_resourcesDisposed)
            {
                _lifetimeCancellation.Cancel();
            }
        };

        _addButton.Click += (_, _) => BeginAdd();
        _editButton.Click += (_, _) => BeginEditSelected();
        _deleteButton.Click += async (_, _) => await DeleteSelectedAsync();
        _resetFiltersButton.Click += async (_, _) => await ResetFiltersAsync();
        _emptyStateActionButton.Click += EmptyStateActionButton_Click;
        _exitMenuItem.Click += (_, _) => Close();
        _aboutMenuItem.Click += (_, _) =>
        {
            using var aboutBox = new AboutBox();
            aboutBox.ShowDialog(this);
        };

        _searchTextBox.TextChanged += SearchTextBox_TextChanged;
        _searchTimer.Tick += SearchTimer_Tick;
        _statusFilterComboBox.SelectedIndexChanged += FilterComboBox_SelectedIndexChanged;
        _priorityFilterComboBox.SelectedIndexChanged += FilterComboBox_SelectedIndexChanged;

        _todoGrid.SelectionChanged += TodoGrid_SelectionChanged;
        _todoGrid.ColumnHeaderMouseClick += TodoGrid_ColumnHeaderMouseClick;
        _todoGrid.CellDoubleClick += TodoGrid_CellDoubleClick;
        _todoGrid.CellFormatting += TodoGrid_CellFormatting;
        _todoGrid.CellValueChanged += TodoGrid_CellValueChanged;
        _todoGrid.CurrentCellDirtyStateChanged += TodoGrid_CurrentCellDirtyStateChanged;
        _todoGrid.DataError += (_, eventArgs) => eventArgs.ThrowException = false;

        _saveButton.Click += async (_, _) => await SaveEditorAsync();
        _cancelButton.Click += (_, _) => CancelEditor();
        _titleTextBox.TextChanged += EditorValueChanged;
        _notesTextBox.TextChanged += EditorValueChanged;
        _editorPriorityComboBox.SelectedIndexChanged += EditorValueChanged;
        _dueDatePicker.ValueChanged += EditorValueChanged;
        _completedCheckBox.CheckedChanged += EditorValueChanged;
    }

    private async void MainForm_Shown(object? sender, EventArgs eventArgs)
    {
        _isLoaded = true;
        await RefreshAsync();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs eventArgs)
    {
        if (!_isEditing || !_isEditorDirty || _isEditorBusy)
        {
            return;
        }

        var result = MessageBox.Show(
            this,
            "Des changements ne sont pas enregistrés. Fermer quand même ?",
            "Changements non enregistrés",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        eventArgs.Cancel = result != DialogResult.Yes;
    }

    private void SearchTextBox_TextChanged(object? sender, EventArgs eventArgs)
    {
        UpdateResetFiltersVisibility();

        if (!_isLoaded || _isEditing || _suppressFilterEvents)
        {
            return;
        }

        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private async void SearchTimer_Tick(object? sender, EventArgs eventArgs)
    {
        _searchTimer.Stop();
        await RefreshAsync();
    }

    private async void FilterComboBox_SelectedIndexChanged(object? sender, EventArgs eventArgs)
    {
        UpdateResetFiltersVisibility();

        if (!_isLoaded || _isEditing || _suppressFilterEvents)
        {
            return;
        }

        await RefreshAsync();
    }

    private async void EmptyStateActionButton_Click(object? sender, EventArgs eventArgs)
    {
        switch (_emptyStateAction)
        {
            case EmptyStateAction.ResetFilters:
                await ResetFiltersAsync();
                break;
            case EmptyStateAction.Retry:
                await RefreshAsync();
                break;
            default:
                BeginAdd();
                break;
        }
    }

    private async Task RefreshAsync(int? itemToSelect = null)
    {
        if (_isEditing || _resourcesDisposed)
        {
            return;
        }

        _refreshCancellation?.Cancel();
        _refreshCancellation?.Dispose();
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            _lifetimeCancellation.Token);
        _refreshCancellation = cancellation;

        _isLoading = true;
        UpdateCommandState();
        SetBusy(true, "Chargement des tâches…");

        try
        {
            var itemsTask = _todoService.GetAsync(BuildQuery(), cancellation.Token);
            var statisticsTask = _todoService.GetStatisticsAsync(cancellation.Token);

            await Task.WhenAll(itemsTask, statisticsTask);

            if (_resourcesDisposed
                || IsDisposed
                || !ReferenceEquals(_refreshCancellation, cancellation))
            {
                return;
            }

            _loadedItems = (await itemsTask).ToList();
            BindRows(_loadedItems, itemToSelect);
            UpdateSummary(await statisticsTask);
            ShowEmptyStateIfNeeded();
            SetStatus(BuildDisplayedCountText(_loadedItems.Count));
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Impossible de charger les tâches.");
            ShowLoadErrorState();
            SetStatus("Le chargement a échoué.");
        }
        finally
        {
            if (ReferenceEquals(_refreshCancellation, cancellation))
            {
                _isLoading = false;
                SetBusy(false);
                UpdateCommandState();
            }
        }
    }

    private TodoQuery BuildQuery()
    {
        var status = (_statusFilterComboBox.SelectedItem
            as ComboOption<TodoStatusFilter>)?.Value ?? TodoStatusFilter.All;
        var priority = (_priorityFilterComboBox.SelectedItem
            as ComboOption<TodoPriority?>)?.Value;
        return new TodoQuery(
            _searchTextBox.Text,
            status,
            priority,
            _sortField,
            _sortDirection);
    }

    private void BindRows(IReadOnlyCollection<TodoItem> items, int? itemToSelect)
    {
        _isBinding = true;

        try
        {
            var rows = new BindingList<TodoItem>(items.ToList());
            _todoBindingSource.DataSource = rows;

            foreach (DataGridViewRow gridRow in _todoGrid.Rows)
            {
                if (gridRow.DataBoundItem is TodoItem item)
                {
                    gridRow.Cells["CompletedColumn"].Value = item.IsCompleted;
                }
            }

            _todoGrid.ClearSelection();

            if (rows.Count == 0)
            {
                return;
            }

            var rowIndex = itemToSelect is { } selectedId
                ? rows.ToList().FindIndex(row => row.Id == selectedId)
                : 0;
            rowIndex = rowIndex < 0 ? 0 : rowIndex;

            _todoGrid.Rows[rowIndex].Selected = true;
            _todoGrid.CurrentCell = _todoGrid.Rows[rowIndex].Cells["TitleColumn"];
            UpdateSortGlyphs();
        }
        finally
        {
            _isBinding = false;
            UpdateCommandState();
        }
    }

    private void ShowEmptyStateIfNeeded()
    {
        if (_loadedItems.Count > 0)
        {
            _emptyStatePanel.Visible = false;
            _todoGrid.Visible = true;
            return;
        }

        _todoGrid.Visible = false;
        _emptyStatePanel.Visible = true;
        _emptyStatePanel.BringToFront();

        if (HasActiveFilters())
        {
            _emptyStateAction = EmptyStateAction.ResetFilters;
            _emptyStateTitleLabel.Text = "Aucun résultat";
            _emptyStateDescriptionLabel.Text =
                "Aucune tâche ne correspond aux filtres actuels.";
            _emptyStateActionButton.Text = "Réinitialiser les filtres";
        }
        else
        {
            _emptyStateAction = EmptyStateAction.Add;
            _emptyStateTitleLabel.Text = "Votre liste est vide";
            _emptyStateDescriptionLabel.Text =
                "Créez votre première tâche pour commencer.";
            _emptyStateActionButton.Text = "Créer ma première tâche";
        }
    }

    private void ShowLoadErrorState()
    {
        _todoGrid.Visible = false;
        _emptyStatePanel.Visible = true;
        _emptyStatePanel.BringToFront();
        _emptyStateAction = EmptyStateAction.Retry;
        _emptyStateTitleLabel.Text = "Chargement impossible";
        _emptyStateDescriptionLabel.Text =
            "La base de données n’a pas pu être lue. Vous pouvez réessayer.";
        _emptyStateActionButton.Text = "Réessayer";
    }

    private async Task ResetFiltersAsync()
    {
        _suppressFilterEvents = true;
        _searchTimer.Stop();

        try
        {
            _searchTextBox.Clear();
            _statusFilterComboBox.SelectedIndex = 0;
            _priorityFilterComboBox.SelectedIndex = 0;
        }
        finally
        {
            _suppressFilterEvents = false;
            UpdateResetFiltersVisibility();
        }

        await RefreshAsync();
    }

    private void TodoGrid_CurrentCellDirtyStateChanged(object? sender, EventArgs eventArgs)
    {
        if (_todoGrid.IsCurrentCellDirty
            && _todoGrid.CurrentCell is DataGridViewCheckBoxCell)
        {
            _todoGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void TodoGrid_SelectionChanged(object? sender, EventArgs eventArgs)
    {
        if (!_resourcesDisposed)
        {
            UpdateCommandState();
        }
    }

    private async void TodoGrid_ColumnHeaderMouseClick(
        object? sender,
        DataGridViewCellMouseEventArgs eventArgs)
    {
        if (_isEditing
            || _isLoading
            || _isEditorBusy
            || eventArgs.ColumnIndex < 0
            || _todoGrid.Columns[eventArgs.ColumnIndex].Tag
                is not TodoSortField selectedField)
        {
            return;
        }

        var selectedId = SelectedItem?.Id;

        if (_sortField == selectedField)
        {
            _sortDirection = _sortDirection == TodoSortDirection.Ascending
                ? TodoSortDirection.Descending
                : TodoSortDirection.Ascending;
        }
        else
        {
            _sortField = selectedField;
            _sortDirection = TodoSortDirection.Ascending;
        }

        UpdateSortGlyphs();
        await RefreshAsync(selectedId);
    }

    private async void TodoGrid_CellValueChanged(
        object? sender,
        DataGridViewCellEventArgs eventArgs)
    {
        if (_isBinding
            || _isEditing
            || eventArgs.RowIndex < 0
            || eventArgs.ColumnIndex != _todoGrid.Columns["CompletedColumn"]!.Index
            || _todoGrid.Rows[eventArgs.RowIndex].DataBoundItem
                is not TodoItem item)
        {
            return;
        }

        var isCompleted = Convert.ToBoolean(
            _todoGrid.Rows[eventArgs.RowIndex].Cells["CompletedColumn"].Value);

        try
        {
            _isEditorBusy = true;
            UpdateCommandState();
            SetBusy(true, isCompleted
                ? "Marquage comme terminée…"
                : "Réouverture de la tâche…");

            var exists = await _todoService.SetCompletedAsync(
                item.Id,
                isCompleted,
                _lifetimeCancellation.Token);

            if (!exists)
            {
                MessageBox.Show(
                    this,
                    "Cette tâche n’existe plus.",
                    "Tâche introuvable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            _isEditorBusy = false;
            await RefreshAsync(item.Id);
            SetStatus(isCompleted ? "Tâche terminée." : "Tâche rouverte.");
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            ShowOperationError(exception, "La tâche n’a pas pu être mise à jour.");
            _isEditorBusy = false;
            await RefreshAsync(item.Id);
        }
        finally
        {
            _isEditorBusy = false;
            SetBusy(false);
            UpdateCommandState();
        }
    }

    private void TodoGrid_CellDoubleClick(
        object? sender,
        DataGridViewCellEventArgs eventArgs)
    {
        if (eventArgs.RowIndex >= 0
            && eventArgs.ColumnIndex != _todoGrid.Columns["CompletedColumn"]!.Index)
        {
            BeginEditSelected();
        }
    }

    private void TodoGrid_CellFormatting(
        object? sender,
        DataGridViewCellFormattingEventArgs eventArgs)
    {
        if (eventArgs.RowIndex < 0
            || _todoGrid.Rows[eventArgs.RowIndex].DataBoundItem
                is not TodoItem item)
        {
            return;
        }

        var columnName = _todoGrid.Columns[eventArgs.ColumnIndex].Name;

        if (columnName == "PriorityColumn")
        {
            eventArgs.Value = GetPriorityLabel(item.Priority);
            eventArgs.FormattingApplied = true;
        }
        else if (columnName == "DueDateColumn")
        {
            eventArgs.Value = GetDueDateLabel(item.DueDate);
            eventArgs.FormattingApplied = true;
        }
        else if (columnName == "CreatedAtColumn")
        {
            eventArgs.Value = DateTime
                .SpecifyKind(item.CreatedAtUtc, DateTimeKind.Utc)
                .ToLocalTime()
                .ToString("g", CultureInfo.CurrentCulture);
            eventArgs.FormattingApplied = true;
        }

        if (item.IsCompleted)
        {
            eventArgs.CellStyle.ForeColor = UiTheme.TextSecondary;
            if (columnName == "TitleColumn")
            {
                eventArgs.CellStyle.Font = _completedFont;
            }
        }

        if (!item.IsCompleted
            && columnName == "PriorityColumn"
            && item.Priority == TodoPriority.High)
        {
            eventArgs.CellStyle.ForeColor = UiTheme.Danger;
        }

        if (columnName == "DueDateColumn"
            && !item.IsCompleted
            && item.DueDate is { } dueDate
            && dueDate < DateOnly.FromDateTime(DateTime.Today))
        {
            eventArgs.CellStyle.ForeColor = UiTheme.Danger;
        }
    }

    private void BeginAdd()
    {
        if (_isEditing || _isLoading || _isEditorBusy)
        {
            return;
        }

        EnterEditor(item: null);
    }

    private void BeginEditSelected()
    {
        if (_isEditing || _isLoading || _isEditorBusy || SelectedItem is not { } item)
        {
            return;
        }

        EnterEditor(item);
    }

    private void EnterEditor(TodoItem? item)
    {
        _suppressEditorEvents = true;

        try
        {
            _editingId = item?.Id;
            _editorHeadingLabel.Text = item is null
                ? "Nouvelle tâche"
                : "Modifier la tâche";
            _titleTextBox.Text = item?.Title ?? string.Empty;
            _notesTextBox.Text = item?.Notes ?? string.Empty;
            SelectEditorPriority(item?.Priority ?? TodoPriority.Normal);

            var dueDate = item?.DueDate;
            _dueDatePicker.Checked = dueDate.HasValue;
            _dueDatePicker.Value = ToPickerDate(dueDate);
            _completedCheckBox.Checked = item?.IsCompleted ?? false;
            ClearEditorErrors();
        }
        finally
        {
            _suppressEditorEvents = false;
        }

        _isEditing = true;
        _isEditorDirty = false;
        _editorEmptyPanel.Visible = false;
        _editorContentPanel.Visible = true;
        _editorContentPanel.BringToFront();
        _todoGrid.Enabled = false;
        _filterPanel.Enabled = false;
        AcceptButton = _saveButton;
        CancelButton = _cancelButton;
        UpdateCommandState();

        _titleTextBox.Focus();
        _titleTextBox.SelectAll();
    }

    private void ExitEditor()
    {
        _isEditing = false;
        _isEditorDirty = false;
        _editingId = null;
        _editorContentPanel.Visible = false;
        _editorContentPanel.Enabled = true;
        _editorEmptyPanel.Visible = true;
        _editorEmptyPanel.BringToFront();
        _todoGrid.Enabled = true;
        _filterPanel.Enabled = true;
        AcceptButton = null;
        CancelButton = null;
        ClearEditorErrors();
        UpdateCommandState();
        _todoGrid.Focus();
    }

    private void CancelEditor()
    {
        if (!_isEditing || _isEditorBusy)
        {
            return;
        }

        if (_isEditorDirty)
        {
            var result = MessageBox.Show(
                this,
                "Abandonner les changements ?",
                "Annuler la modification",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes)
            {
                return;
            }
        }

        ExitEditor();
    }

    private async Task SaveEditorAsync()
    {
        if (!_isEditing || _isEditorBusy || !TryBuildSaveRequest(out var request))
        {
            return;
        }

        _isEditorBusy = true;
        _editorContentPanel.Enabled = false;
        UpdateCommandState();
        SetBusy(true, "Enregistrement…");

        try
        {
            TodoItem? savedItem;
            var wasCreated = _editingId is null;

            if (_editingId is { } id)
            {
                savedItem = await _todoService.UpdateAsync(
                    id,
                    request,
                    _lifetimeCancellation.Token);
            }
            else
            {
                savedItem = await _todoService.CreateAsync(
                    request,
                    _lifetimeCancellation.Token);
            }

            if (savedItem is null)
            {
                MessageBox.Show(
                    this,
                    "Cette tâche n’existe plus.",
                    "Tâche introuvable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ExitEditor();
                await RefreshAsync();
                return;
            }

            ExitEditor();
            await RefreshAsync(savedItem.Id);
            SetStatus(wasCreated ? "Tâche créée." : "Tâche enregistrée.");
        }
        catch (TodoValidationException exception)
        {
            ApplyServiceValidation(exception);
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            ShowOperationError(exception, "La tâche n’a pas pu être enregistrée.");
        }
        finally
        {
            _isEditorBusy = false;
            if (_isEditing)
            {
                _editorContentPanel.Enabled = true;
            }

            SetBusy(false);
            UpdateCommandState();
        }
    }

    private bool TryBuildSaveRequest(out TodoSaveRequest request)
    {
        ClearEditorErrors();

        var title = _titleTextBox.Text.Trim();
        if (title.Length == 0)
        {
            const string error = "Le titre est obligatoire.";
            _errorProvider.SetError(_titleTextBox, error);
            ShowEditorError(error);
            _titleTextBox.Focus();
            request = default!;
            return false;
        }

        var priority = (_editorPriorityComboBox.SelectedItem
            as ComboOption<TodoPriority>)?.Value ?? TodoPriority.Normal;
        DateOnly? dueDate = _dueDatePicker.Checked
            ? DateOnly.FromDateTime(_dueDatePicker.Value.Date)
            : null;

        request = new TodoSaveRequest(
            title,
            _notesTextBox.Text,
            priority,
            dueDate,
            _completedCheckBox.Checked);
        return true;
    }

    private async Task DeleteSelectedAsync()
    {
        if (_isEditing || _isEditorBusy || SelectedItem is not { } item)
        {
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Supprimer « {item.Title} » ? Cette action est définitive.",
            "Supprimer la tâche",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes)
        {
            return;
        }

        _isEditorBusy = true;
        UpdateCommandState();
        SetBusy(true, "Suppression…");

        try
        {
            await _todoService.DeleteAsync(item.Id, _lifetimeCancellation.Token);
            _isEditorBusy = false;
            await RefreshAsync();
            SetStatus("Tâche supprimée.");
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            ShowOperationError(exception, "La tâche n’a pas pu être supprimée.");
        }
        finally
        {
            _isEditorBusy = false;
            SetBusy(false);
            UpdateCommandState();
        }
    }

    private void EditorValueChanged(object? sender, EventArgs eventArgs)
    {
        if (_isEditing && !_suppressEditorEvents)
        {
            _isEditorDirty = true;
        }
    }

    private void ApplyServiceValidation(TodoValidationException exception)
    {
        if (exception.Errors.TryGetValue(nameof(TodoSaveRequest.Title), out var titleError))
        {
            _errorProvider.SetError(_titleTextBox, titleError);
        }

        if (exception.Errors.TryGetValue(nameof(TodoSaveRequest.Notes), out var notesError))
        {
            _errorProvider.SetError(_notesTextBox, notesError);
        }

        if (exception.Errors.TryGetValue(nameof(TodoSaveRequest.Priority), out var priorityError))
        {
            _errorProvider.SetError(_editorPriorityComboBox, priorityError);
        }

        ShowEditorError(string.Join(Environment.NewLine, exception.Errors.Values));
    }

    private void ClearEditorErrors()
    {
        _errorProvider.Clear();
        _editorErrorLabel.Text = string.Empty;
        _editorErrorLabel.Visible = false;
    }

    private void ShowEditorError(string message)
    {
        _editorErrorLabel.Text = message;
        _editorErrorLabel.Visible = true;
    }

    private void SelectEditorPriority(TodoPriority priority)
    {
        for (var index = 0; index < _editorPriorityComboBox.Items.Count; index++)
        {
            if (_editorPriorityComboBox.Items[index]
                is ComboOption<TodoPriority> option
                && option.Value == priority)
            {
                _editorPriorityComboBox.SelectedIndex = index;
                return;
            }
        }
    }

    private DateTime ToPickerDate(DateOnly? dueDate)
    {
        var value = dueDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today;
        if (value < _dueDatePicker.MinDate)
        {
            return _dueDatePicker.MinDate;
        }

        return value > _dueDatePicker.MaxDate ? _dueDatePicker.MaxDate : value;
    }

    private TodoItem? SelectedItem
    {
        get
        {
            if (_resourcesDisposed
                || IsDisposed
                || Disposing
                || _todoGrid.IsDisposed
                || _todoGrid.Disposing
                || _todoGrid.RowCount == 0
                || _todoGrid.CurrentCell is null)
            {
                return null;
            }

            return _todoGrid.CurrentRow?.DataBoundItem as TodoItem;
        }
    }

    private bool HasActiveFilters()
    {
        var status = (_statusFilterComboBox.SelectedItem
            as ComboOption<TodoStatusFilter>)?.Value ?? TodoStatusFilter.All;
        var priority = (_priorityFilterComboBox.SelectedItem
            as ComboOption<TodoPriority?>)?.Value;
        return !string.IsNullOrWhiteSpace(_searchTextBox.Text)
            || status != TodoStatusFilter.All
            || priority is not null;
    }

    private void UpdateResetFiltersVisibility()
    {
        _resetFiltersButton.Visible = HasActiveFilters();
    }

    private void UpdateSortGlyphs()
    {
        foreach (DataGridViewColumn column in _todoGrid.Columns)
        {
            column.HeaderCell.SortGlyphDirection =
                column.Tag is TodoSortField field && field == _sortField
                    ? _sortDirection == TodoSortDirection.Ascending
                        ? SortOrder.Ascending
                        : SortOrder.Descending
                    : SortOrder.None;
        }
    }

    private void UpdateCommandState()
    {
        if (_resourcesDisposed || IsDisposed || Disposing)
        {
            return;
        }

        var hasSelection = SelectedItem is not null;
        var canUseList = !_isEditing && !_isLoading && !_isEditorBusy;

        _addButton.Enabled = canUseList;
        _editButton.Enabled = canUseList && hasSelection;
        _deleteButton.Enabled = canUseList && hasSelection;
        _saveButton.Enabled = _isEditing && !_isEditorBusy;
        _cancelButton.Enabled = _isEditing && !_isEditorBusy;

        if (!_isEditing)
        {
            _todoGrid.Enabled = !_isLoading && !_isEditorBusy;
        }
    }

    private void UpdateSummary(TodoStatistics statistics)
    {
        if (statistics.Total == 0)
        {
            _summaryLabel.Text = "Aucune tâche pour le moment";
            return;
        }

        var summary =
            $"{statistics.Active} à faire · {statistics.Completed} terminée{(statistics.Completed > 1 ? "s" : string.Empty)}";

        if (statistics.Overdue > 0)
        {
            summary += $" · {statistics.Overdue} en retard";
        }

        _summaryLabel.Text = summary;
    }

    private void SetBusy(bool isBusy, string? message = null)
    {
        if (_resourcesDisposed || IsDisposed || Disposing)
        {
            return;
        }

        _progressBar.Visible = isBusy;
        UseWaitCursor = isBusy;

        if (!string.IsNullOrWhiteSpace(message))
        {
            _statusLabel.Text = message;
        }
    }

    private void SetStatus(string message)
    {
        if (_resourcesDisposed || IsDisposed || Disposing)
        {
            return;
        }

        _statusLabel.Text = message;
    }

    private void ShowOperationError(Exception exception, string userMessage)
    {
        _logger.LogError(exception, "{UserMessage}", userMessage);
        SetStatus(userMessage);
        MessageBox.Show(
            this,
            $"{userMessage}\n\n{exception.Message}",
            "Une erreur est survenue",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static string BuildDisplayedCountText(int count) =>
        count switch
        {
            0 => "Aucune tâche affichée",
            1 => "1 tâche affichée",
            _ => $"{count} tâches affichées"
        };

    private static string GetPriorityLabel(TodoPriority priority) => priority switch
    {
        TodoPriority.High => "Haute",
        TodoPriority.Low => "Basse",
        _ => "Normale"
    };

    private static string GetDueDateLabel(DateOnly? dueDate)
    {
        if (dueDate is null)
        {
            return "Aucune";
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (dueDate == today)
        {
            return "Aujourd’hui";
        }

        if (dueDate == today.AddDays(1))
        {
            return "Demain";
        }

        return dueDate.Value.ToString("d", CultureInfo.CurrentCulture);
    }

    private sealed record ComboOption<T>(T Value, string Text);

    private enum EmptyStateAction
    {
        Add,
        ResetFilters,
        Retry
    }
}
