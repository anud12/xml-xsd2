using Godot;

public partial class Settings : Control
{
    static readonly (string Label, Vector2I Size)[] Presets =
    {
        ("1920 x 1080", new Vector2I(1920, 1080)),
        ("2560 x 1440", new Vector2I(2560, 1440)),
        ("3840 x 2160", new Vector2I(3840, 2160)),
        ("2560 x 1080 (Widescreen)", new Vector2I(2560, 1080)),
    };

    public override void _Ready()
    {
        SettingsStore.Load();

        var options = GetNode<OptionButton>("Content/ResolutionRow/ResolutionOption");
        var current = GetNode<CheckButton>("Content/BorderlessRow/BorderlessOption");

        for (int i = 0; i < Presets.Length; i++)
            options.AddItem(Presets[i].Label);
        options.Selected = IndexForSize(SettingsStore.Resolution);
        options.ItemSelected += i => OnResolutionSelected(i);

        current.ButtonPressed = SettingsStore.Borderless;
        current.Toggled += v => OnBorderlessToggled(v);

        GetNode<Button>("Content/BackButton").Pressed += OnBack;
    }

    public void OnResolutionSelected(long index)
    {
        SettingsStore.Resolution = Presets[(int)index].Size;
        Persist();
    }

    public void OnBorderlessToggled(bool toggled)
    {
        SettingsStore.Borderless = toggled;
        Persist();
    }

    int IndexForSize(Vector2I size)
    {
        for (int i = 0; i < Presets.Length; i++)
            if (Presets[i].Size == size)
                return i;
        return 0;
    }

    void Persist()
    {
        SettingsStore.Save();
        SettingsStore.Apply();
    }

    public void OnBack()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu/MainMenu.tscn");
    }
}
