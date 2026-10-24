using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Entity.Mutation;

[TestSuite]
public class EntityMutationFfiTests : Steps {
    [TestCase]
    public void Given_entity_it_should_mutate_number_map_via_put() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        RuntimeInterop.emitActionFor("mutate", "entity-1");
        RuntimeInterop.RunIteration();
        AssertRuntimeOutputContains("value_a=80");
        AssertRuntimeOutputContains("value_b=2");
    }

    [TestCase]
    public void Given_entity_it_should_mutate_text_map_via_put() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        RuntimeInterop.emitActionFor("mutate", "entity-1");
        RuntimeInterop.RunIteration();
        AssertRuntimeOutputContains("label=updated");
    }

    [TestCase]
    public void Given_entity_it_should_remove_from_number_map() {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .ProcessArchive();

        RuntimeInterop.emitActionFor("mutate", "entity-1");
        RuntimeInterop.RunIteration();
        AssertRuntimeOutputContains("value_b=absent");
    }
}
