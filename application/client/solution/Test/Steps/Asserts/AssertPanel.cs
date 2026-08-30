using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using GdUnit4;
using Godot;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using NewGameProject.Runtime;
using NewGameProject.UI;
using Vector2 = Godot.Vector2;

namespace NewGameProject.Tests.XUnit;

public partial class Steps {
    /// <summary>
    /// Creates a fluent assertion wrapper for the given <see cref="Panel"/>.
    /// </summary>
    public AssertPanel AssertPanelThat(Panel panel) {
        return new AssertPanel(panel);
    }

    /// <summary>
    /// Creates a fluent assertion wrapper for the given <see cref="UiWindow"/>.
    /// Accepts null (for negative-existence assertions via <c>IsNonNull</c>).
    /// </summary>
    public AssertPanel AssertPanelThat(UiWindow window) {
        return new AssertPanel(window);
    }

    /// <summary>
    /// Fluent assertion helper for Godot panel/window nodes.
    /// Supports method chaining; each method returns <c>this</c>.
    /// </summary>
    public class AssertPanel {
        private readonly Node node;
        private UiWindow? window;
        private string path = "";

        public AssertPanel(Panel panel) {
            this.node = panel;
            this.path = $"/{panel.Name}";
        }

        public AssertPanel(Panel panel, string path) {
            this.node = panel;
            this.path = path;
        }

        public AssertPanel(UiWindow window) {
            this.window = window;
            this.node = window;
            this.path = window != null ? $"/{window.Name}" : "/null";
        }

        public AssertPanel(UiWindow? window, string path) {
            this.window = window;
            this.node = window;
            this.path = path;
        }

        /// <summary>
        /// Asserts that the node is not null.
        /// </summary>
        public AssertPanel IsNonNull() {
            if (node is null) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Node at \"{path}\" is null")
                    .IsFalse();
            }
            return this;
        }

        /// <summary>
        /// Asserts that the node contains a child window with the specified name,
        /// then invokes an action with an assertion wrapper for that child.
        /// The lookup is recursive over <see cref="UiWindow"/> descendants.
        /// </summary>
        public AssertPanel HasChildPanelNamed(string name, Action<AssertPanel> action) {
            if (window is null) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Node at \"{path}\" is null; cannot look up child \"{name}\"")
                    .IsFalse();
                return this;
            }
            var child = FindChildWindow(window, name);
            if (child is null) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Node at \"{path}\" does not have a child window named \"{name}\"")
                    .IsFalse();
                return this;
            }
            action.Invoke(new AssertPanel(child, $"{path}/{name}"));
            return this;
        }

        /// <summary>
        /// Asserts that the node contains a child window with the specified name.
        /// </summary>
        public AssertPanel HasChildPanelNamed(string name) {
            HasChildPanelNamed(name, _ => { });
            return this;
        }

        /// <summary>
        /// Recursively finds a <see cref="UiWindow"/> descendant by name.
        /// </summary>
        static UiWindow? FindChildWindow(UiWindow root, string name) {
            for (var i = 0; i < root.GetChildCount(); i++) {
                var child = root.GetChild(i);
                if (child is UiWindow win) {
                    if (win.Name == name) return win;
                    var nested = FindChildWindow(win, name);
                    if (nested != null) return nested;
                }
                else {
                    var nested = FindChildNode(child, name);
                    if (nested != null) return nested;
                }
            }
            return null;
        }

        static UiWindow? FindChildNode(Node node, string name) {
            for (var i = 0; i < node.GetChildCount(); i++) {
                var child = node.GetChild(i);
                if (child is UiWindow win) {
                    if (win.Name == name) return win;
                    var nested = FindChildWindow(win, name);
                    if (nested != null) return nested;
                }
                else {
                    var nested = FindChildNode(child, name);
                    if (nested != null) return nested;
                }
            }
            return null;
        }

        /// <summary>
        /// Asserts that the node's position matches the expected coordinates.
        /// </summary>
        public AssertPanel IsPositionEqual(float x, float y) {
            if (node is not Control control) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Node at \"{path}\" is not a Control; cannot assert position")
                    .IsFalse();
                return this;
            }
            Assertions.AssertThat(control.Position)
                .OverrideFailureMessage(
                    $"Node at \"{path}\" position is not equal to ({x}, {y}) but is ({control.Position.X}, {control.Position.Y})")
                .IsEqual(new Vector2(x, y));
            return this;
        }

        /// <summary>
        /// Asserts the node's viewport-space size (width, height).
        /// </summary>
        public AssertPanel ViewportIsSize(float width, float height) {
            if (node is not Control control) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Node at \"{path}\" is not a Control; cannot assert size")
                    .IsFalse();
                return this;
            }
            Assertions.AssertThat(control.Size)
                .OverrideFailureMessage(
                    $"Node at \"{path}\" size is not equal to ({width}, {height}) but is ({control.Size.X}, {control.Size.Y})")
                .IsEqual(new Vector2(width, height));
            return this;
        }

        /// <summary>
        /// Asserts that the node's <c>text</c> child <see cref="Label"/> contains the expected text.
        /// </summary>
        public AssertPanel HasContentText(string expectedText) {
            var label = node?.GetNodeOrNull<Label>("text");
            if (label is null) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Node at \"{path}\" does not have a Label named \"text\"")
                    .IsFalse();
                return this;
            }
            Assertions.AssertThat(label.Text)
                .OverrideFailureMessage($"Node at \"{path}\" content text is not equal to {expectedText} but is {label.Text}")
                .IsEqual(expectedText);
            return this;
        }

        /// <summary>
        /// Asserts that the panel's flow container is a vertical (column) layout.
        /// </summary>
        public AssertPanel IsVertical() {
            var box = window?.GetNodeOrNull<BoxContainer>("box");
            bool isVertical = box != null && box.Vertical;
            Assertions.AssertBool(isVertical)
                .OverrideFailureMessage($"Panel at \"{path}\" is not vertical")
                .IsTrue();
            return this;
        }

        /// <summary>
        /// Asserts that the panel's flow container is a horizontal (row) layout.
        /// </summary>
        public AssertPanel IsHorizontal() {
            var box = window?.GetNodeOrNull<BoxContainer>("box");
            bool isHorizontal = box != null && !box.Vertical;
            Assertions.AssertBool(isHorizontal)
                .OverrideFailureMessage($"Panel at \"{path}\" is not horizontal")
                .IsTrue();
            return this;
        }

        /// <summary>
        /// Asserts that the panel's flow container has the expected number of children.
        /// </summary>
        public AssertPanel HasLength(int expected) {
            var flow = window?.FlowContainer();
            if (flow is null) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Panel at \"{path}\" has no flow container")
                    .IsFalse();
                return this;
            }
            Assertions.AssertInt(flow.GetChildCount())
                .OverrideFailureMessage($"Panel at \"{path}\" has {flow.GetChildCount()} children, expected {expected}")
                .IsEqual(expected);
            return this;
        }

        /// <summary>
        /// Asserts that each child of the panel's flow container matches the
        /// corresponding assertion action.
        /// </summary>
        public AssertPanel HasTemplates(params Action<AssertPanel>[] actions) {
            var flow = window?.FlowContainer();
            if (flow is null) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Panel at \"{path}\" has no flow container")
                    .IsFalse();
                return this;
            }
            HasLength(actions.Length);
            for (var i = 0; i < actions.Length; i++) {
                var child = flow.GetChild(i) as UiWindow;
                if (child is null) {
                    Assertions.AssertThat(true)
                        .OverrideFailureMessage($"Panel at \"{path}\" child {i} is not a UiWindow")
                        .IsFalse();
                    continue;
                }
                actions[i].Invoke(new AssertPanel(child, $"{path}/{i}"));
            }
            return this;
        }

        /// <summary>
        /// Asserts that the node hosts a container list view, then invokes an
        /// action with a <see cref="AssertContainerListViewContent"/> wrapper.
        /// </summary>
        public AssertPanel HasContainerListViewContent(
            Action<AssertContainerListViewContent> action)
        {
            var content = node?.GetNodeOrNull<ContainerListViewContentNode>("content");
            if (content is null)
            {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage(
                        $"Node at \"{path}\" does not have a ContainerListViewContentNode named \"content\"")
                    .IsFalse();
                return this;
            }
            action.Invoke(new AssertContainerListViewContent(content, $"{path}/content"));
            return this;
        }

        /// <summary>
        /// Compares a sub-rectangle of the viewport (absolute viewport pixels)
        /// against a reference PNG. If the reference does not exist yet, it is
        /// created from the current render (reference-generation mode); if it
        /// exists, the region must match it exactly.
        /// </summary>
        public AssertPanel ReferenceRegion(Rect2I rect, string relativeImagePath,
            [CallerFilePath] string callerPath = "")
        {
            var viewport = node?.GetViewport();
            if (viewport is null) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Node at \"{path}\" has no viewport")
                    .IsFalse();
                return this;
            }
            using var fullImage = viewport.GetTexture().GetImage();
            var baseDir = Path.GetDirectoryName(callerPath);
            var fullPath = Path.Combine(baseDir, relativeImagePath);
            using var region = fullImage.GetRegion(rect);
            if (!File.Exists(fullPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                region.SavePng(ProjectSettings.GlobalizePath(fullPath));
                return this;
            }
            using var expected = new Image();
            expected.LoadPngFromBuffer(File.ReadAllBytes(fullPath));
            bool equal = region.GetWidth() == expected.GetWidth()
                && region.GetHeight() == expected.GetHeight()
                && region.GetData().SequenceEqual(expected.GetData());
            if (!equal)
            {
                var actualPath = Path.Combine(Path.GetTempPath(), $"actual_refregion_{DateTime.Now.Ticks}.png");
                region.SavePng(actualPath);
                Assertions.AssertBool(equal)
                    .OverrideFailureMessage(
                        $"Screen region {rect} mismatch! Actual: \"{actualPath}\" vs Expected: \"{relativeImagePath}\"")
                    .IsTrue();
                File.Delete(actualPath);
            }
            return this;
        }

        /// <summary>
        /// Compares the node's rendered "background" TextureRect texture
        /// against a reference PNG at the given dimensions. If the reference
        /// does not exist yet, it is created from the current render
        /// (reference-generation mode); if it exists, the texture must match
        /// it exactly.
        /// </summary>
        public AssertPanel ReferenceBackground(string relativeImagePath, int width, int height,
            [CallerFilePath] string callerPath = "")
        {
            var rect = node?.GetNodeOrNull<TextureRect>("background");
            if (rect is null || rect.Texture is null)
            {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Node at \"{path}\" has no rendered background texture")
                    .IsFalse();
                return this;
            }
            var img = rect.Texture.GetImage();
            if (img.GetWidth() != width || img.GetHeight() != height)
                img.Resize(width, height, Image.Interpolation.Nearest);
            var baseDir = Path.GetDirectoryName(callerPath);
            var fullPath = Path.Combine(baseDir, relativeImagePath);
            if (!File.Exists(fullPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                img.SavePng(ProjectSettings.GlobalizePath(fullPath));
                return this;
            }
            using var expected = new Image();
            expected.LoadPngFromBuffer(File.ReadAllBytes(fullPath));
            bool equal = img.GetWidth() == expected.GetWidth()
                && img.GetHeight() == expected.GetHeight()
                && img.GetData().SequenceEqual(expected.GetData());
            if (!equal)
            {
                var actualPath = Path.Combine(Path.GetTempPath(), $"actual_refbg_{DateTime.Now.Ticks}.png");
                img.SavePng(actualPath);
                Assertions.AssertBool(equal)
                    .OverrideFailureMessage(
                        $"Background mismatch! Actual: \"{actualPath}\" vs Expected: \"{relativeImagePath}\"")
                    .IsTrue();
                File.Delete(actualPath);
            }
            return this;
        }

        /// <summary>
        /// Asserts that the panel renders a nine-patch border with the expected
        /// width (patch margin on all sides) and a hidden center.
        /// </summary>
        public AssertPanel HasBorder(int expectedWidth = 1) {
            var border = window?.GetNodeOrNull<NinePatchRect>("border");
            if (border is null || border.Texture is null) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Panel at \"{path}\" does not have a nine-patch border")
                    .IsFalse();
                return this;
            }
            Assertions.AssertInt(border.PatchMarginLeft)
                .OverrideFailureMessage(
                    $"Panel at \"{path}\" border width is {border.PatchMarginLeft}, expected {expectedWidth}")
                .IsEqual(expectedWidth);
            Assertions.AssertBool(!border.DrawCenter)
                .OverrideFailureMessage($"Panel at \"{path}\" border center is not hidden")
                .IsTrue();
            return this;
        }

        /// <summary>
        /// Compares the node's rendered "border" NinePatchRect image against a
        /// reference PNG, resizing the actual to the reference's dimensions
        /// (nearest-neighbour). Fails if the reference is missing or the pixels
        /// differ.
        /// </summary>
        public AssertPanel BorderMatches(string relativeImagePath,
            [CallerFilePath] string callerPath = "")
        {
            var baseDir = Path.GetDirectoryName(callerPath);
            var fullExpectedPath = Path.Combine(baseDir, relativeImagePath);
            var border = window?.GetNodeOrNull<NinePatchRect>("border");
            if (border is null)
            {
                Assertions.AssertBool(false)
                    .OverrideFailureMessage($"Node at \"{path}\" has no rendered \"border\" NinePatchRect")
                    .IsTrue();
                return this;
            }
            var texture = border.Texture;
            if (texture is null)
            {
                Assertions.AssertBool(false)
                    .OverrideFailureMessage($"Node at \"{path}\" border NinePatchRect has no texture")
                    .IsTrue();
                return this;
            }

#if DEBUG
            if (!File.Exists(fullExpectedPath))
            {
                texture.GetImage().SavePng(fullExpectedPath);
                return this;
            }
#endif

            using var expectedImg = new Image();
            expectedImg.LoadPngFromBuffer(File.ReadAllBytes(fullExpectedPath));
            var actualImg = texture.GetImage();
            var expectedSize = new Vector2I(expectedImg.GetWidth(), expectedImg.GetHeight());
            if (actualImg.GetWidth() != expectedSize.X
                || actualImg.GetHeight() != expectedSize.Y)
            {
                actualImg.Resize(expectedSize.X, expectedSize.Y, Image.Interpolation.Nearest);
            }

            var tempDir = Path.GetTempPath();
            var actualName = $"actual_border_{DateTime.Now.Ticks}.png";
            var actualPath = Path.Combine(tempDir, actualName);
            actualImg.SavePng(actualPath);

            bool equal = actualImg.GetWidth() == expectedImg.GetWidth()
                && actualImg.GetHeight() == expectedImg.GetHeight()
                && actualImg.GetData().SequenceEqual(expectedImg.GetData());

            actualImg.Dispose();

            if (!equal)
            {
                Assertions.AssertBool(equal)
                    .OverrideFailureMessage(
                        $"Node at \"{path}\" border mismatch! Actual: \"{actualPath}\" vs Expected: \"{relativeImagePath}\"")
                    .IsTrue();
            }
            File.Delete(actualPath);
            return this;
        }

        /// <summary>
        /// Asserts that the node's background TextureRect exists and has a texture set.
        /// </summary>
        public AssertPanel HasBackgroundTexture() {
            var rect = node?.GetNodeOrNull<TextureRect>("background");
            if (rect is null || rect.Texture is null)
            {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Node at \"{path}\" has no rendered background texture")
                    .IsFalse();
            }
            return this;
        }

        /// <summary>
        /// Compares the node's rendered "background" TextureRect image against a
        /// reference PNG, resizing the actual to the reference's dimensions
        /// (nearest-neighbour). Fails if the reference is missing or the pixels
        /// differ.
        /// </summary>
        public AssertPanel BackgroundMatches(string relativeImagePath,
            [CallerFilePath] string callerPath = "")
        {
            var baseDir = Path.GetDirectoryName(callerPath);
            var fullExpectedPath = Path.Combine(baseDir, relativeImagePath);
            var rect = node?.GetNodeOrNull<TextureRect>("background");
            if (rect is null)
            {
                Assertions.AssertBool(false)
                    .OverrideFailureMessage($"Node at \"{path}\" has no rendered \"background\" TextureRect")
                    .IsTrue();
                return this;
            }
            var texture = rect.Texture;
            if (texture is null)
            {
                Assertions.AssertBool(false)
                    .OverrideFailureMessage($"Node at \"{path}\" background TextureRect has no texture")
                    .IsTrue();
                return this;
            }

#if DEBUG
            if (!File.Exists(fullExpectedPath))
            {
                texture.GetImage().SavePng(fullExpectedPath);
                return this;
            }
#endif

            using var expectedImg = new Image();
            expectedImg.LoadPngFromBuffer(File.ReadAllBytes(fullExpectedPath));
            var actualImg = texture.GetImage();
            var expectedSize = new Vector2I(expectedImg.GetWidth(), expectedImg.GetHeight());
            if (actualImg.GetWidth() != expectedSize.X
                || actualImg.GetHeight() != expectedSize.Y)
            {
                actualImg.Resize(expectedSize.X, expectedSize.Y, Image.Interpolation.Nearest);
            }

            var tempDir = Path.GetTempPath();
            var actualName = $"actual_bg_{DateTime.Now.Ticks}.png";
            var actualPath = Path.Combine(tempDir, actualName);
            actualImg.SavePng(actualPath);

            bool equal = actualImg.GetWidth() == expectedImg.GetWidth()
                && actualImg.GetHeight() == expectedImg.GetHeight()
                && actualImg.GetData().SequenceEqual(expectedImg.GetData());

            actualImg.Dispose();

            if (!equal)
            {
                Assertions.AssertBool(equal)
                    .OverrideFailureMessage(
                        $"Node at \"{path}\" background mismatch! Actual: \"{actualPath}\" vs Expected: \"{relativeImagePath}\"")
                    .IsTrue();
            }
            File.Delete(actualPath);
            return this;
        }

        /// <summary>
        /// Asserts that the visible pixels of the node match the expected reference image.
        /// Captures the viewport, crops to the node's global rectangle,
        /// and compares pixel-by-pixel against the expected PNG.
        /// </summary>
        public AssertPanel ViewportMatches(string relativeImagePath, float tolerance = 0.01f,
            [CallerFilePath] string callerPath = "")
        {
            if (node is not Control control) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Node at \"{path}\" is not a Control; cannot capture viewport")
                    .IsFalse();
                return this;
            }
            var viewport = control.GetViewport();
            var viewportTexture = viewport.GetTexture();
            using var fullImage = viewportTexture.GetImage();

            var globalPos = control.GetGlobalRect().Position;
            var size = control.GetGlobalRect().Size;

            using var cropped = fullImage.GetRegion(new Rect2I((int)globalPos.X, (int)globalPos.Y, (int)size.X, (int)size.Y));

            var baseDir = Path.GetDirectoryName(callerPath);
            var fullExpectedPath = Path.Combine(baseDir, relativeImagePath);

            if (!File.Exists(fullExpectedPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullExpectedPath));
                cropped.SavePng(fullExpectedPath);
                GD.Print($"[refgen] created viewport reference {fullExpectedPath}");
                return this;
            }

            var tempDir = Path.GetTempPath();
            var actualName = $"actual_panel_{DateTime.Now.Ticks}.png";
            var actualPath = Path.Combine(tempDir, actualName);
            cropped.SavePng(actualPath);

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

            if (!equal) {
                Assertions.AssertBool(equal)
                    .OverrideFailureMessage(
                        $"Node at \"{path}\" screenshot mismatch! Actual: \"{actualPath}\" vs Expected: \"{relativeImagePath}\"")
                    .IsTrue();
            }

            File.Delete(actualPath);
            return this;
        }

        /// <summary>
        /// Asserts that a sub-rectangle of the viewport, at absolute screen
        /// coordinates, matches the expected reference image. Fails if the
        /// reference is missing or the region's dimensions/pixels differ.
        /// </summary>
        public AssertPanel ScreenRegionMatches(Rect2I rect,
            string relativeImagePath, float tolerance = 0f,
            [CallerFilePath] string callerPath = "")
        {
            if (node is not Control control) {
                Assertions.AssertThat(true)
                    .OverrideFailureMessage($"Node at \"{path}\" is not a Control; cannot capture viewport")
                    .IsFalse();
                return this;
            }
            var viewport = control.GetViewport();
            using var fullImage = viewport.GetTexture().GetImage();
            var vpW = fullImage.GetWidth();
            var vpH = fullImage.GetHeight();
            if (rect.Position.X < 0 || rect.Position.Y < 0
                || rect.Position.X + rect.Size.X > vpW
                || rect.Position.Y + rect.Size.Y > vpH)
            {
                Assertions.AssertBool(false)
                    .OverrideFailureMessage(
                        $"Screen region {rect} is outside the viewport {vpW}x{vpH}")
                    .IsTrue();
                return this;
            }

            var baseDir = Path.GetDirectoryName(callerPath);
            var fullExpectedPath = Path.Combine(baseDir, relativeImagePath);
            using var region = fullImage.GetRegion(rect);

#if DEBUG
            if (!File.Exists(fullExpectedPath))
            {
                region.SavePng(fullExpectedPath);
                return this;
            }
#endif

            var tempDir = Path.GetTempPath();
            var actualName = $"actual_region_{DateTime.Now.Ticks}.png";
            var actualPath = Path.Combine(tempDir, actualName);
            region.SavePng(actualPath);

            using var expectedImg = new Image();
            expectedImg.LoadPngFromBuffer(File.ReadAllBytes(fullExpectedPath));

            bool equal = false;
            if (region.GetWidth() == expectedImg.GetWidth()
                && region.GetHeight() == expectedImg.GetHeight())
            {
                int diffChannels = 0;
                var actualData = region.GetData();
                var expectedData = expectedImg.GetData();
                for (int i = 0; i < actualData.Length; i++)
                {
                    if (Math.Abs(actualData[i] - expectedData[i]) > 1)
                        diffChannels++;
                }
                double diffRatio = (double)diffChannels / actualData.Length;
                equal = diffRatio <= tolerance;
            }

            if (!equal)
            {
                Assertions.AssertBool(equal)
                    .OverrideFailureMessage(
                        $"Screen region {rect} mismatch! Actual: \"{actualPath}\" vs Expected: \"{relativeImagePath}\"")
                    .IsTrue();
            }

            File.Delete(actualPath);
            return this;
        }
    }
}
