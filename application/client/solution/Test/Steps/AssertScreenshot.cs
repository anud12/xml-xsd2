using System.Runtime.CompilerServices;
using GdUnit4;
using Godot;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    public void AssertScreenshot(string relativeImagePath, float tolerance = 0.01f,
        [CallerFilePath] string callerPath = "")
    {
        var actualName = DateTime.Now.ToFileTimeUtc() + "_actual.png";
        var actualPath = SaveScreenshot(actualName,callerPath);;
        
        string baseDir = Path.GetDirectoryName(callerPath);
        string fullExpectedPath = Path.Combine(baseDir, relativeImagePath);
        string godotPath = ProjectSettings.LocalizePath(fullExpectedPath);

        using Image actualImage = runner.Scene().GetViewport().GetTexture().GetImage();
        
        var expectedTexture = GD.Load<Texture2D>(godotPath);
        if (expectedTexture == null)
        {
            Assertions.AssertBool(false).OverrideFailureMessage($"Reference not found at: {godotPath}").IsTrue();
            return;
        }
        
        using Image expectedImage = expectedTexture.GetImage();

        Assertions.AssertThat(actualImage)
            .OverrideFailureMessage($"Image mismatch! Actual: \"{actualPath}\" vs Expected: \"{relativeImagePath}\"")
            .IsEqual(expectedImage);
        File.Delete(actualPath);
    }
}