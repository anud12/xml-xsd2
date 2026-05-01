using Godot;
using NewGameProject.Runtime;

public partial class EntityNumberValueContentNode : RichTextLabel {
    
    private EntityNumberValueContent content;
    
    public EntityNumberValueContentNode(EntityNumberValueContent content) {
        Name = "content";
        FitContent = true;
        this.content = content;
        SetAutowrapMode(TextServer.AutowrapMode.WordSmart);
        SetJustificationFlags(TextServer.JustificationFlag.None);
        SetAnchorsPreset(LayoutPreset.FullRect);
        ApplyAlignment(content.Align);
        Text = RuntimeInterop.GetEntityNumberMapValue(content.EntityId, content.Name);
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

    public override void _Process(double delta) {
        Text = RuntimeInterop.ReadEntityNumberValue(content.EntityId, content.Name);
    }
}
