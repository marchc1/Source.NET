using Source.Common.GUI;
using Source.GUI.Controls;

namespace Source.Engine;

public abstract class BasePanel : Panel {
	public abstract bool ShouldDraw();
	public abstract override void OnTick();

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
