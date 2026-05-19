using System;
using System.IO;
using Godot;
using GdUnit4.Examples.Basics.Setup.Sources.UI;
using NewGameProject.Runtime;

public partial class Main : Node
{
	private double _tickAccumulator = 0;
	private const double TickInterval = 0.1;
	private bool _initialized = false;

	public override void _Ready()
	{
		var modulePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "module.zip"));

		if (!File.Exists(modulePath))
		{
			GD.PrintErr($@"
**********************************************************
*  Missing module archive
*
*  Expected file not found:
*    {modulePath}
*
*  Place module.zip in the project root directory:
*    {Path.GetDirectoryName(modulePath)}
*
*  Aborting startup.
**********************************************************");
			return;
		}

		GD.Print($"[Main] Loading module from: {modulePath}");
		RuntimeInterop.ProcessArchive(modulePath);

		var rootNode = new RootNode();
		AddChild(rootNode);
		GD.Print("[Main] Runtime started.");
	}

	public override void _Process(double delta)
	{
		if (!_initialized)
		{
			_initialized = true;
			return;
		}

		_tickAccumulator += delta;
		if (_tickAccumulator >= TickInterval)
		{
			_tickAccumulator = 0;
			try
			{
				RuntimeInterop.RunIteration(0);
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[Runtime] Unhandled exception: {ex.Message}");
			}
		}
	}
}
