using Source.Common.GUI;
using Source.Common.Input;
using Source.Engine;
using Source.GUI.Controls;

namespace Game.Client.GarrysMod;

public class GModBase : Panel
{
	static GModBase() => ChainToAnimationMap<GModBase>();

	static Panel? BasePanel;

	bool FirstThink;

	public static Panel? GetGModBasePanel(bool create) {
		if (BasePanel == null && create)
			BasePanel = new GModBase("GModBasePanel");

		return BasePanel;
	}

	public GModBase(ReadOnlySpan<char> panelName) : base(null, panelName) {
		SetParent(enginevgui.GetPanel(VGuiPanelType.Root));
		SetScheme(SchemeManager.LoadSchemeFromFileEx(enginevgui.GetPanel(VGuiPanelType.ClientDll), "resource/ClientScheme.res", "ClientScheme")!);
		SetProportional(false);
		SetMouseInputEnabled(true);
		SetKeyboardInputEnabled(true);
		SetVisible(true);
		SetSize(ScreenWidth(), ScreenHeight());
		SetPos(0, 0);
		base.OnScreenSizeChanged(0, 0);
		FirstThink = true;
		SetZPos(90);
	}

	public override void Think() {
		if (FirstThink) {
			LuaFonts.RecreateFonts();
			FirstThink = false;
		}
	}

	public override void OnMousePressed(ButtonCode code) {
		if (engine.IsPaused())
			return;

		// todo: GUIMousePressed hook
	}

	public override void OnMouseDoublePressed(ButtonCode code) {
		if (engine.IsPaused())
			return;

		// todo: GUIMouseDoublePressed hook
	}

	public override void OnMouseReleased(ButtonCode code) {
		if (engine.IsPaused())
			return;

		// todo: GUIMouseReleased hook
	}

	public override void OnScreenSizeChanged(int oldWide, int oldTall) {
		SetSize(ScreenWidth(), ScreenHeight());
		SetPos(0, 0);
		base.OnScreenSizeChanged(oldWide, oldTall);
		FirstThink = true;

		if (oldWide != 0) {
			// todo: OnScreenSizeChanged hook
		}
	}
}
