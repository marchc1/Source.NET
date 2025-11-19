using Source.Common.GUI;

using System.Drawing;

namespace Source.GUI.Controls;

public class Image : IImage
{
	readonly public ISurface Surface = Singleton<ISurface>();

	int X, Y, Width, Height;
	Color Color;

	public virtual void GetContentSize(out int wide, out int tall) {
		GetSize(out wide, out tall);
	}

	public Color GetColor() => Color;

	public void GetPos(out int x, out int y) {
		x = X;
		y = Y;
	}
	public virtual void GetSize(out int wide, out int tall) {
		wide = Width;
		tall = Height;
	}

	void IImage.Paint() => Paint();

	public virtual void SetColor(Color color) {
		Color = color;
		DrawSetTextColor(color);
	}

	public virtual void SetPos(int x, int y) {
		X = x;
		Y = y;
	}

	void IImage.SetSize(int wide, int tall) => SetSize(wide, tall);

	public virtual void SetSize(int wide, int tall) {
		Width = wide;
		Height = tall;
	}
	protected virtual void DrawSetColor(Color color) => Surface.DrawSetColor(color.R, color.G, color.B, color.A);
	protected virtual void DrawSetColor(int r, int g, int b, int a) => Surface.DrawSetColor(r, g, b, a);
	protected virtual void DrawFilledRect(int x0, int y0, int x1, int y1) {
		x0 += X;
		y0 += Y;
		x1 += X;
		y1 += Y;
		Surface.DrawFilledRect(x0, y0, x1, y1);
	}
	protected virtual void DrawOutlinedRect(int x0, int y0, int x1, int y1) {
		x0 += X;
		y0 += Y;
		x1 += X;
		y1 += Y;
		Surface.DrawOutlinedRect(x0, y0, x1, y1);
	}
	protected virtual void DrawLine(int x0, int y0, int x1, int y1) {
		x0 += X;
		y0 += Y;
		x1 += X;
		y1 += Y;
		Surface.DrawLine(x0, y0, x1, y1);
	}
	protected virtual void DrawPolyLine(Span<Point> positions) {
		for (int i = 0; i < positions.Length; i++) {
			ref Point p = ref positions[i];
			positions[i] = new(p.X + X, p.Y + Y);
		}
		Surface.DrawPolyLine(positions);
	}
	protected virtual void DrawSetTextFont(IFont font) {
		Surface.DrawSetTextFont(font);
	}
	protected virtual void DrawSetTextColor(Color color) => Surface.DrawSetTextColor(color.R, color.G, color.B, color.A);
	protected virtual void DrawSetTextColor(int r, int g, int b, int a) => Surface.DrawSetTextColor(r, g, b, a);
	protected virtual void DrawSetTextPos(int x, int y) => Surface.DrawSetTextPos(x + X, y + Y);
	protected virtual void DrawPrintText(ReadOnlySpan<char> str) => Surface.DrawPrintText(str);
	protected virtual void DrawPrintText(int x, int y, ReadOnlySpan<char> str) {
		Surface.DrawSetTextPos(x + X, y + Y);
		Surface.DrawPrintText(str);
	}
	protected virtual void DrawPrintChar(char c) {
		Surface.DrawChar(c);
	}
	protected virtual void DrawPrintChar(int x, int y, char c) {
		Surface.DrawSetTextPos(x + X, y + Y);
		Surface.DrawChar(c);
	}
	protected virtual void DrawSetTexture(in TextureID texID) {
		Surface.DrawSetTexture(in texID);
	}
	protected virtual void DrawTexturedRect(int x0, int y0, int x1, int y1) {
		x0 += X;
		y0 += Y;
		x1 += X;
		y1 += Y;
		Surface.DrawTexturedRect(x0, y0, x1, y1);
	}
	public virtual void Paint() {

	}
}
