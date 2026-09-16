using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.EsModule;

[TestSuite]
public class TestClass : Steps {
    [TestCategory("Step_0")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task MultiFileModule_it_should_bundle_esm_imports_and_load_the_panels() {
        try {
            // Hand-written ESM module spanning three files (no TypeScript). index.js
            // pulls ./panels/base and ./panels/offset with relative `import`
            // statements, so the two panels only exist if the runtime resolves
            // those imports against the archive and inlines them when loading the
            // entry. This isolates the runtime's multi-file ESM loading from the
            // tsc step exercised by the Typescript stage.
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/panels/base.js", "panels/base.js")
                .AddFileToArchive("module/panels/offset.js", "panels/offset.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            scene.AssertPanelThat("base").IsNonNull();
            scene.AssertPanelThat("offset").IsNonNull();

            DebugSaveScreenshot("result.png");
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
    }
}
