using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Module.LxmSeed;

[TestSuite]
public class LxmSeedFfiTests : Steps {
    // The LXM PRNG is seeded per module at load. For a fixed seed the
    // number.random sequence is fully reproducible: these assert the exact
    // value for two distinct seeds. The two goldens differ (and the default
    // seed 0x7FFFFFFF yields yet another), so the seed actually drives output.
    [TestCase]
    public void number_random_is_reproducible_for_seed_a() {
        RunRoll(0x12345, 935913);
    }

    [TestCase]
    public void number_random_is_reproducible_for_seed_b() {
        RunRoll(0x54321, 850404);
    }

    void RunRoll(uint seed, int expected) {
        try {
            CleanupArchive();
            RuntimeInterop.SetSeed(seed);
            AddFileToArchive("module/index.js", "index.js")
                .AddFileToArchive("module/manifest.json", "manifest.json")
                .ProcessArchive();
            AssertRuntimeOutputContains("RND:" + expected);
        } finally {
            RuntimeInterop.SetSeed(0x7FFFFFFF); // restore the default seed
        }
    }
}
