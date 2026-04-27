using GdUnit4;
using Godot;

namespace NewGameProject.Tests.XUnit;

public partial class Steps {
    public AssertPanel AssertPanelThat(Panel panel) {
        return new AssertPanel(panel);
    }

    public class AssertPanel {
        private Panel panel;

        public AssertPanel(Panel panel) {
            this.panel = panel;
        }

        public AssertPanel IsNonNull() {
            if (panel is null) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Panel {panel.Name} is null")
                    .IsFalse();
            }

            return this;
        }

        public AssertPanel HasChildPanelNamed(string name) {
            HasChildPanelNamed(name, _ => { });
            return this;
        }

        public AssertPanel HasChildPanelNamed(string name, Action<AssertPanel> action) {
            var childPanel = panel.GetNode<Panel>($"%{name}");
            if (childPanel is null) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Panel {panel.Name} does not have child named {name}")
                    .IsFalse();
            }

            action.Invoke(new AssertPanel(childPanel));
            return this;
        }

        public AssertPanel IsPositionEqual(float x, float y) {
            Assertions.AssertThat(panel.Position)
                .OverrideFailureMessage(
                    $"Panel \"{panel.Name}\" position is not equal to ({x}, {y}) but is ({panel.Position.X}, {panel.Position.Y})")
                .IsEqual(new Vector2(x, y));

            return this;
        }

        public AssertPanel HasContentText(string expcetedText) {
            var content = panel.GetNode<RichTextLabel>("content");
            if (content is null) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Panel does not have a RichTextLabel named \"{content}\"")
                    .IsFalse();
            }

            Assertions.AssertThat(content.Text)
                .OverrideFailureMessage($"Panel content text is not equal to {expcetedText} but is {content.Text}")
                .IsEqual(expcetedText);
            return this;
        }
    }
}