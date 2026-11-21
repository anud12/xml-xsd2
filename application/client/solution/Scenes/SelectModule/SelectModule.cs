using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;

public partial class SelectModule : Control
{
    public override void _Ready()
    {
        GetNode<Button>("Options/BackButton").Pressed += OnBack;
        // Default the seed field to the current Unix time (seconds) so each run
        // starts from a fresh, time-based seed unless the user overrides it.
        GetNode<LineEdit>("Options/SeedInput").Text =
            DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        PopulateModules(GetNode<VBoxContainer>("Options/ModuleList"));
    }

    /// Scans the Modules folder (each subfolder holds a manifest.json) and adds
    /// one button per module. Selecting a module points the game at that
    /// folder and loads the game scene.
    void PopulateModules(VBoxContainer list)
    {
        string modulesDir = ProjectSettings.GlobalizePath("res://Modules/");
        if (!Directory.Exists(modulesDir))
        {
            GD.PrintErr($"Modules directory not found: {modulesDir}");
            return;
        }

        foreach (string dir in Directory.GetDirectories(modulesDir).OrderBy(d => d, StringComparer.Ordinal))
        {
            string manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath)) continue;

            string name = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            try
            {
                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
                if (doc.RootElement.TryGetProperty("name", out var n) &&
                    n.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(n.GetString()))
                {
                    name = n.GetString()!;
                }
            }
            catch (System.Exception ex)
            {
                GD.PushWarning($"Could not read manifest {manifestPath}: {ex.Message}");
            }

            string moduleDir = dir;
            var button = new Button
            {
                Text = name,
                CustomMinimumSize = new Vector2(0, 48),
            };
            button.Pressed += () => OnSelect(moduleDir);
            list.AddChild(button);
        }
    }

    public void OnSelect(string moduleDir)
    {
        Game.MODULE_DIR = moduleDir;
        Game.SEED = ParseSeed(GetNode<LineEdit>("Options/SeedInput").Text);
        GetTree().ChangeSceneToFile("res://Scenes/GameScene/Game.tscn");
    }

    /// Parses the seed field into a 32-bit LXM seed. Blank yields null (the
    /// default sequence); decimal and "0x" hex are accepted; anything else
    /// warns and falls back to the default.
    static uint? ParseSeed(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = text.Trim();
        try
        {
            if (text.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase))
                return Convert.ToUInt32(text.Substring(2), 16);
            return uint.Parse(text);
        }
        catch
        {
            GD.PushWarning($"Invalid random seed '{text}'; using the default");
            return null;
        }
    }

    void OnBack()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu/MainMenu.tscn");
    }
}
