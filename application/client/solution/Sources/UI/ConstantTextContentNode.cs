using Godot;
using NewGameProject.UI;

public partial class ConstantTextContentNode : RichTextLabel, IContentNode {
    public ConstantTextContentNode(NewGameProject.Runtime.ConstantTextContent content) {
        Name = "content";
        FitContent = true;
        SetAutowrapMode(TextServer.AutowrapMode.WordSmart);
        SetJustificationFlags(TextServer.JustificationFlag.None);
        SetAnchorsPreset(LayoutPreset.FullRect);
        ApplyAlignment(content.Align);
        MouseFilter = MouseFilterEnum.Pass;
        UpdateContent(content);
    }

    public void UpdateContent(NewGameProject.Runtime.PanelContent content) {
        if (content is NewGameProject.Runtime.ConstantTextContent ctc) 
            Text = ctc.Value;
    }

    private void ApplyAlignment(string align) {
        switch (align) {
            case "top":
                SetHorizontalAlignment(HorizontalAlignment.Center);
                SetVerticalAlignment(VerticalAlignment.Top);
                break;
            case "top-left":
                SetHorizontalAlignment(HorizontalAlignment.Left);
                SetVerticalAlignment(VerticalAlignment.Top);
                break;
            case "top-right":
                SetHorizontalAlignment(HorizontalAlignment.Right);
                SetVerticalAlignment(VerticalAlignment.Top);
                break;
            case "center":
                SetHorizontalAlignment(HorizontalAlignment.Center);
                SetVerticalAlignment(VerticalAlignment.Center);
                break;
            case "center-left":
                SetHorizontalAlignment(HorizontalAlignment.Left);
                SetVerticalAlignment(VerticalAlignment.Center);
                break;
            case "center-right":
                SetHorizontalAlignment(HorizontalAlignment.Right);
                SetVerticalAlignment(VerticalAlignment.Center);
                break;
            case "bottom":
                SetHorizontalAlignment(HorizontalAlignment.Center);
                SetVerticalAlignment(VerticalAlignment.Bottom);
                break;
            case "bottom-left":
                SetHorizontalAlignment(HorizontalAlignment.Left);
                SetVerticalAlignment(VerticalAlignment.Bottom);
                break;
            case "bottom-right":
                SetHorizontalAlignment(HorizontalAlignment.Right);
                SetVerticalAlignment(VerticalAlignment.Bottom);
                break;
        }
    }
}
