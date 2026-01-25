using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.GameUI;
using Source.Common.GUI;

namespace Game.UI;

public class GameConsole(IEngineAPI engineAPI, ISurface Surface, ISchemeManager Scheme) : IGameConsole
{
	bool Initialized;
	GameConsoleDialog? Console;
	public void Activate() {
		if (!Initialized)
			return;
		Surface.RestrictPaintToSinglePanel(null);
		Console!.Activate();
	}

	public void Clear() {
		if (!Initialized)
			return;
		Console!.Clear();
	}

	public void Hide() {
		if (!Initialized)
			return;
		Console!.Hide();
	}

	public void Initialize() {
		Console = new GameConsoleDialog();
		Console.MakeReadyForUse();

		Surface.GetScreenSize(out int swide, out int stall);
		int offsetX = Scheme.GetProportionalScaledValue(16);
		int offsetY = Scheme.GetProportionalScaledValue(64);

		Console.SetBounds(swide / 2 - offsetX, offsetY, swide / 2, stall - (offsetY * 2));

		Initialized = true;
	}

	public bool IsConsoleVisible() {
		if (!Initialized)
			return false;

		return Console!.IsVisible();
	}

	public void SetParent(IPanel? parent) {
		if (!Initialized)
			return;

		Console!.SetParent(parent);
	}

	[ConCommand("condump", "dump the text currently in the console to condumpXX.log")]
	static void condump() => Msg("not implemented\n");
}