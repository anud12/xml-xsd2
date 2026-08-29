using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_2.Hover.Propagate;

[TestSuite]
public partial class TestClass : Steps {
    /// Raw pixel bytes of a texture; empty when the texture is null. Pixel
    /// (not object-identity) comparison is used because a scene repaint can
    /// re-apply the same PNG as a fresh ImageTexture instance.
    static byte[] TexData(Texture2D? t) => t is null ? Array.Empty<byte>() : t.GetImage().GetData();

    static bool SamePixels(byte[] a, byte[] b) => a.AsSpan().SequenceEqual(b.AsSpan());

    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_parent_with_child_it_should_bubble_hover_action_to_parent() {
        try {
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .AddFileToArchive("module/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            SimulateMouse(new Vector2(0, 0));
            await runner.SimulateFrames(1);

            var parent = scene.Window("hoverParent");
            AssertPanelThat(parent).IsNonNull();

            var inner = scene.Window("inner");
            AssertPanelThat(inner).IsNonNull();

            // Hover over `inner`'s region (parent-local 20..50). `inner` has no
            // hover capability, so the parent must become the hover owner and
            // fire its action (bubble-up). Anchor to the top-level parent's
            // global position — a child's global position is not reliably
            // settled immediately after scene attach.
            var at = parent.GlobalPosition + new Vector2(30, 30);
            SimulateMouse(at);
            await runner.SimulateMouseMoveAbsolute(at, 0);
            await runner.SimulateFrames(1);

            AssertRuntimeOutputContains("___hover prop enter fired line___");

            SimulateMouse(new Vector2(0, 0));
            await runner.SimulateMouseMoveAbsolute(new Vector2(0, 0), 0);
            await runner.SimulateFrames(1);

            AssertRuntimeOutputContains("___hover prop exit fired line___");
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
        finally {
            CleanupArchive();
        }
    }

    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_child_stops_propagation_it_should_not_bubble_hover_action_to_parent() {
        try {
            AddFileToArchive("stop/index.js", "index.js")
                .AddFileToArchive("stop/manifest.json", "manifest.json")
                .AddFileToArchive("stop/texture.png", "texture.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            SimulateMouse(new Vector2(0, 0));
            await runner.SimulateFrames(1);

            var parent = scene.Window("parent");
            AssertPanelThat(parent).IsNonNull();

            var child = scene.Window("child");
            AssertPanelThat(child).IsNonNull();

            var at = child.GlobalPosition + new Vector2(1, 1);
            SimulateMouse(at);
            await runner.SimulateMouseMoveAbsolute(at, 0);
            await runner.SimulateFrames(1);

            AssertRuntimeOutputContains("___child hover enter fired line___");
            AssertRuntimeOutputContainsNot("___parent hover enter fired line___");

            SimulateMouse(new Vector2(0, 0));
            await runner.SimulateMouseMoveAbsolute(new Vector2(0, 0), 0);
            await runner.SimulateFrames(1);

            AssertRuntimeOutputContains("___child hover exit fired line___");
            AssertRuntimeOutputContainsNot("___parent hover exit fired line___");
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
        finally {
            CleanupArchive();
        }
    }

    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_parent_with_child_it_should_bubble_hover_background_to_parent() {
        try {
            AddFileToArchive("background/module/index.js", "index.js")
                .AddFileToArchive("background/module/manifest.json", "manifest.json")
                .AddFileToArchive("background/module/texture.png", "texture.png")
                .AddFileToArchive("background/module/hover.png", "hover.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            SimulateMouse(new Vector2(0, 0));
            await runner.SimulateFrames(1);

            var parent = scene.Window("hoverParent");
            AssertPanelThat(parent).IsNonNull();

            var inner = scene.Window("inner");
            AssertPanelThat(inner).IsNonNull();

            // The parent owns the hover background (a static PNG swap). Its
            // `background` TextureRect must hold the base texture before hover,
            // swap to the hover texture while hovered (even when the cursor is
            // over the non-hover-capable `inner` child, i.e. bubbled), and
            // revert to the base texture on exit.
            var bg = parent.GetNode<TextureRect>("background");
            var baseData = TexData(bg.Texture);
            Assertions.AssertThat(baseData.Length > 0)
                .OverrideFailureMessage("parent should start with its base background")
                .IsTrue();

            // Move the cursor over `inner`'s region (parent-local 20..50).
            // `inner` has no hover capability, so the parent must become the
            // hover owner and swap its background (bubble-up).
            var at = parent.GlobalPosition + new Vector2(30, 30);
            SimulateMouse(at);
            await runner.SimulateMouseMoveAbsolute(at, 0);
            await runner.SimulateFrames(1);

            Assertions.AssertThat(!SamePixels(TexData(bg.Texture), baseData))
                .OverrideFailureMessage("parent background should swap to its hover background when bubbled")
                .IsTrue();

            SimulateMouse(new Vector2(0, 0));
            await runner.SimulateMouseMoveAbsolute(new Vector2(0, 0), 0);
            await runner.SimulateFrames(1);

            Assertions.AssertThat(SamePixels(TexData(bg.Texture), baseData))
                .OverrideFailureMessage("parent background should revert to its base background after exit")
                .IsTrue();
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
        finally {
            CleanupArchive();
        }
    }

    [TestCategory("Step_2")]
    [TestCase]
    [RequireGodotRuntime]
    public async Task Given_child_stops_propagation_it_should_not_bubble_hover_background_to_parent() {
        try {
            AddFileToArchive("background/stop/index.js", "index.js")
                .AddFileToArchive("background/stop/manifest.json", "manifest.json")
                .AddFileToArchive("background/stop/texture.png", "texture.png")
                .AddFileToArchive("background/stop/hover.png", "hover.png")
                .EnsureDllAccessible()
                .ProcessArchive();

            var scene = await AttachUiScene();

            SimulateMouse(new Vector2(0, 0));
            await runner.SimulateFrames(1);

            var parent = scene.Window("parent");
            AssertPanelThat(parent).IsNonNull();

            var child = scene.Window("child");
            AssertPanelThat(child).IsNonNull();

            var parentBg = parent.GetNode<TextureRect>("background");
            var childBg = child.GetNode<TextureRect>("background");
            var parentBase = TexData(parentBg.Texture);
            var childBase = TexData(childBg.Texture);
            Assertions.AssertThat(parentBase.Length > 0)
                .OverrideFailureMessage("parent should start with its base background")
                .IsTrue();
            Assertions.AssertThat(childBase.Length > 0)
                .OverrideFailureMessage("child should start with its base background")
                .IsTrue();

            var at = child.GlobalPosition + new Vector2(1, 1);
            SimulateMouse(at);
            await runner.SimulateMouseMoveAbsolute(at, 0);
            await runner.SimulateFrames(1);

            Assertions.AssertThat(!SamePixels(TexData(childBg.Texture), childBase))
                .OverrideFailureMessage("child background should swap to its hover background")
                .IsTrue();
            Assertions.AssertThat(SamePixels(TexData(parentBg.Texture), parentBase))
                .OverrideFailureMessage("parent background must not swap when the child stops propagation")
                .IsTrue();

            SimulateMouse(new Vector2(0, 0));
            await runner.SimulateMouseMoveAbsolute(new Vector2(0, 0), 0);
            await runner.SimulateFrames(1);

            Assertions.AssertThat(SamePixels(TexData(childBg.Texture), childBase))
                .OverrideFailureMessage("child background should revert to its base background after exit")
                .IsTrue();
            ClearSimulatedMouse();
        }
        catch (Exception e) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Error: {e.Message}\n{e.StackTrace}")
                .IsFalse();
        }
        finally {
            CleanupArchive();
        }
    }
}
