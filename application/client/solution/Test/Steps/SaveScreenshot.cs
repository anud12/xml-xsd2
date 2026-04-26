using System.Runtime.CompilerServices;
using Godot;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    public string DebugSaveScreenshot(string fileName, [CallerFilePath] string callerPath = "")
    {
        using Image img = runner.Scene().GetViewport().GetTexture().GetImage();

        // 1. Get the directory of the .cs file calling this method
        string baseDir = Path.GetDirectoryName(callerPath);
        string absolutePath = Path.Combine(baseDir, fileName);

        // 2. Ensure Godot can handle the OS-specific path
        string globalPath = ProjectSettings.GlobalizePath(absolutePath);

        // 3. Ensure folder exists
        Directory.CreateDirectory(baseDir);

        Error err = img.SavePng(globalPath);
        if (err != Error.Ok)
            GD.PrintErr($"Failed to save screenshot to {globalPath}: {err}");
        else
            GD.Print($"Screenshot saved to: {globalPath}");
        return globalPath;
    }

}