using System;using System.IO;using System.Linq;using System.Runtime.CompilerServices;using GdUnit4;using Godot;namespace NewGameProject.Tests.XUnit;public partial class Steps{    public void AssertScreenshot(string relativeImagePath, float tolerance = 0.01f,        [CallerFilePath] string callerPath = "")    {        var actualName = DateTime.Now.ToFileTimeUtc() + "_actual.png";        var actualPath = DebugSaveScreenshot(actualName, callerPath);        string baseDir = Path.GetDirectoryName(callerPath);        string fullExpectedPath = Path.Combine(baseDir, relativeImagePath);        string godotPath = ProjectSettings.LocalizePath(fullExpectedPath);                // Reference-generation mode: when the expected image is missing,
        // save the current capture as the new reference and pass.
        if (!File.Exists(fullExpectedPath))
        {
            Directory.CreateDirectory(baseDir);
            File.Copy(actualPath, fullExpectedPath);
            GD.Print($"[refgen] created reference {fullExpectedPath}");
            File.Delete(actualPath);
            return;
        }                bool equal = false;
        try
        {
            var actualImg = new Image();
            actualImg.LoadPngFromBuffer(File.ReadAllBytes(actualPath));
            var expectedImg = new Image();
            expectedImg.LoadPngFromBuffer(File.ReadAllBytes(fullExpectedPath));

            if (actualImg.GetWidth() != expectedImg.GetWidth() || actualImg.GetHeight() != expectedImg.GetHeight())
            {
                equal = false;
            }
            else
            {
                int diffChannels = 0;
                var actualData = actualImg.GetData();
                var expectedData = expectedImg.GetData();
                for (int i = 0; i < actualData.Length; i++)
                {
                    if (Math.Abs(actualData[i] - expectedData[i]) > 1)
                    {
                        diffChannels++;
                    }
                }
                double diffRatio = (double)diffChannels / actualData.Length;
                equal = diffRatio <= tolerance;
            }
        }
        catch (Exception ex)
        {
            Assertions.AssertBool(false).OverrideFailureMessage($"Error comparing images: {ex.Message}").IsTrue();
            return;
        }        Assertions.AssertBool(equal)            .OverrideFailureMessage($"Image mismatch! Actual: \"{actualPath}\" vs Expected: \"{relativeImagePath}\"")            .IsTrue();        File.Delete(actualPath);    }}