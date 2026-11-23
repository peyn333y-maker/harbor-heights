using Godot;

public partial class HarborHud : CanvasLayer
{
    private const string Ink = "10242c";
    private const string Cream = "fcf3dc";
    private const string Gold = "f2be72";
    private static readonly Color Thin = new("adcac6");
    private Control _screen = null!;
    private Panel _status = null!;
    private VBoxContainer _statusText = null!;
    private Label _district = null!;
    private Label _objective = null!;
    private Label _clock = null!;
    private Label _hint = null!;
    private Label _prompt = null!;
    private Label _toast = null!;
    private Label _drive = null!;
    private Label _navStats = null!;
    private Needle _needle = null!;
    private Panel _nav = null!;
    private ColorRect _wash = null!;
    private ColorRect _scrim = null!;
    private Panel _menu = null!;
    private VBoxContainer _menuStack = null!;
    private Label _heading = null!;
    private Label _subheading = null!;
    private Button _primary = null!;
    private Button _secondary = null!;
    private Button _credits = null!;
    private Button _extraButton = null!;
    private Label _crosshair = null!;
    private Panel _beach = null!;
    private Label _activity = null!;
    private ProgressBar _energy = null!;
    private Action? _action;
    private Action? _alternate;
    private Action? _extraAction;
    private Action? _leaveCredits;
    private readonly SystemFont _display = new() { FontNames = new[] { "Bahnschrift", "Segoe UI" }, FontWeight = 600 };

    public void Build()
    {
        _screen = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_screen);
        _screen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _wash = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore };
        _screen.AddChild(_wash);
        _wash.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _wash.Color = new Color(0, 0, 0, 0);

        _status = Card(new Color("10242cde"));
        Put(_screen, _status, 0, 0, 30, 26, 440, 101);
        _statusText = Stack(_status, 13, 12, 14, 12, 4);
        _district = Text(_statusText, "SALT BEACH   /   HARBOR HEIGHTS", 15, Gold);
        _objective = Text(_statusText, "Отдохните на пляже", 23, Cream);
        _clock = Text(_statusText, "", 15, "bcd0ca");

        _hint = Text(_screen, "WASD  Движение   ·   Мышь  Обзор   ·   Shift  Бег", 15, "e8ead4");
        Put(_screen, _hint, 1, 1, -596, -52, 568, 30);
        _hint.HorizontalAlignment = HorizontalAlignment.Right;
        Shadow(_hint);
        _crosshair = Text(_screen, "·", 23, Cream);
        Put(_screen, _crosshair, .5f, .5f, -8, -16, 16, 30);
        _crosshair.HorizontalAlignment = HorizontalAlignment.Center;
        Shadow(_crosshair);

        _prompt = Text(_screen, "", 23, Cream);
        Put(_screen, _prompt, .5f, .5f, -375, 35, 750, 46);
        _prompt.HorizontalAlignment = HorizontalAlignment.Center;
        Shadow(_prompt);

        _toast = Text(_screen, "", 20, Cream);
        Put(_screen, _toast, .5f, 1, -420, -127, 840, 38);
        _toast.HorizontalAlignment = HorizontalAlignment.Center;
        Shadow(_toast);
        _drive = Text(_screen, "", 25, Cream);
        Put(_screen, _drive, 0, 1, 30, -64, 268, 48);
        Shadow(_drive);

        _nav = Card(new Color("12272dde"));
        Put(_screen, _nav, 1, 0, -264, 26, 235, 91);
        _needle = new Needle { MouseFilter = Control.MouseFilterEnum.Ignore };
        Put(_nav, _needle, 0, 0, 10, 9, 64, 64);
        _navStats = Text(_nav, "", 19, Cream);
        Put(_nav, _navStats, 0, 0, 75, 10, 154, 67);

        _beach = Card(new Color("10242ce3"));
        Put(_screen, _beach, 0, 1, 29, -96, 375, 61);
        _activity = Text(_beach, "", 15, Cream);
        Put(_beach, _activity, 0, 0, 12, 7, 350, 24);
        _energy = new ProgressBar { MaxValue=1, ShowPercentage=false, MouseFilter = Control.MouseFilterEnum.Ignore };
        _energy.AddThemeStyleboxOverride("background",Frame(new Color("30494e"),3));
        _energy.AddThemeStyleboxOverride("fill",Frame(new Color("e5b16e"),3));
        Put(_beach, _energy, 0, 0, 13, 37, 114, 6);
        var energy = Text(_beach, "ВЫНОСЛИВОСТЬ", 11, "bcd0ca");
        Put(_beach, energy, 0, 0, 139, 28, 170, 23);
        _beach.Hide();

        _scrim = new ColorRect { Color = new Color("10242c55"), MouseFilter = Control.MouseFilterEnum.Stop };
        _screen.AddChild(_scrim);
        _scrim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _menu = Card(new Color("10242cf2"));
        Put(_scrim, _menu, 0, .5f, 46, -214, 515, 460);
        _menuStack = Stack(_menu, 28, 24, 28, 24, 14);
        var kicker = Text(_menuStack, "H A R B O R   H E I G H T S", 17, Gold);
        kicker.AddThemeConstantOverride("line_spacing", 0);
        _heading = Text(_menuStack, "", 61, Cream);
        _heading.AddThemeFontOverride("font", _display);
        _subheading = Text(_menuStack, "", 20, "bcd0ca");
        _subheading.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _subheading.CustomMinimumSize = new Vector2(0, 86);
        _primary = MenuButton("", true);
        _secondary = MenuButton("", false);
        _extraButton = MenuButton("", false);
        _credits = MenuButton("Авторы и музыка", false);
        _primary.Pressed += () => _action?.Invoke();
        _secondary.Pressed += () => _alternate?.Invoke();
        _extraButton.Pressed += () => _extraAction?.Invoke();
        _credits.Pressed += ShowCredits;
    }

    public void Menu(string title, string description, string main, Action action, string alternate = "",
        Action? alternateAction = null, string extra = "", Action? extraAction = null)
    {
        _heading.Text = title;
        _subheading.Text = description;
        _primary.Text = main;
        _action = action;
        _alternate = alternateAction;
        _secondary.Text = alternate;
        _secondary.Visible = alternateAction != null;
        _extraButton.Text = extra;
        _extraButton.Visible = extraAction != null;
        _extraAction = extraAction;
        _credits.Visible = true;
        _scrim.Show();
        _status.Hide();
        _hint.Hide();
        _nav.Hide();
        _prompt.Hide();
        _crosshair.Hide();
        _drive.Hide();
        _beach.Hide();
        _toast.Hide();
        _primary.GrabFocus();
        _leaveCredits = () => Menu(title, description, main, action, alternate, alternateAction, extra, extraAction);
    }

    private void ShowCredits()
    {
        var back = _leaveCredits;
        Menu("Авторы", "Music by Eric Matyas\nSound Effects by Eric Matyas\nwww.soundimage.org\n\nМодели людей — Quaternius\nТекстуры песка и коры — Poly Haven", "Назад", () => back?.Invoke());
        _credits.Hide();
    }

    public void Play()
    {
        _scrim.Hide();
        _status.Show();
        _hint.Show();
        _prompt.Show();
        _crosshair.Show();
        _drive.Show();
        _toast.Show();
    }

    public void UpdateStatus(string area, string goal, string clock, bool danger)
    {
        _district.Text = area;
        _objective.Text = goal;
        _clock.Text = clock;
        _clock.AddThemeColorOverride("font_color", danger ? new Color("ffc58b") : Thin);
    }

    public void Driving(bool inside, float speed, bool swim, bool beach, bool match)
    {
        _drive.Text = inside ? $"{speed * 3.6f:00}  км/ч" : "";
        _hint.Text = inside ? "WASD  Вождение   ·   Q  Взгляд назад   ·   Пробел  Тормоз   ·   E  Выйти" :
            swim ? "WASD  Плыть   ·   Ctrl  Нырнуть   ·   Пробел  Всплыть" :
            match ? "ЛКМ  Удар   ·   Мышь  Направление   ·   E  Покинуть матч" :
            beach ? "WASD  Движение   ·   Shift  Бег   ·   E  Действие   ·   Tab  Занятия" :
            "WASD  Движение   ·   Мышь  Обзор   ·   Shift  Бег   ·   E  Действие";
    }

    public void BeachStatus(bool visible, string activity, float energy, bool optics)
    {
        _beach.Visible = visible && !_scrim.Visible;
        _activity.Text = activity;
        _energy.Value = energy;
        _crosshair.Text = optics ? "+" : "·";
    }

    public void Guide(Vector3 from, Vector3 target, Vector3 heading, string detail, float remaining, bool visible)
    {
        _nav.Visible = visible && !_scrim.Visible;
        if (!_nav.Visible) return;
        var direction = (target - from).Slide(Vector3.Up).Normalized();
        var facing = heading.Slide(Vector3.Up).Normalized();
        if (facing.LengthSquared() > .1f && direction.LengthSquared() > .1f)
            _needle.Angle = -facing.SignedAngleTo(direction, Vector3.Up);
        _needle.QueueRedraw();
        _navStats.Text = detail + "\n" + (remaining >= 990 ? $"{remaining / 1000:0.00} км" : $"{remaining:0} м");
    }

    public void Message(string action, string small)
    {
        _prompt.Text = action;
        _toast.Text = small;
    }

    public void Tint(Color color) => _wash.Color = color;

    private Button MenuButton(string word, bool primary)
    {
        var button = new Button { Text = word, CustomMinimumSize = new Vector2(0, primary ? 52 : 42),
            MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        button.AddThemeFontSizeOverride("font_size", primary ? 22 : 17);
        var bg = new Color(primary ? Gold : "24424a");
        var fg = new Color(primary ? Ink : Cream);
        button.AddThemeStyleboxOverride("normal", Frame(bg, 6));
        button.AddThemeStyleboxOverride("hover", Frame(bg.Lightened(.15f), 6));
        button.AddThemeStyleboxOverride("pressed", Frame(bg.Darkened(.13f), 6));
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        button.AddThemeColorOverride("font_color", fg);
        button.AddThemeColorOverride("font_hover_color", fg);
        button.AddThemeColorOverride("font_pressed_color", fg);
        button.AddThemeColorOverride("font_focus_color", fg);
        _menuStack.AddChild(button);
        return button;
    }

    private static Panel Card(Color color) => new() { MouseFilter = Control.MouseFilterEnum.Ignore,
        Theme = MakePanelTheme(color) };

    private static Theme MakePanelTheme(Color color)
    {
        var theme = new Theme();
        var frame = Frame(color, 10);
        frame.BorderColor = new Color("638a8a65");
        frame.SetBorderWidthAll(1);
        theme.SetStylebox("panel", "Panel", frame);
        return theme;
    }

    private static StyleBoxFlat Frame(Color color, int radius)
    {
        var frame = new StyleBoxFlat { BgColor = color };
        frame.SetCornerRadiusAll(radius);
        return frame;
    }

    private static VBoxContainer Stack(Control parent, float left, float top, float right, float bottom, int gap)
    {
        var stack = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        parent.AddChild(stack);
        stack.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        stack.OffsetLeft = left;
        stack.OffsetTop = top;
        stack.OffsetRight = -right;
        stack.OffsetBottom = -bottom;
        stack.AddThemeConstantOverride("separation", gap);
        return stack;
    }

    private static Label Text(Control parent, string value, int size, string color)
    {
        var label = new Label { Text = value, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color(color));
        if (parent is Container) parent.AddChild(label);
        return label;
    }

    private static void Put(Control parent, Control child, float anchorX, float anchorY, float x, float y, float w, float h)
    {
        parent.AddChild(child);
        child.AnchorLeft = child.AnchorRight = anchorX;
        child.AnchorTop = child.AnchorBottom = anchorY;
        child.OffsetLeft = x;
        child.OffsetTop = y;
        child.OffsetRight = x + w;
        child.OffsetBottom = y + h;
    }

    private static void Shadow(Label label)
    {
        label.AddThemeColorOverride("font_shadow_color", new Color("0c222a"));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        label.AddThemeConstantOverride("outline_size", 3);
        label.AddThemeColorOverride("font_outline_color", new Color("10242c"));
    }

    private partial class Needle : Control
    {
        public float Angle { get; set; }
        public override void _Draw()
        {
            var at = Size / 2;
            DrawArc(at, 26, 0, Mathf.Tau, 36, new Color("718f8f"), 1.3f, true);
            Vector2[] points = { new(0, -19), new(11, 12), new(0, 6), new(-11, 12) };
            DrawColoredPolygon(points.Select(p => at + p.Rotated(Angle)).ToArray(), new Color(Gold));
        }
    }
}
