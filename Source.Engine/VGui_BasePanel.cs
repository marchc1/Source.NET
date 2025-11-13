using Source.Common.Commands;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Source.Engine;

public abstract class BasePanel : Panel {
	static ConVar vgui_nav_lock = new("0", FCvar.DevelopmentOnly);
	static ConVar vgui_nav_lock_default_button = new("0", FCvar.DevelopmentOnly);

	public BasePanel(Panel parent) : base(parent) {
		VGui.AddTickSignal(this);
	}
	public abstract bool ShouldDraw();
	public override void OnTick() {
		if (vgui_nav_lock.GetInt() > 0) 
			vgui_nav_lock.SetValue(vgui_nav_lock.GetInt() - 1);

		if (vgui_nav_lock_default_button.GetInt() > 0) 
			vgui_nav_lock_default_button.SetValue(vgui_nav_lock_default_button.GetInt() - 1);

		SetVisible(ShouldDraw());
	}

	protected int DrawColoredText(IFont? font, int x, int y, int r, int g, int b, int a, ReadOnlySpan<char> text) {
		if (text.Length <= 0)
			return x;

		Surface.DrawSetTextFont(font);

		Surface.DrawSetTextPos(x, y);
		Surface.DrawSetTextColor(r, g, b, a);

		int pixels = DrawTextLen(font, text);

		Surface.DrawPrintText(text);

		return x + pixels;
	}

	protected int DrawText(IFont? font, int x, int y, ReadOnlySpan<char> data) {
		int len = DrawColoredText(font,
						   x,
						   y,
						   255,
						   255,
						   255,
						   255,
						   data);
		return len;
	}
	protected int DrawTextLen(IFont? font, ReadOnlySpan<char> text) {
		int len = text.Length;
		int x = 0;

		Surface.DrawSetTextFont(font);

		for (int i = 0; i < len; i++) {
			Surface.GetCharABCwide(font, text[i], out int a, out int b, out int c);

			if (i != 0)
				x += a;
			x += b;
			if (i != len - 1)
				x += c;
		}

		return x;
	}
}
