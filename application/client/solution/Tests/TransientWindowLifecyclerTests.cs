using GdUnit4;
using NewGameProject.UI;
using static NewGameProject.UI.TransientWindowLifecycler;

namespace NewGameProject.Tests;

[TestSuite]
public class TransientWindowLifecyclerTests
{
    [TestCase]
    public void StateTable()
    {
        foreach (var pinned in new[] { false, true })
        foreach (var action in new[] { "pin", "unpin", "remove", "add", "source" })
        {
            var life = new TransientWindowLifecycler();
            if (pinned) life.Pin();
            switch (action)
            {
                case "pin": life.Pin(); break;
                case "unpin": life.Unpin(); break;
                case "remove": life.RemoveDelta(); break;
                case "add": life.AddDelta(); break;
                case "source": life.SourceRemoved(); break;
            }
            var expected = action switch
            {
                "pin" => Ownership.Pinned,
                "unpin" => Ownership.Transient,
                "remove" or "source" when !pinned => Ownership.Dismissed,
                _ => pinned ? Ownership.Pinned : Ownership.Transient
            };
            Assertions.AssertThat(life.State == expected).IsTrue();
        }
    }

    [TestCase]
    public void UnpinAfterExitOrReentry()
    {
        var life = new TransientWindowLifecycler();
        life.Pin(); life.RemoveDelta(); life.Unpin();
        Assertions.AssertThat(life.State == Ownership.Dismissed).IsTrue();
        life = new TransientWindowLifecycler();
        life.Pin(); life.RemoveDelta(); life.AddDelta(); life.Unpin();
        Assertions.AssertThat(life.State == Ownership.Transient).IsTrue();
    }
}
