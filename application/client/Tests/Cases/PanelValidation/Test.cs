using NewGameProject.Tests.XUnit;
using Xunit;


public partial class Test : Steps
{
    [Fact]
    public void Given_I_have_a_module_with_two_panels_When_I_get_the_panel_IDs_Then_they_should_be_correct()
    {
        // I create a module from the first folder
        AddFileToArchive("modules/index.js", "index.js")
            .AddFileToArchive("modules/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // Then panel IDs should contain 'panel' and 'panel_2'
        string[] panels = RuntimeInterop.GetPanelIds();
        Assert.Equal(2, panels.Length);
        Assert.Contains("panel", panels);
        Assert.Contains("panel_2", panels);
    }
}