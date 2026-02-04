using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Game.Client;

public class CenterStringLabel : Label
{
	public CenterStringLabel(Panel parent) : base(parent, "CenterStringLabel", " ") {
		SetParent(parent);
		ComputeSize();
		SetVisible(false);
		SetCursor(0);
		SetKeyboardInputEnabled(false);
		SetMouseInputEnabled(false);
		SetContentAlignment(Alignment.Center);

		Font = null;
		SetFgColor(new Color(255, 255, 255, 255));

		SetPaintBackgroundEnabled(false);

		CentertimeOff = 0.0;

		VGui.AddTickSignal(this, 100);
	}

	public override void OnScreenSizeChanged(int oldWide, int oldTall) {
		base.OnScreenSizeChanged(oldWide, oldTall);
		ComputeSize();
	}

	public void Clear() => CentertimeOff = 0;

	private void ComputeSize() {
		int w, h;
		w = ScreenWidth();
		h = ScreenHeight();

		int iHeight = (int)(h * 0.3f);

		SetSize(w, iHeight);
		SetPos(0, (int)((h * 0.35f) - (iHeight / 2f)));
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		Font = scheme.GetFont("Trebuchet24");
		Assert(Font != null);
		SetFont(Font);

		int w, h;
		w = ScreenWidth();
		h = ScreenHeight();
		int iHeight = (int)(h * 0.3f);
		SetSize(w, iHeight);
		SetPos(0, (int)((h * 0.35f) - (iHeight / 2)));
	}

	public void SetTextColor(int r, int g, int b, int a) => SetFgColor(new(r, g, b, a));

	public bool ShouldDraw() {
		if (engine.IsDrawingLoadingImage())
			return false;

		if (CentertimeOff <= gpGlobals.CurTime)
			// not time to turn off the message yet
			return false;

		return true;
	}

	public override void OnTick() {
		bool visible = ShouldDraw();
		if (IsVisible() != visible)
			SetVisible(visible);
	}

	static readonly ConVar scr_centertime = new("scr_centertime", "2", 0);

	public void Print(ReadOnlySpan<char> text) {
		SetText(text);
		CentertimeOff = scr_centertime.GetDouble() + gpGlobals.CurTime;
	}
	public void ColorPrint(int r, int g, int b, int a, ReadOnlySpan<char> text) {
		SetTextColor(r, g, b, a);
		Print(text);
	}

	IFont? Font;
	TimeUnit_t CentertimeOff;
}

public class CenterPrint : ICenterPrint
{
	public static readonly CenterPrint CenterString = new();

	public void Clear() => vguiCenterString?.Clear();
	public void ColorPrint(int r, int g, int b, int a, ReadOnlySpan<char> text) => vguiCenterString?.ColorPrint(r, g, b, a, text);
	public void Print(ReadOnlySpan<char> text) => vguiCenterString?.Print(text);
	public void SetTextColor(int r, int g, int b, int a) => vguiCenterString?.SetTextColor(r, g, b, a);

	CenterStringLabel? vguiCenterString;

	public void Create(IPanel parent) {
		if (vguiCenterString != null)
			Destroy();
		vguiCenterString = new CenterStringLabel((Panel)parent);
	}

	public void Destroy() {
		if (vguiCenterString != null) {
			vguiCenterString.SetParent(null);
			vguiCenterString.DeletePanel();
			vguiCenterString = null;
		}
	}

	[ConCommand]
	static void scr_centerprint(in TokenizedCommand cmd) {
		if (cmd.ArgC() >= 2)
			centerprint.ColorPrint(cmd.Arg(2, 255), cmd.Arg(3, 255), cmd.Arg(4, 255), cmd.Arg(5, 255), cmd.Arg(1));
		else
			Msg("scr_centerprint (text) (r?) (g?) (b?) (a?) \n");
	}
}
