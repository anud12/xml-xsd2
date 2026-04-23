using Xunit;

namespace NewGameProject.Tests.XUnit
{
    public class ModuleScenarios : Steps
    {
        [Fact]
        public void Given_I_create_module_from_first_folder_When_load_it_Then_two_panels_registered2()
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
}