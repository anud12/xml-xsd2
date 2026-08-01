using System;
using System.IO;
using System.Runtime.CompilerServices;
using GdUnit4;
using Godot;
using NewGameProject.Runtime;
using NewGameProject.UI;
using Vector2 = Godot.Vector2;

namespace NewGameProject.Tests.XUnit;

public partial class Steps {
    /// <summary>
    /// Creates an fluent assertion wrapper for the given <see cref="Panel"/>.
    /// </summary>
    /// <param name="panel">The panel to assert against, typically retrieved via <c>rootNode.GetNode&lt;Panel&gt;(id)</c>.</param>
    /// <returns>An <see cref="AssertPanel"/> instance for chained assertions.</returns>
    /// <example>
    /// AssertPanelThat(rootNode.GetNode&lt;Panel&gt;(idList[0]))
    ///     .IsNonNull()
    ///     .IsPositionEqual(450, 450);
    /// </example>
    public AssertPanel AssertPanelThat(Panel panel) {
        return new AssertPanel(panel);
    }

    /// <summary>
    /// Fluent assertion helper for Godot <see cref="Panel"/> nodes.
    /// </summary>
    /// <remarks>
    /// Supports method chaining for readable test assertions. Each method returns <c>this</c>
    /// to allow chaining. The instance can also be stored in a variable and reused across
    /// multiple test iterations to verify the same panel's state changes over time.
    /// </remarks>
    /// <example>
    /// Storing for reuse across iterations:
    /// <code>
    /// var assertions = AssertPanelThat(rootNode.GetNode&lt;Panel&gt;(idList[0]))
    ///     .HasContentText("0");
    /// // ... run some iterations ...
    /// assertions.HasContentText("3");
    /// </code>
    /// Chaining assertions:
    /// <code>
    /// AssertPanelThat(panel)
    ///     .IsNonNull()
    ///     .IsPositionEqual(450, 450)
    ///     .HasChildPanelNamed("child", child => child.IsPositionEqual(0, 0));
    /// </code>
    /// </example>
    public class AssertPanel {
        private Panel panel;
        private string path = "";
        public AssertPanel(Panel panel) {
            this.panel = panel;
            this.path = $"/{panel.Name}";
        }
        public AssertPanel(Panel panel, string path) {
            this.panel = panel;
            this.path = path;
        }

        /// <summary>
        /// Asserts that the panel is not null.
        /// </summary>
        /// <returns>This instance for chaining.</returns>
        public AssertPanel IsNonNull() {
            if (panel is null) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Panel {panel.Name} at \"{path}\" is null")
                    .IsFalse();
            }

            return this;
        }

        /// <summary>
        /// Asserts that the panel contains a child <see cref="Panel"/> with the specified name.
        /// </summary>
        /// <param name="name">The node name of the child panel (uses <c>%</c> lookup prefix internally).</param>
        /// <returns>This instance for chaining.</returns>
        public AssertPanel HasChildPanelNamed(string name) {
            HasChildPanelNamed(name, _ => { });
            return this;
        }

        /// <summary>
        /// Asserts that the panel contains a child <see cref="Panel"/> with the specified name,
        /// then invokes an action with an assertion wrapper for that child.
        /// </summary>
        /// <param name="name">The node name of the child panel (uses <c>%</c> lookup prefix internally).</param>
        /// <param name="action">
        /// A callback receiving a new <see cref="AssertPanel"/> instance for the child,
        /// allowing nested assertions on the child panel.
        /// </param>
        /// <returns>This instance for chaining on the parent panel.</returns>
        /// <example>
        /// AssertPanelThat(panel)
        ///     .HasChildPanelNamed("child", child => child.IsPositionEqual(0, 0))
        ///     .HasChildPanelNamed("child_2", child => child.IsPositionEqual(0, 10));
        /// </example>
        public AssertPanel HasChildPanelNamed(string name, Action<AssertPanel> action) {
            var childPanel = panel.GetNode<Panel>($"%{name}");
            if (childPanel is null) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Panel {panel.Name} does not have child named {name}")
                    .IsFalse();
            }

            action.Invoke(new AssertPanel(childPanel, $"{path}/{name}"));
            return this;
        }

        /// <summary>
        /// Asserts that the panel's position matches the expected coordinates.
        /// </summary>
        /// <param name="x">Expected X coordinate.</param>
        /// <param name="y">Expected Y coordinate.</param>
        /// <returns>This instance for chaining.</returns>
        public AssertPanel IsPositionEqual(float x, float y) {
            Assertions.AssertThat(panel.Position)
                .OverrideFailureMessage(
                    $"Panel at \"{path}\" position is not equal to ({x}, {y}) but is ({panel.Position.X}, {panel.Position.Y})")
                .IsEqual(new Vector2(x, y));

            return this;
        }

        /// <summary>
        /// Asserts that the panel's <c>content</c> child <see cref="RichTextLabel"/> contains the expected text.
        /// </summary>
        /// <param name="expcetedText">The expected text content of the panel's <c>content</c> label.</param>
        /// <returns>This instance for chaining.</returns>
        /// <remarks>
        /// Looks up a child node named <c>"content"</c> of type <see cref="RichTextLabel"/>.
        /// Fails if the child does not exist or its text does not match.
        /// </remarks>
        public AssertPanel HasContentText(string expcetedText) {
            var content = panel.GetNode<RichTextLabel>("content");
            if (content is null) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Panel at \"{path}\" does not have a RichTextLabel named \"{content}\"")
                    .IsFalse();
            }

            Assertions.AssertThat(content.Text)
                .OverrideFailureMessage($"Panel at \"{path}\" content text is not equal to {expcetedText} but is {content.Text}")
                .IsEqual(expcetedText);
            return this;
        }

        /// <summary>
        /// Asserts that the panel's <c>content</c> child is a <see cref="ContainerListViewContentNode"/>,
        /// then invokes an action with an assertion wrapper for nested assertions.
        /// </summary>
        /// <param name="action">
        /// A callback receiving an <see cref="AssertContainerListViewContent"/> instance
        /// for asserting properties of the container list view.
        /// </param>
        /// <returns>This instance for chaining on the parent panel.</returns>
        /// <example>
        /// AssertPanelThat(panel)
        ///     .HasContainerListViewContent(content =&gt; {
        ///         content.IsVertical();
        ///         content.HasTemplates(
        ///             p =&gt; p.HasContentText("1"),
        ///             p =&gt; p.HasContentText("2")
        ///         );
        ///     });
        /// </example>
        public AssertPanel HasContainerListViewContent(Action<AssertContainerListViewContent> action) {
            var content = panel.GetNode<ContainerListViewContentNode>("content");
            if (content is null) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Panel at \"{path}\" does not have a RichTextLabel named \"{content}\"")
                    .IsFalse();
                return this;
            }
            action.Invoke(new AssertContainerListViewContent(content, $"{path}/content"));
            return this;
        }

        /// <summary>
        /// Asserts that the panel's background texture matches the expected texture path.
        /// </summary>
        /// <param name="expectedTexture">The expected texture filename resolved from the animation's first frame.</param>
        /// <returns>This instance for chaining.</returns>
        /// <example>
        /// AssertPanelThat(panel)
        ///     .HasBackgroundTexture("frame_1.png");
        /// </example>
        public AssertPanel HasBackgroundTexture(string expectedTexture) {
            var ffiPanel = RuntimeInterop.GetPanelById(panel.Name);
            Assertions.AssertThat(ffiPanel.Background)
                .OverrideFailureMessage(
                    $"Panel at \"{path}\" background texture is not equal to \"{expectedTexture}\" but is \"{ffiPanel.Background}\"")
                .IsEqual(expectedTexture);
            return this;
        }

        /// <summary>
        /// Asserts that the visible pixels of the panel match the expected reference image.
        /// Captures the viewport, crops to the panel's global rectangle,
        /// and compares pixel-by-pixel against the expected PNG.
        /// </summary>
        /// <param name="relativeImagePath">Path to the expected reference PNG, relative to this file.</param>
        /// <param name="tolerance">Ratio of differing channels allowed (default 0.01 = 1%).</param>
        public AssertPanel ViewportMatches(string relativeImagePath, float tolerance = 0.01f,
            [CallerFilePath] string callerPath = "")
        {
            var viewport = panel.GetViewport();
            var viewportTexture = viewport.GetTexture();
            using var fullImage = viewportTexture.GetImage();

            var globalPos = panel.GetGlobalRect().Position;
            var size = panel.GetGlobalRect().Size;

            using var cropped = fullImage.GetRegion(new Rect2(globalPos, size));

            var baseDir = Path.GetDirectoryName(callerPath);
            var fullExpectedPath = Path.Combine(baseDir, relativeImagePath);

            if (!File.Exists(fullExpectedPath))
            {
                Assertions.AssertBool(false)
                    .OverrideFailureMessage($"Reference image not found at: {fullExpectedPath}")
                    .IsTrue();
                return this;
            }

            var actualName = DateTime.Now.ToFileTimeUtc() + "_actual_panel.png";
            var actualPath = Path.Combine(baseDir, actualName);
            var globalActualPath = ProjectSettings.GlobalizePath(actualPath);
            cropped.SavePng(globalActualPath);

            var expectedImg = new Image();
            expectedImg.LoadPngFromBuffer(File.ReadAllBytes(fullExpectedPath));

            bool equal = false;
            if (cropped.GetWidth() != expectedImg.GetWidth() || cropped.GetHeight() != expectedImg.GetHeight())
            {
                equal = false;
            }
            else
            {
                int diffChannels = 0;
                var actualData = cropped.GetData();
                var expectedData = expectedImg.GetData();
                for (int i = 0; i < actualData.Length; i++)
                {
                    if (Math.Abs(actualData[i] - expectedData[i]) > 1)
                        diffChannels++;
                }
                double diffRatio = (double)diffChannels / actualData.Length;
                equal = diffRatio <= tolerance;
            }

            expectedImg.Dispose();

            Assertions.AssertBool(equal)
                .OverrideFailureMessage(
                    $"Panel at \"{path}\" screenshot mismatch! Actual: \"{actualPath}\" vs Expected: \"{relativeImagePath}\"")
                .IsTrue();

            File.Delete(actualPath);
            return this;
        }
    }
}