using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.Expression.ListReadOnlyExpression.ExactlyOne;

[TestSuite]
public class TestClass : Steps
{
    [TestCategory("Step_0")]
    [TestCase]
    public void exactlyOne_is_present_only_when_the_list_holds_exactly_one_value()
    {
        CleanupArchive();
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();

        // Fire the pending effect: it runs `.exactlyOne()` over three lookups
        // and writes each result into the "game" sink entity.
        RuntimeInterop.RunIteration();

        // Exactly one entity -> present: carries "solo"'s cost (5).
        Assertions.AssertThat(double.Parse(
            RuntimeInterop.ReadEntityNumberValue("game", "one"))).IsEqual(5.0);

        // Zero entities -> absent: falls back to the default (-1).
        Assertions.AssertThat(double.Parse(
            RuntimeInterop.ReadEntityNumberValue("game", "zero"))).IsEqual(-1.0);

        // More than one entity -> absent: falls back to the default (-1).
        Assertions.AssertThat(double.Parse(
            RuntimeInterop.ReadEntityNumberValue("game", "multi"))).IsEqual(-1.0);
    }
}
