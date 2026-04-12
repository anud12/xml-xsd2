using System;
using Godot;

public partial class Main : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Ensure xml_xsd2.dll is present in the project root or in PATH
		var moduleZip = "module.zip"; // replace with actual module path or set via editor
		try {
			var dbPath = RuntimeInterop.ProcessArchive(moduleZip);
			GD.Print($"Runtime persisted DB: {dbPath}");
		} catch (Exception ex) {
			GD.PrintErr("RuntimeInterop call failed: " + ex.Message);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
