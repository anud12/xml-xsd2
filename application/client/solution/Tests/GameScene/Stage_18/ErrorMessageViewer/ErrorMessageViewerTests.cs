using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage18;

[TestSuite]
[TestCategory("Stage_18")]
public class ErrorMessageViewerTests : Steps
{
    private void LoadModule()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();
        RuntimeInterop.DrainActionDenials();
    }

    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Viewer_shows_guard_denial_message()
    {
        LoadModule();
        var host = LoadSceneForTest("res://Tests/GameScene/Stage_18/ErrorMessageViewer/ViewerHost.tscn");
        var viewer = host.GetNode<Control>("ErrorMessageViewer");
        await runner.SimulateFrames(5);
        var stack = viewer.GetNode<VBoxContainer>("Stack");
        Assertions.AssertThat(viewer.Visible).IsFalse();
        Assertions.AssertThat(stack.GetChildCount()).IsEqual(0);
        AssertPanelThat(viewer).ViewportMatches("viewer-before.png");

        RuntimeInterop.emitActionFor("denied", "viewer-actor");
        await runner.SimulateFrames(5);
        Assertions.AssertThat(viewer.Visible).IsTrue();
        Assertions.AssertThat(stack.GetChild<PanelContainer>(0).GetChild<Label>(0).Text).IsEqual("Cannot buy: cost 9, you have 4");
        AssertPanelThat(viewer).ViewportMatches("viewer-after.png");
    }

    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Viewer_stacks_denials_and_fades_each_independently()
    {
        LoadModule();
        var host = LoadSceneForTest("res://Tests/GameScene/Stage_18/ErrorMessageViewer/ViewerHost.tscn");
        var viewer = host.GetNode<NewGameProject.UI.ErrorMessageViewer>("ErrorMessageViewer");
        var stack = viewer.GetNode<VBoxContainer>("Stack");
        await runner.SimulateFrames(5);
        viewer.SetProcess(false);
        AssertPanelThat(viewer).ViewportMatches("viewer-before.png");

        RuntimeInterop.emitActionFor("denied", "fade-actor-1");
        viewer._Process(0);
        viewer._Process(1);
        RuntimeInterop.emitActionFor("composed", "fade-actor-2");
        viewer._Process(0);
        viewer._Process(1);
        RuntimeInterop.emitActionFor("boundary", "fade-actor-3");
        viewer._Process(0);
        await runner.SimulateFrames(5);
        Assertions.AssertThat(stack.GetChildCount()).IsEqual(3);
        var oldest = stack.GetChild<PanelContainer>(0);
        var middle = stack.GetChild<PanelContainer>(1);
        var newest = stack.GetChild<PanelContainer>(2);
        Assertions.AssertThat(oldest.GetChild<Label>(0).Text).IsEqual("Cannot buy: cost 9, you have 4");
        Assertions.AssertThat(middle.GetChild<Label>(0).Text).IsEqual("[a-b]");
        Assertions.AssertThat(newest.GetChild<Label>(0).Text).IsEqual("equal");
        AssertPanelThat(viewer).ViewportMatches("viewer-three-errors.png");

        // Oldest is 5.375 seconds old; the other two are still fully opaque.
        viewer._Process(3.375);
        await runner.SimulateFrames(5);
        Assertions.AssertThat(oldest.Modulate.A > 0 && oldest.Modulate.A < 1).IsTrue();
        Assertions.AssertThat(middle.Modulate.A).IsEqual(1f);
        Assertions.AssertThat(newest.Modulate.A).IsEqual(1f);
        AssertPanelThat(viewer).ViewportMatches("viewer-oldest-fading.png");
        viewer._Process(0.5);
        await runner.SimulateFrames(5);
        Assertions.AssertThat(stack.GetChildCount()).IsEqual(2);
        Assertions.AssertThat(stack.GetChild<PanelContainer>(0).GetChild<Label>(0).Text).IsEqual("[a-b]");
        Assertions.AssertThat(middle.Modulate.A).IsEqual(1f);
        Assertions.AssertThat(newest.Modulate.A).IsEqual(1f);
        viewer._Process(2);
        await runner.SimulateFrames(5);
        Assertions.AssertThat(stack.GetChildCount()).IsEqual(0);
        Assertions.AssertThat(viewer.Visible).IsFalse();
        AssertPanelThat(viewer).ViewportMatches("viewer-before.png");
    }

    [TestCase(Timeout = 10_000)]
    [RequireGodotRuntime]
    public async Task Viewer_stacks_appended_messages_with_denial_and_fades_independently()
    {
        LoadModule();
        var host = LoadSceneForTest("res://Tests/GameScene/Stage_18/ErrorMessageViewer/ViewerHost.tscn");
        var viewer = host.GetNode<NewGameProject.UI.ErrorMessageViewer>("ErrorMessageViewer");
        var stack = viewer.GetNode<VBoxContainer>("Stack");
        await runner.SimulateFrames(5);
        viewer.SetProcess(false);
        RuntimeInterop.emitActionFor("denied", "append-view-denied");
        viewer._Process(0);
        viewer._Process(1);
        RuntimeInterop.emitActionFor("append", "append-view-actor");
        viewer._Process(0);
        await runner.SimulateFrames(5);
        Assertions.AssertThat(stack.GetChildCount()).IsEqual(3);
        var oldest = stack.GetChild<PanelContainer>(0);
        var middle = stack.GetChild<PanelContainer>(1);
        var newest = stack.GetChild<PanelContainer>(2);
        Assertions.AssertThat(oldest.GetChild<Label>(0).Text).IsEqual("Cannot buy: cost 9, you have 4");
        Assertions.AssertThat(middle.GetChild<Label>(0).Text).IsEqual("appended-1");
        Assertions.AssertThat(newest.GetChild<Label>(0).Text).IsEqual("appended-2");
        AssertPanelThat(viewer).ViewportMatches("viewer-appended-stack.png");
        viewer._Process(4.375);
        await runner.SimulateFrames(5);
        Assertions.AssertThat(oldest.Modulate.A > 0 && oldest.Modulate.A < 1).IsTrue();
        Assertions.AssertThat(middle.Modulate.A).IsEqual(1f);
        Assertions.AssertThat(newest.Modulate.A).IsEqual(1f);
        AssertPanelThat(viewer).ViewportMatches("viewer-appended-fading.png");
        viewer._Process(0.5);
        await runner.SimulateFrames(5);
        Assertions.AssertThat(stack.GetChildCount()).IsEqual(2);
    }

    [TestCase]
    public void Appended_messages_are_drained_in_order()
    {
        LoadModule();
        RuntimeInterop.emitActionFor("append", "append-data-actor");
        var messages = RuntimeInterop.DrainActionDenials();
        Assertions.AssertThat(messages.Length).IsEqual(2);
        Assertions.AssertThat(messages[0]).IsEqual("appended-1");
        Assertions.AssertThat(messages[1]).IsEqual("appended-2");
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("append-data-actor")).IsEqual("append");
        Assertions.AssertThat(RuntimeInterop.DrainActionDenials().Length).IsEqual(0);
    }

    [TestCase]
    public void Effect_apply_message_is_drained_after_tick()
    {
        LoadModule();
        RuntimeInterop.emitAction("emit-message-effect");
        RuntimeInterop.RunIteration(1);
        var messages = RuntimeInterop.DrainActionDenials();
        Assertions.AssertThat(messages.Length).IsEqual(1);
        Assertions.AssertThat(messages[0]).IsEqual("effect-message");
        Assertions.AssertThat(RuntimeInterop.DrainActionDenials().Length).IsEqual(0);
    }

}
