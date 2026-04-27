using GdUnit4;
using NewGameProject.Runtime;

namespace NewGameProject.Tests.XUnit;

public partial class Steps {
    
    public void AssertRuntimeOutputContains(string expected) {
        bool found = LogLines.Any(line => line.Contains(expected));

        if (!found) {
            Assertions.AssertThat(true)
                .OverrideFailureMessage($"Runtime output does not contain a line containing: \"{expected}\"")
                .IsFalse();
        }
    }
}