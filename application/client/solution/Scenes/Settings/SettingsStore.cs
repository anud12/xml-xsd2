using Godot;

public static class SettingsStore
{
    public static Vector2I Resolution { get; set; } = new Vector2I(0, 0);
    public static bool Borderless { get; set; }

    public static string SaveDirectory { get; set; } =
        ProjectSettings.GlobalizePath("user://");

    public static void Load()
    {
        var p = SavePath();
        if (System.IO.File.Exists(p))
        {
            try
            {
                var data = Godot.Json.ParseString(System.IO.File.ReadAllText(p));
                var obj = (Godot.Collections.Dictionary)data;
                if (obj.ContainsKey("resolution"))
                {
                    var arr = (Godot.Collections.Array)obj["resolution"];
                    Resolution = new Vector2I((int)arr[0].AsInt32(), (int)arr[1].AsInt32());
                }
                if (obj.ContainsKey("borderless"))
                    Borderless = (bool)obj["borderless"];
            }
            catch (System.Exception)
            {
            }
        }
    }

    public static void Save()
    {
        if (!System.IO.Directory.Exists(SaveDirectory))
            System.IO.Directory.CreateDirectory(SaveDirectory);
        var obj = new Godot.Collections.Dictionary
        {
            ["resolution"] = new Godot.Collections.Array { Resolution.X, Resolution.Y },
            ["borderless"] = Borderless,
        };
        System.IO.File.WriteAllText(SavePath(), Godot.Json.Stringify(obj, "  "));
    }

    public static void Clear()
    {
        var p = SavePath();
        if (System.IO.File.Exists(p))
            System.IO.File.Delete(p);
    }

    static string SavePath() => System.IO.Path.Combine(SaveDirectory, "settings.json");

    public static void Apply()
    {
        if (Resolution != Vector2I.Zero)
            DisplayServer.WindowSetSize(Resolution);

        if (Borderless)
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        else
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
    }
}
