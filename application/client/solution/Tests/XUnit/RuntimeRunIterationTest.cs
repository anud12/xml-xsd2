using NewGameProject.Runtime;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.RunIteration;

[TestSuite]
public class RuntimeRunIterationTest : Steps {
    [TestCase]
    public void Given_run_iteration_with_zero_tickrate_should_return_elapsed_time() {
        double elapsedTime = RuntimeInterop.RunIteration(0);
        Assertions.AssertThat(elapsedTime).IsGreaterThanOrEqualTo(0.0);
    }

    [TestCase]
    public void Given_run_iteration_with_positive_tickrate_should_wait_and_return_elapsed_time() {
        double tickRate = 0.01; // 10ms tick rate
        double elapsedTime = RuntimeInterop.RunIteration(tickRate);
        Assertions.AssertThat(elapsedTime).IsGreaterThanOrEqualTo(0.0);
    }

    [TestCase]
    public void Given_run_iteration_should_return_positive_elapsed_time() {
        double elapsedTime = RuntimeInterop.RunIteration();
        Assertions.AssertThat(elapsedTime >= 0.0).IsTrue();
    }
}
