using NewGameProject.Tests.XUnit;
using Xunit;


public partial class Test : Steps
{
    [Fact]
    public void Given_panel_it_should_load_the_panel_into_the_scene()
    {
        // I create a module from the first folder
        AddFileToArchive("modules/index.js", "index.js")
            .AddFileToArchive("modules/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // Then panel IDs should contain 'panel' and 'panel_2'
        var panel = RuntimeInterop.GetPanelById( "panel");
        
        
    }
}