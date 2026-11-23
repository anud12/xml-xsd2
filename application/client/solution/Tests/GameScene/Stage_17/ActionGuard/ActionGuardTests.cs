using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage17;

[TestSuite]
public class ActionGuardTests : Steps
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
        var host = LoadSceneForTest("res://Tests/GameScene/Stage_17/ActionGuard/ViewerHost.tscn");
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
        var host = LoadSceneForTest("res://Tests/GameScene/Stage_17/ActionGuard/ViewerHost.tscn");
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

    [TestCase]
    public void False_guard_denies_and_drains_composed_message()
    {
        LoadModule();
        RuntimeInterop.emitActionFor("denied", "actor-1");
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("actor-1")).IsEqual("");
        var messages = RuntimeInterop.DrainActionDenials();
        Assertions.AssertThat(messages.Length).IsEqual(1);
        Assertions.AssertThat(messages[0]).IsEqual("Cannot buy: cost 9, you have 4");
        Assertions.AssertThat(RuntimeInterop.DrainActionDenials().Length).IsEqual(0);
    }

    [TestCase]
    public void Join_prefix_and_suffix_resolve()
    {
        LoadModule();
        RuntimeInterop.emitActionFor("composed", "composed-actor");
        var messages = RuntimeInterop.DrainActionDenials();
        Assertions.AssertThat(messages.Length).IsEqual(1);
        Assertions.AssertThat(messages[0]).IsEqual("[a-b]");
    }

    [TestCase]
    public void Guard_exception_is_reported_as_denial()
    {
        LoadModule();
        RuntimeInterop.emitActionFor("broken", "broken-actor");
        var messages = RuntimeInterop.DrainActionDenials();
        Assertions.AssertThat(messages.Length).IsEqual(1);
        Assertions.AssertThat(messages[0]).IsEqual("broken guard error: Error: boom");
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("broken-actor")).IsEqual("");
    }

    [TestCase]
    public void Equal_value_is_denied()
    {
        LoadModule();
        RuntimeInterop.emitActionFor("boundary", "boundary-actor");
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("boundary-actor")).IsEqual("");
        var messages = RuntimeInterop.DrainActionDenials();
        Assertions.AssertThat(messages.Length).IsEqual(1);
        Assertions.AssertThat(messages[0]).IsEqual("equal");
    }

    [TestCase]
    public void True_guard_runs_apply()
    {
        LoadModule();
        RuntimeInterop.emitActionFor("allowed", "allowed-actor");
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("allowed-actor")).IsEqual("allowed");
        Assertions.AssertThat(RuntimeInterop.DrainActionDenials().Length).IsEqual(0);
    }

    [TestCase]
    public void Absent_guard_runs_apply()
    {
        LoadModule();
        RuntimeInterop.emitActionFor("unguarded", "unguarded-actor");
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("unguarded-actor")).IsEqual("unguarded");
        Assertions.AssertThat(RuntimeInterop.DrainActionDenials().Length).IsEqual(0);
    }
}
