using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Stage_0.Expression.ListReadOnlyExpression.RandomElement;

[TestSuite]
public class TestClass : Steps
{
    [TestCategory("Step_0")]
    [TestCase]
    public void randomElement_is_a_valid_member_and_driven_by_the_seed()
    {
        const uint seedA = 0x1234ABCD;
        try {
            // Reproducible for a fixed seed (the LXM stream governs the pick).
            double a1 = RunWithSeed(seedA);
            double a2 = RunWithSeed(seedA);
            Assertions.AssertThat(a1).IsEqual(a2);

            // It is a real member of the list (one of 1, 2, 3), never the absent default.
            Assertions.AssertThat(a1 == 1.0 || a1 == 2.0 || a1 == 3.0).IsTrue();

            // Different seeds drive different picks: across a few seeds at least
            // two distinct values must appear (it is not a constant).
            var picks = new HashSet<double> {
                a1,
                RunWithSeed(0xDEADBEEF),
                RunWithSeed(0x00000001),
                RunWithSeed(0x99999999),
            };
            Assertions.AssertThat(picks.Count >= 2).IsTrue();
        } finally {
            RuntimeInterop.SetSeed(0x7FFFFFFF); // restore the default seed
        }
    }

    double RunWithSeed(uint seed) {
        CleanupArchive();
        RuntimeInterop.SetSeed(seed);
        AddFileToArchive("module/index.js", "index.js")
            .AddFileToArchive("module/manifest.json", "manifest.json")
            .EnsureDllAccessible()
            .ProcessArchive();
        RuntimeInterop.RunIteration();
        return double.Parse(RuntimeInterop.ReadEntityNumberValue("sink", "pick"));
    }
}
