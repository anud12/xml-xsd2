using GdUnit4.Examples.Basics.Setup.Sources.UI;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;
using Vector2 = Godot.Vector2;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_5.ElapsedTimeUnits;

[TestSuite]
public partial class TestClass : Steps {
    [TestCategory("Step_5")] [TestCase] [RequireGodotRuntime]
    public async Task Given_RunIteration_should_increment_elapsed_time() {
        // I create a module from the first folder
        AddFileToArchive("module/index.js", "index.js")
            .EnsureDllAccessible()
            .ProcessArchive();

        //Run iteration with 1 unit elapsed time
        RuntimeInterop.RunIteration(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(1L);
        
        
        //Run iteration with 1 unit elapsed time
        RuntimeInterop.RunIteration(1);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(2L);
        
        
        //Run iteration with 2 units elapsed time
        RuntimeInterop.RunIteration(2);
        Assertions.AssertThat(RuntimeInterop.GetElapsedTimeUnits()).IsEqual(4L);
    }
}