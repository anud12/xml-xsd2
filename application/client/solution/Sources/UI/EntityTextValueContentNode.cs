using Godot;
using NewGameProject.Runtime;
using NewGameProject.UI;

public partial class EntityTextValueContentNode : RichTextLabel, IContentNode {
    
    private EntityTextValueContent content;
    
    public EntityTextValueContentNode(EntityTextValueContent content) {
        Name = "content";
        FitContent = true;
        this.content = content;
        SetAutowrapMode(TextServer.AutowrapMode.WordSmart);
        SetJustificationFlags(TextServer.JustificationFlag.None);
        SetAnchorsPreset(LayoutPreset.FullRect);
        ApplyAlignment(content.Align);
        Text = RuntimeInterop.GetEntityTextMapValue(content.EntityId, content.Name);
        MouseFilter = MouseFilterEnum.Pass;
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

    public void UpdateContent(NewGameProject.Runtime.PanelContent content) {
        if (content is EntityTextValueContent etvc)
            Text = RuntimeInterop.GetEntityTextMapValue(etvc.EntityId, etvc.Name);
    }

    public override void _Process(double delta) {
        Text = RuntimeInterop.ReadEntityTextValue(content.EntityId, content.Name);
    }
}
