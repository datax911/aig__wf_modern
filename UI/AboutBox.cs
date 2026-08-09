using System.Reflection;

namespace ModernTodo.UI;

public sealed class AboutBox : Form
{
    private readonly Font _baseFont = new(
        "Segoe UI",
        9.5F,
        FontStyle.Regular,
        GraphicsUnit.Point);

    private readonly Font _titleFont = new(
        "Segoe UI",
        20F,
        FontStyle.Bold,
        GraphicsUnit.Point);

    private readonly Font _technologyFont = new(
        "Segoe UI",
        10F,
        FontStyle.Bold,
        GraphicsUnit.Point);

    public AboutBox()
    {
        InitializeComponent();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _baseFont.Dispose();
            _titleFont.Dispose();
            _technologyFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        Text = "À propos de Modern Todo";
        AccessibleDescription = "Informations sur l'application Modern Todo.";
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = UiTheme.Surface;
        ClientSize = new Size(540, 320);
        Font = _baseFont;
        ForeColor = UiTheme.TextPrimary;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        var rootLayout = new TableLayoutPanel
        {
            BackColor = UiTheme.Surface,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(30, 24, 30, 20),
            RowCount = 6
        };
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleLabel = new Label
        {
            AutoSize = true,
            Font = _titleFont,
            ForeColor = UiTheme.TextPrimary,
            Margin = new Padding(0),
            Text = "Modern Todo"
        };

        var versionLabel = new Label
        {
            AutoSize = true,
            ForeColor = UiTheme.TextSecondary,
            Margin = new Padding(2, 4, 0, 0),
            Text = $"Version {GetAssemblyVersion()}"
        };

        var technologyLabel = new Label
        {
            AutoSize = true,
            Font = _technologyFont,
            ForeColor = UiTheme.Accent,
            Margin = new Padding(2, 18, 0, 0),
            Text = ".NET 10  ·  WinForms  ·  EF Core  ·  SQLite"
        };

        var divider = new Panel
        {
            BackColor = UiTheme.Border,
            Dock = DockStyle.Bottom,
            Height = 1,
            Margin = new Padding(0, 12, 0, 12)
        };

        var descriptionLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = UiTheme.TextSecondary,
            Margin = new Padding(2, 5, 0, 12),
            Text = "Une liste de tâches bâtie avec une architecture pragmatique : " +
                   "une couche de services concrète, EF Core sans repository et " +
                   "l'hébergement générique de .NET pour la configuration et l'injection de dépendances."
        };

        var buttonPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0),
            WrapContents = false
        };

        var okButton = new Button
        {
            AccessibleDescription = "Ferme la boîte À propos.",
            DialogResult = DialogResult.OK,
            Margin = new Padding(0),
            Text = "OK"
        };
        UiTheme.StyleButton(okButton, ButtonKind.Primary);
        buttonPanel.Controls.Add(okButton);

        rootLayout.Controls.Add(titleLabel, 0, 0);
        rootLayout.Controls.Add(versionLabel, 0, 1);
        rootLayout.Controls.Add(technologyLabel, 0, 2);
        rootLayout.Controls.Add(divider, 0, 3);
        rootLayout.Controls.Add(descriptionLabel, 0, 4);
        rootLayout.Controls.Add(buttonPanel, 0, 5);

        AcceptButton = okButton;
        CancelButton = okButton;
        Controls.Add(rootLayout);

        ResumeLayout(performLayout: true);
    }

    private static string GetAssemblyVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(AboutBox).Assembly;
        return assembly.GetName().Version?.ToString() ?? "inconnue";
    }
}
