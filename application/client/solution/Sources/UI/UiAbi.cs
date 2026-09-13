using System.Runtime.InteropServices;

namespace NewGameProject.UI;

/// Pure-POD mirror of the Rust `ui::abi` structs (`application/runtime/src/ui/abi.rs`).
/// Field order is the ABI contract: any change must bump `AbiVersion` on both
/// sides. The parity test (`Test/Ffi/Ui/UiAbiParityTests.cs`) compares these
/// layouts against `runtime_ui_abi_sizes` and fails on drift.
///
/// A snapshot or delta is a single native slab: struct regions first, a
/// NUL-terminated string arena second. Every `uint` string field is a byte
/// offset into that arena; `NoStr` means "no string".
public static class UiAbi
{
    public const uint AbiVersion = 1;
    public const uint NoStr = uint.MaxValue;

    public const uint KindDivision = 0;
    public const uint KindText = 1;
    public const uint KindField = 2;
    public const uint KindWindow = 3;
    public const uint KindImage = 4;
    public const uint KindCanvas = 5;

    public const byte OpAdd = 0;
    public const byte OpUpdate = 1;
    public const byte OpRemove = 2;

    public const byte MapNone = 0;
    public const byte MapNumber = 1;
    public const byte MapText = 2;

    public const byte LayoutNone = 0;
    public const byte LayoutColumn = 1;
    public const byte LayoutRow = 2;
    public const byte LayoutTracks = 3;

    public const byte TrackHasMin = 1;
    public const byte TrackHasMax = 2;

    public const byte BgNone = 0;
    public const byte BgStatic = 1;
    public const byte BgAnimation = 2;
    public const byte BgSpriteMap = 3;

    public const byte ClickNone = 0;
    public const byte ClickAction = 1;
    public const byte ClickSteps = 2;

    public const byte ArgString = 0;
    public const byte ArgNumber = 1;
    public const byte ArgCursor = 2;

    [StructLayout(LayoutKind.Sequential)]
    public struct UiTrack
    {
        public float Min;
        public float Max;
        public float Scale;
        public byte Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UiMapLayer
    {
        public uint Texture;
        public uint Layer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UiArgPair
    {
        public uint Key;
        public uint Value;
        public byte Vt;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UiClickStep
    {
        public uint Action;
        public uint ArgCount;
        public IntPtr Args;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UiOnClick
    {
        public byte Kind;
        public uint Action;
        public uint StepCount;
        public IntPtr Steps;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UiBinding
    {
        public uint Entity;
        public byte Map;
        public uint Name;
        public uint Fallback;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UiLayout
    {
        public byte Kind;
        public byte RowFirst;
        public byte Reverse;
        public float GapRow;
        public float GapCol;
        public uint EqualTracks;
        public uint ColCount;
        public uint RowCount;
        public IntPtr ColTracks;
        public IntPtr RowTracks;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UiBackground
    {
        public byte Kind;
        public byte Loop;
        public float Duration;
        public uint Path;
        public uint Name;
        public uint Map;
        public uint LayerCount;
        public IntPtr Layers;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UiOnHover
    {
        public byte StopPropagation;
        public float Thickness;
        public uint EmitAction;
        public uint Background;
        public uint Texture;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UiNodeOptions
    {
        public float X;
        public float Y;
        public byte HasXY;
        public byte HasSize;
        public uint Anchor;
        public uint Align;
        public float Width;
        public float Height;
        public UiLayout Layout;
        public UiBackground Background;
        public uint BorderTexture;
        public float BorderWidth;
        public byte HasBorder;
        public UiOnClick OnClick;
        public UiOnHover OnHover;
        public uint Container;
        public uint WorldMap;
        public uint WorldRoom;
        public uint CamRoom;
        public float CamX;
        public float CamY;
        public float CamZoom;
        public byte HasCamera;
        public byte Resizable;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UiNode
    {
        public uint Kind;
        public uint Id;
        public uint Value;
        public uint Src;
        public UiBinding Binding;
        public UiNodeOptions Opt;
        public uint ChildCount;
        public IntPtr Children;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UiAnimation
    {
        public uint Name;
        public uint FrameCount;
        public float Duration;
        public byte Loop;
        public IntPtr Frames;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UiSnapshot
    {
        public uint Version;
        public uint NodeCount;
        public uint AnimCount;
        public uint StringLen;
        public IntPtr Nodes;
        public IntPtr Anims;
        public IntPtr Strings;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UiDeltaOp
    {
        public byte Op;
        public UiNode Node;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UiDelta
    {
        public uint Version;
        public uint OpCount;
        public uint StringLen;
        public IntPtr Ops;
        public IntPtr Strings;
    }

    /// Mirror of Rust `UiAbiSizes`: sizes and field offsets published for the
    /// parity test.
    [StructLayout(LayoutKind.Sequential)]
    public struct UiAbiSizes
    {
        public uint Version;
        public uint SizeSnapshot;
        public uint SizeNode;
        public uint SizeBinding;
        public uint SizeOptions;
        public uint SizeLayout;
        public uint SizeTrack;
        public uint SizeBackground;
        public uint SizeMapLayer;
        public uint SizeOnClick;
        public uint SizeClickStep;
        public uint SizeArgPair;
        public uint SizeOnHover;
        public uint SizeAnimation;
        public uint SizeDelta;
        public uint SizeDeltaOp;
        public uint OffNodeKind;
        public uint OffNodeId;
        public uint OffNodeValue;
        public uint OffNodeSrc;
        public uint OffNodeBinding;
        public uint OffNodeOpt;
        public uint OffNodeChildCount;
        public uint OffNodeChildren;
        public uint OffSnapVersion;
        public uint OffSnapNodes;
        public uint OffSnapAnims;
        public uint OffSnapStrings;
        public uint OffDeltaVersion;
        public uint OffDeltaOps;
        public uint OffDeltaStrings;
        public uint OffDeltaOpOp;
        public uint OffDeltaOpNode;
        public uint OffOptsLayout;
        public uint OffOptsBackground;
        public uint OffOptsOnClick;
        public uint OffOptsOnHover;
        public uint OffOptsContainer;
        public uint OffOnClickSteps;
        public uint OffClickStepArgs;
        public uint OffLayoutColTracks;
        public uint OffLayoutRowTracks;
        public uint OffBackgroundLayers;
        public uint OffAnimationFrames;
    }

    [DllImport("libxml_xsd2", CallingConvention = CallingConvention.Cdecl)]
    private static extern UiAbiSizes runtime_ui_abi_sizes();

    public static UiAbiSizes AbiSizes() => runtime_ui_abi_sizes();
}
