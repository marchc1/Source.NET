using Source.Common;
using Source.Common.GUI;
using Source.GUI.Controls;

using System.Numerics;

namespace Game.Client;

public class DebugOverlay : Panel
{
	IFont? Font;
	public DebugOverlay(Panel? parent) : base(parent, "CDebugOverlay") {
		surface.GetScreenSize(out int w, out int h);
		SetParent(parent);
		SetSize(w, h);
		SetPos(0, 0);
		SetVisible(false);
		SetCursor(CursorCode.None);

		Font = null;
		SetFgColor(new(0, 0, 0, 0));
		SetPaintBackgroundEnabled(false);

		IScheme scheme = SchemeManager.LoadSchemeFromFileEx(enginevgui.GetPanel(Source.Engine.VGuiPanelType.ClientDllTools), "resource/ClientScheme.res", "Client")!;
		SetScheme(scheme);

		VGui.AddTickSignal(this, 250);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		Font = scheme.GetFont("DebugOverlay");
		Assert(Font != null);

		surface.GetScreenSize(out int w, out int h);
		SetSize(w, h);
		SetPos(0, 0);
	}

	public override void OnTick() {
		bool visible = ShouldDraw();
		if (visible != IsVisible())
			SetVisible(visible);
	}

	private bool ShouldDraw() => debugoverlay != null && false;//debugoverlay.GetFirst() != null; //todo

	public override void Paint() {
		if (debugoverlay == null)
			return;

		OverlayText? curText = null;//debugoverlay.GetFirst(); //TODO
		while (curText != null) {
			if (curText.Text[0] != '\0') {
				byte r = (byte)curText.R;
				byte g = (byte)curText.G;
				byte b = (byte)curText.B;
				byte a = (byte)curText.A;
				Vector3 screenPos;
				if (curText.UseOrigin) {
					if (debugoverlay.ScreenPosition(curText.Origin, out screenPos) == 0) {
						float xpos = screenPos.X; ;
						float ypos = screenPos.Y + (curText.LineOffset * 13);
						surface.DrawColoredText(Font!, (int)xpos, (int)ypos, r, g, b, a, curText.Text);
					}
				}
				else {
					if (debugoverlay.ScreenPosition(curText.XPos, curText.YPos, out screenPos) == 0) {
						float xpos = screenPos.X;
						float ypos = screenPos.Y + (curText.LineOffset * 13);
						surface.DrawColoredText(Font!, (int)xpos, (int)ypos, r, g, b, a, curText.Text);
					}
				}
			}
			curText = debugoverlay.GetNext(curText);
		}

		debugoverlay.ClearDeadOverlays();
	}
}

public interface IDebugOverlayPanel
{
	void Create(IPanel parent);
	void Destroy();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	public static IDebugOverlayPanel DebugOverlay;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}

class DebugOverlayPanel : IDebugOverlayPanel
{
	public DebugOverlay? DebugOverlay;
	static DebugOverlayPanel() => IDebugOverlayPanel.DebugOverlay = new DebugOverlayPanel();
	public void Create(IPanel parent) => DebugOverlay = new DebugOverlay((Panel)parent);
	public void Destroy() {
		if (DebugOverlay != null) {
			DebugOverlay.SetParent(null);
			DebugOverlay.MarkForDeletion();
			DebugOverlay = null;
		}
	}
}