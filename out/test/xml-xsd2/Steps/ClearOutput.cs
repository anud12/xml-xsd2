using System.Collections.Generic;

namespace NewGameProject.Tests.XUnit;

public partial class Steps {
    public Steps ClearOutput() {
        LogLines = new List<string>();
        return this;
    }
}
