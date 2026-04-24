namespace GdUnit4.Examples.Basics.Setup.Test;


// This 'using static' directive allows us to call AssertThat() directly
// instead of writing Assertions.AssertThat(), making test code cleaner
using Godot;

using static Assertions;

[TestSuite]
public class GdUnitTest
{

    [TestCase]
    [RequireGodotRuntime]
    public void IsNodeNotNull()
    {
        var node = new Node();
        AssertThat(node).IsNotNull();
        node.Free();
    }
}