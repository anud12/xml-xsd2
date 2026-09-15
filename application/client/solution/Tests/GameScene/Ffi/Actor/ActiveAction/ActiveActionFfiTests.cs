using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Actor.ActiveAction;

[TestSuite]
public class ActiveActionFfiTests : Steps {
    [TestCase]
    public void Given_free_actor_it_should_return_no_active_action() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        // A registered actor that received no action has nothing active.
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("actor-1")).IsEqual("");
        // An empty actor id also reports no active action.
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("")).IsEqual("");
    }

    [TestCase]
    public void Given_parked_action_it_should_return_its_name() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        RuntimeInterop.emitActionFor("long-action", "actor-1");
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("actor-1")).IsEqual("long-action");
    }

    [TestCase]
    public void Given_overwrite_it_should_return_the_latest_action() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        RuntimeInterop.emitActionFor("long-action", "actor-1");
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("actor-1")).IsEqual("long-action");
        // other-action is interruptible: it replaces long-action rather than queueing.
        RuntimeInterop.emitActionFor("other-action", "actor-1");
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("actor-1")).IsEqual("other-action");
    }

    [TestCase]
    public void Given_non_parking_action_it_should_return_no_active_action() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        RuntimeInterop.emitActionFor("long-action", "actor-1");
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("actor-1")).IsEqual("long-action");
        // instant-action runs without parking, discarding the parked plan.
        RuntimeInterop.emitActionFor("instant-action", "actor-1");
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("actor-1")).IsEqual("");
    }

    [TestCase]
    public void Given_busy_actor_another_free_actor_should_have_no_active_action() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        RuntimeInterop.emitActionFor("long-action", "actor-1");
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("actor-1")).IsEqual("long-action");
        // actor-2 never received an action: actor-1's plan does not leak to it.
        Assertions.AssertThat(RuntimeInterop.GetActorActiveAction("actor-2")).IsEqual("");
    }
}
