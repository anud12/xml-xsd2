using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;

public partial class SelectModule : Control
{
    public override void _Ready()
    {
        GetNode<Button>("Options/BackButton").Pressed += OnBack;
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
        GetTree().ChangeSceneToFile("res://Scenes/GameScene/Game.tscn");
    }

    void OnBack()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu/MainMenu.tscn");
    }
}
