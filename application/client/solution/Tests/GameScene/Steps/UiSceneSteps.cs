using GdUnit4;
using Godot;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    /// <summary>
    /// Loads a scene file into the runner (replacing the previous scene) and
    /// returns the root node. Useful for testing non-game scenes like the
    /// main menu and settings.
    /// </summary>
    public Node LoadSceneForTest(string path)
    {
        runner = ISceneRunner.Load(path, true);
        var scene = runner.Scene();
        Assertions.AssertThat(runner).IsNotNull();
        Assertions.AssertThat(scene).IsNotNull();
        return scene;
    }

    /// <summary>
    /// Pushes synthetic mouse press + release events at the given control's
    /// center so the control's gui_input handler runs and its signal fires.
    /// A frame is pumped between press and release so the engine services the
    /// button-press before the release; pushing both in one frame deadlocks
    /// the scene-tree's input processing and SimulateFrames never returns.
    /// </summary>
    public async Task<Steps> ClickControl(string path, Node root)
    {
        var control = root.GetNode<Control>(path);
        var viewport = runner.Scene().GetViewport();
        var center = control.GlobalPosition + control.Size / 2f;
        var press = new InputEventMouseButton {
            Position = center,
            GlobalPosition = center,
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            ButtonMask = MouseButtonMask.Left
        };
        viewport.PushInput(press);
        await runner.SimulateFrames(1);
        var release = new InputEventMouseButton {
            Position = center,
            GlobalPosition = center,
            ButtonIndex = MouseButton.Left,
            Pressed = false,
            ButtonMask = 0
        };
        viewport.PushInput(release);
        return this;
    }

    /// <summary>
    /// Sets a CheckButton's pressed state directly (fires its toggled signal).
    /// </summary>
    public Steps SetCheckButton(string path, Node root, bool pressed)
    {
        var button = root.GetNode<CheckButton>(path);
        button.ButtonPressed = pressed;
        return this;
    }

    /// <summary>
    /// Selects an OptionButton item by index (fires its item_selected signal).
    /// </summary>
    public Steps SetOptionButton(string path, Node root, int index)
    {
        var option = root.GetNode<OptionButton>(path);
        option.Selected = index;
        return this;
    }

    /// <summary>
    /// Returns the scene tree's current scene (the scene most recently loaded
    /// via ChangeSceneToFile or ISceneRunner.Load).
    /// </summary>
    public Node GetCurrentScene()
    {
        return ((SceneTree)Engine.GetMainLoop()).CurrentScene;
    }
}
