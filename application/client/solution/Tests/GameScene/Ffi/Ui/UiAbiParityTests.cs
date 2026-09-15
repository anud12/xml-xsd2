using System.Runtime.InteropServices;
using NewGameProject.UI;
using NewGameProject.Tests.XUnit;

namespace GdUnit4.Examples.Basics.Setup.Test.Ffi.Ui;

/// Layout drift guard: the managed mirrors in `UiAbi` must match the Rust
/// `#[repr(C)]` structs byte-for-byte. The Rust side publishes its sizes and
/// field offsets via `runtime_ui_abi_sizes`; this suite compares them against
/// `Marshal.SizeOf`/`OffsetOf`.
[TestSuite]
public class UiAbiParityTests : Steps
{
    [TestCase]
    public void Given_runtime_abi_sizes_they_should_match_managed_struct_sizes()
    {
        var s = UiAbi.AbiSizes();
        Assertions.AssertThat((int)s.Version).IsEqual((int)UiAbi.AbiVersion);
        Assertions.AssertThat(Marshal.SizeOf<UiAbi.UiSnapshot>()).IsEqual((int)s.SizeSnapshot);
        Assertions.AssertThat(Marshal.SizeOf<UiAbi.UiNode>()).IsEqual((int)s.SizeNode);
        Assertions.AssertThat(Marshal.SizeOf<UiAbi.UiBinding>()).IsEqual((int)s.SizeBinding);
        Assertions.AssertThat(Marshal.SizeOf<UiAbi.UiNodeOptions>()).IsEqual((int)s.SizeOptions);
        Assertions.AssertThat(Marshal.SizeOf<UiAbi.UiLayout>()).IsEqual((int)s.SizeLayout);
        Assertions.AssertThat(Marshal.SizeOf<UiAbi.UiTrack>()).IsEqual((int)s.SizeTrack);
        Assertions.AssertThat(Marshal.SizeOf<UiAbi.UiBackground>()).IsEqual((int)s.SizeBackground);
        Assertions.AssertThat(Marshal.SizeOf<UiAbi.UiMapLayer>()).IsEqual((int)s.SizeMapLayer);
        Assertions.AssertThat(Marshal.SizeOf<UiAbi.UiOnClick>()).IsEqual((int)s.SizeOnClick);
        Assertions.AssertThat(Marshal.SizeOf<UiAbi.UiClickStep>()).IsEqual((int)s.SizeClickStep);
        Assertions.AssertThat(Marshal.SizeOf<UiAbi.UiArgPair>()).IsEqual((int)s.SizeArgPair);
        Assertions.AssertThat(Marshal.SizeOf<UiAbi.UiOnHover>()).IsEqual((int)s.SizeOnHover);
        Assertions.AssertThat(Marshal.SizeOf<UiAbi.UiAnimation>()).IsEqual((int)s.SizeAnimation);
        Assertions.AssertThat(Marshal.SizeOf<UiAbi.UiDelta>()).IsEqual((int)s.SizeDelta);
        Assertions.AssertThat(Marshal.SizeOf<UiAbi.UiDeltaOp>()).IsEqual((int)s.SizeDeltaOp);
    }

    [TestCase]
    public void Given_runtime_abi_offsets_they_should_match_managed_field_offsets()
    {
        var s = UiAbi.AbiSizes();
        Check(s.OffNodeKind, typeof(UiAbi.UiNode), nameof(UiAbi.UiNode.Kind));
        Check(s.OffNodeId, typeof(UiAbi.UiNode), nameof(UiAbi.UiNode.Id));
        Check(s.OffNodeValue, typeof(UiAbi.UiNode), nameof(UiAbi.UiNode.Value));
        Check(s.OffNodeSrc, typeof(UiAbi.UiNode), nameof(UiAbi.UiNode.Src));
        Check(s.OffNodeBinding, typeof(UiAbi.UiNode), nameof(UiAbi.UiNode.Binding));
        Check(s.OffNodeOpt, typeof(UiAbi.UiNode), nameof(UiAbi.UiNode.Opt));
        Check(s.OffNodeChildCount, typeof(UiAbi.UiNode), nameof(UiAbi.UiNode.ChildCount));
        Check(s.OffNodeChildren, typeof(UiAbi.UiNode), nameof(UiAbi.UiNode.Children));
        Check(s.OffSnapVersion, typeof(UiAbi.UiSnapshot), nameof(UiAbi.UiSnapshot.Version));
        Check(s.OffSnapNodes, typeof(UiAbi.UiSnapshot), nameof(UiAbi.UiSnapshot.Nodes));
        Check(s.OffSnapAnims, typeof(UiAbi.UiSnapshot), nameof(UiAbi.UiSnapshot.Anims));
        Check(s.OffSnapStrings, typeof(UiAbi.UiSnapshot), nameof(UiAbi.UiSnapshot.Strings));
        Check(s.OffDeltaVersion, typeof(UiAbi.UiDelta), nameof(UiAbi.UiDelta.Version));
        Check(s.OffDeltaOps, typeof(UiAbi.UiDelta), nameof(UiAbi.UiDelta.Ops));
        Check(s.OffDeltaStrings, typeof(UiAbi.UiDelta), nameof(UiAbi.UiDelta.Strings));
        Check(s.OffDeltaOpOp, typeof(UiAbi.UiDeltaOp), nameof(UiAbi.UiDeltaOp.Op));
        Check(s.OffDeltaOpNode, typeof(UiAbi.UiDeltaOp), nameof(UiAbi.UiDeltaOp.Node));
        Check(s.OffOptsLayout, typeof(UiAbi.UiNodeOptions), nameof(UiAbi.UiNodeOptions.Layout));
        Check(s.OffOptsBackground, typeof(UiAbi.UiNodeOptions), nameof(UiAbi.UiNodeOptions.Background));
        Check(s.OffOptsOnClick, typeof(UiAbi.UiNodeOptions), nameof(UiAbi.UiNodeOptions.OnClick));
        Check(s.OffOptsOnHover, typeof(UiAbi.UiNodeOptions), nameof(UiAbi.UiNodeOptions.OnHover));
        Check(s.OffOptsContainer, typeof(UiAbi.UiNodeOptions), nameof(UiAbi.UiNodeOptions.Container));
        Check(s.OffOnClickSteps, typeof(UiAbi.UiOnClick), nameof(UiAbi.UiOnClick.Steps));
        Check(s.OffClickStepArgs, typeof(UiAbi.UiClickStep), nameof(UiAbi.UiClickStep.Args));
        Check(s.OffLayoutColTracks, typeof(UiAbi.UiLayout), nameof(UiAbi.UiLayout.ColTracks));
        Check(s.OffLayoutRowTracks, typeof(UiAbi.UiLayout), nameof(UiAbi.UiLayout.RowTracks));
        Check(s.OffBackgroundLayers, typeof(UiAbi.UiBackground), nameof(UiAbi.UiBackground.Layers));
        Check(s.OffAnimationFrames, typeof(UiAbi.UiAnimation), nameof(UiAbi.UiAnimation.Frames));
    }

    static void Check(uint nativeOffset, Type type, string field)
    {
        // Marshal.OffsetOf returns IntPtr; GdUnit4 has no AssertThat(IntPtr).
        Assertions.AssertThat(Marshal.OffsetOf(type, field).ToInt32()).IsEqual((int)nativeOffset);
    }
}
