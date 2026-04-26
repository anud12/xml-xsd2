using GdUnit4;
using Godot;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    public AssertPanel AssertPanelThat(Panel panel)
    {
        return new AssertPanel(panel);
    }

    public class AssertPanel
    {
        private Panel panel;

        public AssertPanel(Panel panel)
        {
            this.panel = panel;
        }

        public AssertPanel IsNonNull()
        {
            if (panel is null)
            {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Panel {panel.Name} is null")
                    .IsFalse();
            }
            return this;
        }

        public AssertPanel HasChildNamed(string name)
        {
            if (panel.GetNode(name) is null)
            {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Panel {panel.Name} does not have child named {name}")
                    .IsFalse();
            }

            return this;
        }

        public AssertPanel IsPositionEqual(float x, float y)
        {
            Assertions.AssertThat(panel.Position).IsEqual(new Vector2(x, y));

            return this;
        }
    }
}