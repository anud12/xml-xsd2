using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_4.ConcurrentEffects;

[TestSuite]
public class ConcurrentEffectsTests : Steps {
    [TestCase]
    public void Given_two_reoccurring_effects_modifying_same_entity_value_it_should_serialize_writes() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // Both effects reoccur every tick. Effect A adds 1, Effect B adds 10.
        // Each runs 5 times. If B reads A's write within the same tick
        // (proper serialization), final = 5*(1+10) = 55.
        // If B reads a stale value, final would be 5*10 = 50.
        for (int i = 0; i < 10; i++) {
            RuntimeInterop.RunIteration(1);
        }

        Assertions.AssertThat(RuntimeInterop.ReadEntityNumberValue("game", "counter"))
            .IsEqual("55");
    }

    [TestCase]
    public void Given_reoccurring_effect_and_action_effect_modifying_same_value_it_should_serialize_writes() {
        CleanupArchive();
        AddFileToArchive("module_action/index.js", "index.js")
            .AddFileToArchive("module_action/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // Tick 1: reoccurring effect fires (counter 0→1)
        RuntimeInterop.RunIteration(1);

        // Queue the action-triggered effect, then run tick 2 where both fire.
        RuntimeInterop.emitAction("action-effect");
        RuntimeInterop.RunIteration(1);

        // After tick 2: reoccurring added 1 (total 2), action added 10 (total 12 if serialized).
        // Run 3 more ticks: reoccurring fires 3 more times (total 5 applications).
        for (int i = 0; i < 3; i++) {
            RuntimeInterop.RunIteration(1);
        }

        // Expected: 5*1 (recur) + 1*10 (action) = 15
        Assertions.AssertThat(RuntimeInterop.ReadEntityNumberValue("game", "counter"))
            .IsEqual("15");
    }
}
