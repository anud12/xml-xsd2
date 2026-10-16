using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Module.HotReload;

[TestSuite]
public class HotReloadFfiTests : Steps
{
    [TestCategory("HotReload")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_running_module_when_hot_reloaded_then_state_is_kept_and_new_declarations_apply()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .AddFileToArchive("module/tex1.png", "tex1.png")
            .EnsureDllAccessible()
            .ProcessArchive();

        var scene = await AttachUiScene();

        // v1: the initial emit fires on the first processed iteration (t=10)
        // and the reoccurance is parked on the next 10-boundary (t=20).
        RuntimeInterop.RunIteration(10);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(10L);
        Assertions.AssertThat(ReadKey()).IsEqual(1.0);

        // v1 declarations: the badge shows the red texture, and there is no
        // node-2 / grid-2 yet.
        Assertions.AssertThat(BackgroundPixel(scene.Window("badge")))
            .IsEqual(new Color(1f, 0f, 0f, 1f));
        Assertions.AssertThat(RuntimeInterop.GetEntityNumberMapValue("node-2", "x"))
            .IsEqual("");
        Assertions.AssertThat(ContainerInterop.GetContainerIds().Contains("grid-2"))
            .IsFalse();

        // Swap the module content (same entry names) and hot reload.
        AddFileToArchive("module_v2/index.js", "index.js")
            .AddFileToArchive("module_v2/manifest.json", "manifest.json")
            .AddFileToArchive("module_v2/tex2.png", "tex2.png")
            .ReloadCurrentArchive();
        await runner.SimulateFrames(1);

        // The previous game state is kept: the clock did not reset and the
        // entity value was NOT re-initialized to the declared 0.
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(10L);
        Assertions.AssertThat(ReadKey()).IsEqual(1.0);

        // The reloaded module's new declarations are live.
        Assertions.AssertThat(RuntimeInterop.GetActionIds()).Contains("ping");

        // The same panel now renders the v2 texture (loaded from the reloaded
        // archive on repaint).
        Assertions.AssertThat(BackgroundPixel(scene.Window("badge")))
            .IsEqual(new Color(0f, 0f, 1f, 1f));

        // New entity declared by the reloaded module is live...
        Assertions.AssertThat(double.Parse(
            RuntimeInterop.GetEntityNumberMapValue("node-2", "x"))).IsEqual(1.0);
        Assertions.AssertThat(double.Parse(
            RuntimeInterop.GetEntityNumberMapValue("node-2", "y"))).IsEqual(1.0);

        // ...and so is the new container, holding both entities with the
        // preserved/declared positions.
        var ids = ContainerInterop.GetContainerIds();
        Assertions.AssertThat(ids).Contains("grid-2");
        var grid = ContainerInterop.GetContainerById("grid-2");
        Assertions.AssertThat(grid.Entities).Contains("node-1");
        Assertions.AssertThat(grid.Entities).Contains("node-2");
        var gx = grid.GetXForEntityId ?? new Dictionary<string, double>();
        var gy = grid.GetYForEntityId ?? new Dictionary<string, double>();
        Assertions.AssertThat(gx["node-2"]).IsEqual(1.0);
        Assertions.AssertThat(gy["node-2"]).IsEqual(1.0);

        // The module entry re-emitted the recurring effect: it fires on the
        // next processed iteration...
        RuntimeInterop.RunIteration(5);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(15L);
        Assertions.AssertThat(ReadKey()).IsEqual(2.0);

        // ...and now reoccurs with the v2 period (5, not the old 10): it
        // fires at t=20 and t=25.
        RuntimeInterop.RunIteration(10);
        await runner.SimulateFrames(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(25L);
        Assertions.AssertThat(ReadKey()).IsEqual(4.0);
    }

    [TestCategory("HotReload")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_reoccurring_effect_when_reloaded_mid_period_then_it_keeps_its_phase()
    {
        CleanupArchive();
        AddFileToArchive("module_reoccur/index.js", "index.js")
            .AddFileToArchive("module_reoccur/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // The effect reoccurs every 3 units. Its initial emit fires on the
        // first processed iteration (t=2) and parks on the next 3-boundary
        // (t=3).
        RuntimeInterop.RunIteration(2);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(2L);
        Assertions.AssertThat(ReadKey()).IsEqual(1.0);

        // Hot reload mid-period (t=2, between the fire at t=2 and the next
        // due at t=3). The reloaded module re-declares the same effect but
        // does NOT re-emit it, so any fire after reload must come from the
        // preserved schedule.
        AddFileToArchive("module_reoccur_v2/index.js", "index.js")
            .AddFileToArchive("module_reoccur_v2/manifest.json", "manifest.json")
            .ReloadCurrentArchive();

        // State is preserved: the clock and the entity value carry over.
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(2L);
        Assertions.AssertThat(ReadKey()).IsEqual(1.0);

        // The reoccurrence kept its phase: it was already due at t=3, so a
        // single tick reaches it. If the reload had reset the schedule to a
        // full period from the reload tick, it would not fire until t=5 and
        // the key would still read 1 here.
        RuntimeInterop.RunIteration(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(3L);
        Assertions.AssertThat(ReadKey()).IsEqual(2.0);

        // And it keeps reoccurring on the same absolute 3-boundaries (t=6).
        RuntimeInterop.RunIteration(3);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(6L);
        Assertions.AssertThat(ReadKey()).IsEqual(3.0);
    }

    [TestCategory("HotReload")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_effect_not_declared_when_reloaded_then_it_stops_executing()
    {
        CleanupArchive();
        AddFileToArchive("module_reoccur/index.js", "index.js")
            .AddFileToArchive("module_reoccur/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // The effect reoccurs every 3 units; the initial emit fires at t=2
        // and parks on the next 3-boundary (t=3).
        RuntimeInterop.RunIteration(2);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(2L);
        Assertions.AssertThat(ReadKey()).IsEqual(1.0);

        // Reload to a module that keeps the entity but no longer declares the
        // "repeat" effect.
        AddFileToArchive("module_noeffect_v2/index.js", "index.js")
            .AddFileToArchive("module_noeffect_v2/manifest.json", "manifest.json")
            .ReloadCurrentArchive();

        // The entity value is still preserved...
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(2L);
        Assertions.AssertThat(ReadKey()).IsEqual(1.0);

        // ...but the parked reoccurrence has nothing to resolve to, so it
        // stops firing even when its boundary arrives.
        RuntimeInterop.RunIteration(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(3L);
        Assertions.AssertThat(ReadKey()).IsEqual(1.0);

        RuntimeInterop.RunIteration(3);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(6L);
        Assertions.AssertThat(ReadKey()).IsEqual(1.0);
    }

    static double ReadKey() =>
        double.Parse(RuntimeInterop.GetEntityNumberMapValue("node-1", "key"));

    static Color BackgroundPixel(UiWindow window)
    {
        var tr = window.GetNode<TextureRect>("background");
        var img = ((ImageTexture)tr.Texture).GetImage();
        return img.GetPixel(0, 0);
    }
}
