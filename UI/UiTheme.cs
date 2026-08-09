namespace ModernTodo.UI;

internal enum ButtonKind
{
    Primary,
    Secondary,
    Danger,
    Subtle
}

internal static class UiTheme
{
    public static Color WindowBackground => Pick(Color.FromArgb(245, 247, 250), SystemColors.AppWorkspace);
    public static Color Surface => Pick(Color.White, SystemColors.Window);
    public static Color TextPrimary => Pick(Color.FromArgb(17, 24, 39), SystemColors.WindowText);
    public static Color TextSecondary => Pick(Color.FromArgb(107, 114, 128), SystemColors.GrayText);
    public static Color Border => Pick(Color.FromArgb(229, 231, 235), SystemColors.ControlDark);
    public static Color Accent => Pick(Color.FromArgb(37, 99, 235), SystemColors.Highlight);
    public static Color AccentHover => Pick(Color.FromArgb(29, 78, 216), SystemColors.HotTrack);
    public static Color Danger => Pick(Color.FromArgb(220, 38, 38), Color.Red);
    public static Color DangerSurface => Pick(Color.FromArgb(254, 242, 242), SystemColors.Control);
    public static Color Selection => Pick(Color.FromArgb(239, 246, 255), SystemColors.Highlight);
    public static Color SelectionText => Pick(Color.FromArgb(30, 64, 175), SystemColors.HighlightText);
    public static Color AlternatingRow => Pick(Color.FromArgb(249, 250, 251), SystemColors.ControlLight);
    public static Color Warning => Pick(Color.FromArgb(180, 83, 9), Color.DarkOrange);

    public static void StyleButton(Button button, ButtonKind kind)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.Cursor = Cursors.Hand;
        button.FlatStyle = FlatStyle.Flat;
        button.MinimumSize = new Size(0, 38);
        button.Padding = new Padding(14, 4, 14, 4);
        button.UseVisualStyleBackColor = false;

        switch (kind)
        {
            case ButtonKind.Primary:
                button.BackColor = Accent;
                button.ForeColor = SystemInformation.HighContrast
                    ? SystemColors.HighlightText
                    : Color.White;
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor = AccentHover;
                break;

            case ButtonKind.Danger:
                button.BackColor = Surface;
                button.ForeColor = Danger;
                button.FlatAppearance.BorderColor = Danger;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.MouseOverBackColor = DangerSurface;
                break;

            case ButtonKind.Subtle:
                button.BackColor = Surface;
                button.ForeColor = TextSecondary;
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor = WindowBackground;
                break;

            default:
                button.BackColor = Surface;
                button.ForeColor = TextPrimary;
                button.FlatAppearance.BorderColor = Border;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.MouseOverBackColor = WindowBackground;
                break;
        }
    }

    private static Color Pick(Color regular, Color highContrast) =>
        SystemInformation.HighContrast ? highContrast : regular;
}
