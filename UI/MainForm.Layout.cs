using System.ComponentModel;
using ModernTodo.Domain;
using ModernTodo.Services;

namespace ModernTodo.UI;

public sealed partial class MainForm
{
    private readonly BindingSource _todoBindingSource = new();
    private readonly System.Windows.Forms.Timer _searchTimer = new() { Interval = 300 };
    private readonly ErrorProvider _errorProvider = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };
    private readonly ToolTip _toolTip = new();

    private readonly Font _baseFont = new("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
    private readonly Font _headingFont = new("Segoe UI", 22F, FontStyle.Bold, GraphicsUnit.Point);
    private readonly Font _sectionFont = new("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point);
    private readonly Font _completedFont = new("Segoe UI", 9.5F, FontStyle.Strikeout, GraphicsUnit.Point);
    private readonly Font _gridHeaderFont = new("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point);

    private readonly MenuStrip _mainMenuStrip = new();
    private readonly ToolStripMenuItem _exitMenuItem = new();
    private readonly ToolStripMenuItem _aboutMenuItem = new();
    private readonly Label _summaryLabel = new();
    private readonly Button _addButton = new();
    private readonly Button _editButton = new();
    private readonly Button _deleteButton = new();
    private readonly FlowLayoutPanel _filterPanel = new();
    private readonly TextBox _searchTextBox = new();
    private readonly ComboBox _statusFilterComboBox = new();
    private readonly ComboBox _priorityFilterComboBox = new();
    private readonly Button _resetFiltersButton = new();

    private readonly DataGridView _todoGrid = new();
    private readonly Panel _emptyStatePanel = new();
    private readonly Label _emptyStateTitleLabel = new();
    private readonly Label _emptyStateDescriptionLabel = new();
    private readonly Button _emptyStateActionButton = new();

    private readonly Panel _editorEmptyPanel = new();
    private readonly Panel _editorContentPanel = new();
    private readonly Label _editorHeadingLabel = new();
    private readonly Label _editorHintLabel = new();
    private readonly TextBox _titleTextBox = new();
    private readonly TextBox _notesTextBox = new();
    private readonly ComboBox _editorPriorityComboBox = new();
    private readonly DateTimePicker _dueDatePicker = new();
    private readonly CheckBox _completedCheckBox = new();
    private readonly Label _editorErrorLabel = new();
    private readonly Button _saveButton = new();
    private readonly Button _cancelButton = new();

    private readonly StatusStrip _statusStrip = new();
    private readonly ToolStripStatusLabel _statusLabel = new();
    private readonly ToolStripProgressBar _progressBar = new();

    private void InitializeComponent()
    {
        SuspendLayout();

        Text = "Modern Todo";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(960, 640);
        ClientSize = new Size(1_220, 780);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = UiTheme.WindowBackground;
        ForeColor = UiTheme.TextPrimary;
        Font = _baseFont;
        KeyPreview = true;

        _errorProvider.ContainerControl = this;

        var rootLayout = new TableLayoutPanel
        {
            BackColor = UiTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 16, 24, 14),
            RowCount = 4
        };
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

        rootLayout.Controls.Add(CreateHeader(), 0, 0);
        rootLayout.Controls.Add(CreateFilterBar(), 0, 1);
        rootLayout.Controls.Add(CreateContent(), 0, 2);
        rootLayout.Controls.Add(CreateStatusBar(), 0, 3);

        var shellLayout = new TableLayoutPanel
        {
            BackColor = UiTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 2
        };
        shellLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        shellLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shellLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        shellLayout.Controls.Add(CreateMainMenu(), 0, 0);
        shellLayout.Controls.Add(rootLayout, 0, 1);

        Controls.Add(shellLayout);
        MainMenuStrip = _mainMenuStrip;

        _toolTip.SetToolTip(_addButton, "Créer une tâche (Ctrl+N)");
        _toolTip.SetToolTip(_editButton, "Modifier la tâche sélectionnée (Entrée)");
        _toolTip.SetToolTip(_deleteButton, "Supprimer la tâche sélectionnée (Suppr)");
        _toolTip.SetToolTip(_saveButton, "Enregistrer les changements (Ctrl+S)");

        ResumeLayout(performLayout: true);
    }

    private Control CreateMainMenu()
    {
        _mainMenuStrip.AccessibleName = "Menu principal";
        _mainMenuStrip.BackColor = UiTheme.Surface;
        _mainMenuStrip.Dock = DockStyle.Fill;
        _mainMenuStrip.Margin = Padding.Empty;
        _mainMenuStrip.RenderMode = ToolStripRenderMode.System;

        var fileMenuItem = new ToolStripMenuItem("&Fichier");
        _exitMenuItem.Text = "&Quitter";
        _exitMenuItem.ShortcutKeyDisplayString = "Alt+F4";
        fileMenuItem.DropDownItems.Add(_exitMenuItem);

        var helpMenuItem = new ToolStripMenuItem("&Aide");
        _aboutMenuItem.Text = "À &propos…";
        helpMenuItem.DropDownItems.Add(_aboutMenuItem);

        _mainMenuStrip.Items.Add(fileMenuItem);
        _mainMenuStrip.Items.Add(helpMenuItem);
        return _mainMenuStrip;
    }

    private Control CreateHeader()
    {
        var header = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 1
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titleStack = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 2
        };

        var titleLabel = new Label
        {
            AutoSize = true,
            Font = _headingFont,
            ForeColor = UiTheme.TextPrimary,
            Margin = Padding.Empty,
            Text = "Mes tâches"
        };

        _summaryLabel.AutoSize = true;
        _summaryLabel.ForeColor = UiTheme.TextSecondary;
        _summaryLabel.Margin = new Padding(2, 2, 0, 0);
        _summaryLabel.Text = "Chargement…";

        titleStack.Controls.Add(titleLabel, 0, 0);
        titleStack.Controls.Add(_summaryLabel, 0, 1);

        var commands = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = Padding.Empty,
            Padding = new Padding(0, 8, 0, 0),
            WrapContents = false
        };

        _addButton.Text = "&Nouvelle tâche";
        _addButton.AccessibleName = "Nouvelle tâche";
        UiTheme.StyleButton(_addButton, ButtonKind.Primary);

        _editButton.Text = "&Modifier";
        _editButton.AccessibleName = "Modifier la tâche sélectionnée";
        UiTheme.StyleButton(_editButton, ButtonKind.Secondary);

        _deleteButton.Text = "&Supprimer";
        _deleteButton.AccessibleName = "Supprimer la tâche sélectionnée";
        UiTheme.StyleButton(_deleteButton, ButtonKind.Danger);

        commands.Controls.Add(_addButton);
        commands.Controls.Add(_deleteButton);
        commands.Controls.Add(_editButton);

        header.Controls.Add(titleStack, 0, 0);
        header.Controls.Add(commands, 1, 0);
        return header;
    }

    private Control CreateFilterBar()
    {
        _filterPanel.Dock = DockStyle.Fill;
        _filterPanel.FlowDirection = FlowDirection.LeftToRight;
        _filterPanel.Margin = Padding.Empty;
        _filterPanel.Padding = new Padding(0, 9, 0, 9);
        _filterPanel.WrapContents = false;
        _filterPanel.AutoScroll = true;

        _searchTextBox.AccessibleName = "Rechercher dans les tâches";
        _searchTextBox.AutoSize = false;
        _searchTextBox.BorderStyle = BorderStyle.FixedSingle;
        _searchTextBox.Height = 36;
        _searchTextBox.Margin = new Padding(0, 2, 12, 0);
        _searchTextBox.PlaceholderText = "Rechercher une tâche…";
        _searchTextBox.Width = 260;

        ConfigureFilterComboBox(_statusFilterComboBox, "Filtrer par état", 130);
        ConfigureFilterComboBox(_priorityFilterComboBox, "Filtrer par priorité", 130);
        _resetFiltersButton.Text = "Réinitialiser";
        _resetFiltersButton.Visible = false;
        UiTheme.StyleButton(_resetFiltersButton, ButtonKind.Subtle);

        _filterPanel.Controls.Add(_searchTextBox);
        _filterPanel.Controls.Add(CreateFilterLabel("État"));
        _filterPanel.Controls.Add(_statusFilterComboBox);
        _filterPanel.Controls.Add(CreateFilterLabel("Priorité"));
        _filterPanel.Controls.Add(_priorityFilterComboBox);
        _filterPanel.Controls.Add(_resetFiltersButton);
        return _filterPanel;
    }

    private Control CreateContent()
    {
        var splitContainer = new SplitContainer
        {
            BackColor = UiTheme.WindowBackground,
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2,
            IsSplitterFixed = false,
            Margin = Padding.Empty,
            Size = new Size(1_172, 600),
            SplitterDistance = 780,
            Panel1MinSize = 500,
            Panel2MinSize = 330,
            SplitterWidth = 10
        };

        splitContainer.Panel1.BackColor = UiTheme.Surface;
        splitContainer.Panel1.Padding = new Padding(1);
        splitContainer.Panel2.BackColor = UiTheme.Surface;

        splitContainer.Panel1.Controls.Add(CreateGridSurface());
        splitContainer.Panel2.Controls.Add(CreateEditorSurface());
        return splitContainer;
    }

    private Control CreateGridSurface()
    {
        var gridSurface = new Panel
        {
            BackColor = UiTheme.Border,
            Dock = DockStyle.Fill,
            Padding = new Padding(1)
        };

        ConfigureGrid();
        ConfigureEmptyState();

        gridSurface.Controls.Add(_todoGrid);
        gridSurface.Controls.Add(_emptyStatePanel);
        return gridSurface;
    }

    private void ConfigureGrid()
    {
        _todoGrid.AccessibleName = "Liste des tâches";
        _todoGrid.AllowUserToAddRows = false;
        _todoGrid.AllowUserToDeleteRows = false;
        _todoGrid.AllowUserToResizeRows = false;
        _todoGrid.AlternatingRowsDefaultCellStyle.BackColor = UiTheme.AlternatingRow;
        _todoGrid.AutoGenerateColumns = false;
        _todoGrid.BackgroundColor = UiTheme.Surface;
        _todoGrid.BorderStyle = BorderStyle.None;
        _todoGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _todoGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        _todoGrid.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.Surface;
        _todoGrid.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.TextSecondary;
        _todoGrid.ColumnHeadersDefaultCellStyle.Font = _gridHeaderFont;
        _todoGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        _todoGrid.ColumnHeadersHeight = 42;
        _todoGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _todoGrid.DefaultCellStyle.BackColor = UiTheme.Surface;
        _todoGrid.DefaultCellStyle.ForeColor = UiTheme.TextPrimary;
        _todoGrid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        _todoGrid.DefaultCellStyle.SelectionBackColor = UiTheme.Selection;
        _todoGrid.DefaultCellStyle.SelectionForeColor = UiTheme.SelectionText;
        _todoGrid.Dock = DockStyle.Fill;
        _todoGrid.EditMode = DataGridViewEditMode.EditOnEnter;
        _todoGrid.EnableHeadersVisualStyles = false;
        _todoGrid.GridColor = UiTheme.Border;
        _todoGrid.MultiSelect = false;
        _todoGrid.ReadOnly = false;
        _todoGrid.RowHeadersVisible = false;
        _todoGrid.RowTemplate.Height = 44;
        _todoGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        var completedColumn = new DataGridViewCheckBoxColumn
        {
            FlatStyle = FlatStyle.Standard,
            HeaderText = "Fait",
            Name = "CompletedColumn",
            ReadOnly = false,
            SortMode = DataGridViewColumnSortMode.Programmatic,
            Tag = TodoSortField.IsCompleted,
            Width = 64
        };

        var titleColumn = new DataGridViewTextBoxColumn
        {
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            DataPropertyName = nameof(TodoItem.Title),
            HeaderText = "Tâche",
            MinimumWidth = 220,
            Name = "TitleColumn",
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.Programmatic,
            Tag = TodoSortField.Title
        };

        var priorityColumn = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(TodoItem.Priority),
            HeaderText = "Priorité",
            Name = "PriorityColumn",
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.Programmatic,
            Tag = TodoSortField.Priority,
            Width = 105
        };

        var dueDateColumn = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(TodoItem.DueDate),
            HeaderText = "Échéance",
            Name = "DueDateColumn",
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.Programmatic,
            Tag = TodoSortField.DueDate,
            Width = 125
        };

        var createdAtColumn = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(TodoItem.CreatedAtUtc),
            HeaderText = "Créée",
            Name = "CreatedAtColumn",
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.Programmatic,
            Tag = TodoSortField.CreatedAt,
            Width = 135
        };

        _todoGrid.Columns.AddRange(
            completedColumn,
            titleColumn,
            priorityColumn,
            dueDateColumn,
            createdAtColumn);
        _todoGrid.DataSource = _todoBindingSource;
    }

    private void ConfigureEmptyState()
    {
        _emptyStatePanel.BackColor = UiTheme.Surface;
        _emptyStatePanel.Dock = DockStyle.Fill;
        _emptyStatePanel.Visible = false;

        var centeringLayout = new TableLayoutPanel
        {
            ColumnCount = 3,
            Dock = DockStyle.Fill,
            RowCount = 3
        };
        centeringLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        centeringLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        centeringLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        centeringLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
        centeringLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
        centeringLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));

        var stack = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        _emptyStateTitleLabel.AutoSize = true;
        _emptyStateTitleLabel.Font = _sectionFont;
        _emptyStateTitleLabel.ForeColor = UiTheme.TextPrimary;
        _emptyStateTitleLabel.Margin = new Padding(0, 0, 0, 8);
        _emptyStateTitleLabel.TextAlign = ContentAlignment.MiddleCenter;

        _emptyStateDescriptionLabel.AutoSize = true;
        _emptyStateDescriptionLabel.ForeColor = UiTheme.TextSecondary;
        _emptyStateDescriptionLabel.Margin = new Padding(0, 0, 0, 16);
        _emptyStateDescriptionLabel.MaximumSize = new Size(420, 0);
        _emptyStateDescriptionLabel.TextAlign = ContentAlignment.MiddleCenter;

        UiTheme.StyleButton(_emptyStateActionButton, ButtonKind.Primary);

        stack.Controls.Add(_emptyStateTitleLabel);
        stack.Controls.Add(_emptyStateDescriptionLabel);
        stack.Controls.Add(_emptyStateActionButton);
        centeringLayout.Controls.Add(stack, 1, 1);
        _emptyStatePanel.Controls.Add(centeringLayout);
    }

    private Control CreateEditorSurface()
    {
        var surface = new Panel
        {
            BackColor = UiTheme.Surface,
            Dock = DockStyle.Fill,
            Padding = new Padding(24)
        };

        ConfigureEditorEmptyState();
        ConfigureEditor();

        surface.Controls.Add(_editorContentPanel);
        surface.Controls.Add(_editorEmptyPanel);
        _editorEmptyPanel.BringToFront();
        return surface;
    }

    private void ConfigureEditorEmptyState()
    {
        _editorEmptyPanel.BackColor = UiTheme.Surface;
        _editorEmptyPanel.Dock = DockStyle.Fill;

        var layout = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));

        var message = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = UiTheme.TextSecondary,
            MaximumSize = new Size(300, 0),
            Text = "Sélectionnez une tâche puis cliquez sur Modifier,\nou créez-en une nouvelle.",
            TextAlign = ContentAlignment.MiddleCenter
        };

        layout.Controls.Add(message, 0, 1);
        _editorEmptyPanel.Controls.Add(layout);
    }

    private void ConfigureEditor()
    {
        _editorContentPanel.BackColor = UiTheme.Surface;
        _editorContentPanel.Dock = DockStyle.Fill;
        _editorContentPanel.Visible = false;

        var layout = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 13
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _editorHeadingLabel.AutoSize = true;
        _editorHeadingLabel.Font = _sectionFont;
        _editorHeadingLabel.ForeColor = UiTheme.TextPrimary;
        _editorHeadingLabel.Margin = Padding.Empty;

        _editorHintLabel.AutoSize = true;
        _editorHintLabel.ForeColor = UiTheme.TextSecondary;
        _editorHintLabel.Margin = new Padding(0, 4, 0, 0);
        _editorHintLabel.Text = "Les changements sont enregistrés dans SQLite.";

        _titleTextBox.AccessibleName = "Titre de la tâche";
        _titleTextBox.AutoSize = false;
        _titleTextBox.BorderStyle = BorderStyle.FixedSingle;
        _titleTextBox.Dock = DockStyle.Fill;
        _titleTextBox.MaxLength = 160;
        _titleTextBox.PlaceholderText = "Ex. Préparer la présentation";

        _notesTextBox.AcceptsReturn = true;
        _notesTextBox.AccessibleName = "Notes de la tâche";
        _notesTextBox.BorderStyle = BorderStyle.FixedSingle;
        _notesTextBox.Dock = DockStyle.Fill;
        _notesTextBox.MaxLength = 2_000;
        _notesTextBox.Multiline = true;
        _notesTextBox.PlaceholderText = "Ajouter des détails utiles…";
        _notesTextBox.ScrollBars = ScrollBars.Vertical;

        var metadataLayout = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 1
        };
        metadataLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        metadataLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        _editorPriorityComboBox.AccessibleName = "Priorité de la tâche";
        _editorPriorityComboBox.Dock = DockStyle.Top;
        _editorPriorityComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _editorPriorityComboBox.Margin = new Padding(0, 4, 8, 0);

        _dueDatePicker.AccessibleName = "Échéance facultative de la tâche";
        _dueDatePicker.Dock = DockStyle.Top;
        _dueDatePicker.Format = DateTimePickerFormat.Short;
        _dueDatePicker.Margin = new Padding(8, 4, 0, 0);
        _dueDatePicker.ShowCheckBox = true;

        metadataLayout.Controls.Add(
            CreateEditorField("Priorité", _editorPriorityComboBox, new Padding(0, 0, 8, 0)),
            0,
            0);
        metadataLayout.Controls.Add(
            CreateEditorField("Échéance", _dueDatePicker, new Padding(8, 0, 0, 0)),
            1,
            0);

        _completedCheckBox.AccessibleName = "Tâche terminée";
        _completedCheckBox.AutoSize = true;
        _completedCheckBox.Margin = new Padding(0, 14, 0, 8);
        _completedCheckBox.Text = "Cette tâche est terminée";

        _editorErrorLabel.AutoSize = true;
        _editorErrorLabel.Dock = DockStyle.Fill;
        _editorErrorLabel.ForeColor = UiTheme.Danger;
        _editorErrorLabel.Margin = new Padding(0, 4, 0, 8);
        _editorErrorLabel.Visible = false;

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = Padding.Empty,
            WrapContents = false
        };

        _saveButton.Text = "&Enregistrer";
        UiTheme.StyleButton(_saveButton, ButtonKind.Primary);

        _cancelButton.Text = "&Annuler";
        UiTheme.StyleButton(_cancelButton, ButtonKind.Secondary);

        buttons.Controls.Add(_saveButton);
        buttons.Controls.Add(_cancelButton);

        layout.Controls.Add(_editorHeadingLabel, 0, 0);
        layout.Controls.Add(_editorHintLabel, 0, 1);
        layout.Controls.Add(CreateFieldLabel("Titre *"), 0, 3);
        layout.Controls.Add(_titleTextBox, 0, 4);
        layout.Controls.Add(CreateFieldLabel("Notes"), 0, 6);
        layout.Controls.Add(_notesTextBox, 0, 7);
        layout.Controls.Add(metadataLayout, 0, 9);
        layout.Controls.Add(_completedCheckBox, 0, 10);
        layout.Controls.Add(_editorErrorLabel, 0, 11);
        layout.Controls.Add(buttons, 0, 12);

        _editorContentPanel.Controls.Add(layout);
    }

    private Control CreateStatusBar()
    {
        _statusStrip.BackColor = UiTheme.WindowBackground;
        _statusStrip.Dock = DockStyle.Fill;
        _statusStrip.GripStyle = ToolStripGripStyle.Hidden;
        _statusStrip.Margin = Padding.Empty;
        _statusStrip.Padding = new Padding(0, 4, 0, 0);
        _statusStrip.SizingGrip = false;

        _statusLabel.ForeColor = UiTheme.TextSecondary;
        _statusLabel.Spring = true;
        _statusLabel.Text = "Prêt";
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;

        _progressBar.Alignment = ToolStripItemAlignment.Right;
        _progressBar.MarqueeAnimationSpeed = 25;
        _progressBar.Size = new Size(100, 14);
        _progressBar.Style = ProgressBarStyle.Marquee;
        _progressBar.Visible = false;

        _statusStrip.Items.Add(_statusLabel);
        _statusStrip.Items.Add(_progressBar);
        return _statusStrip;
    }

    private static Label CreateFilterLabel(string text) => new()
    {
        AutoSize = true,
        ForeColor = UiTheme.TextSecondary,
        Margin = new Padding(0, 10, 6, 0),
        Text = text
    };

    private static Label CreateFieldLabel(string text) => new()
    {
        AutoSize = true,
        ForeColor = UiTheme.TextPrimary,
        Margin = new Padding(0, 0, 0, 5),
        Text = text
    };

    private static Control CreateEditorField(
        string labelText,
        Control control,
        Padding margin)
    {
        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = margin,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        layout.Controls.Add(CreateFieldLabel(labelText), 0, 0);
        layout.Controls.Add(control, 0, 1);
        return layout;
    }

    private static void ConfigureFilterComboBox(
        ComboBox comboBox,
        string accessibleName,
        int width)
    {
        comboBox.AccessibleName = accessibleName;
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Margin = new Padding(0, 3, 12, 0);
        comboBox.Width = width;
    }
}
