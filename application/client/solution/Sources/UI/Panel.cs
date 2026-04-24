using Godot;
using System;

public partial class Panel : Godot.Panel
{
	private NewGameProject.Runtime.Panel panel;
	
	public Panel(NewGameProject.Runtime.Panel panel)
	{
		this.panel = panel;
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Size = new Vector2(panel.Size.Width, panel.Size.Height);
		
		var textNode = new Label
		{
			Text = panel.Id
		};
		AddChild(textNode);	
	}

	public override void _EnterTree()
	{
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
