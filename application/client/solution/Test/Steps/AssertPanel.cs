using GdUnit4;
using Godot;

namespace NewGameProject.Tests.XUnit;

public partial class Steps
{
    public class AssertPanel
    {
        private Panel panel;
        
        public AssertPanel(Panel panel)
        {
            this.panel = panel;
        }

        public AssertPanel IsPositionEqual(float x, float y)
        {
            Assertions.AssertThat(panel.Position).IsEqual(new Vector2(x,y));
            
            return this;
        }
    }
    public AssertPanel AssertPanelThat(Panel panel)
    {
        return new AssertPanel(panel);
    }
}