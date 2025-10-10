using Source.Common.Formats.Keyvalues;

namespace Source.Common.GUI;

public enum Sides
{
	None = -1,
	Left = 0,
	Top = 1,
	Right = 2,
	Bottom = 3
};

public interface IBorder
{
	void Paint(IPanel panel);
	void Paint(int x0, int y0, int x1, int y1);
	void Paint(int x0, int y0, int x1, int y1, Sides breakSide, int breakStart, int breakStop);
	void SetInset(int left, int top, int right, int bottom);
	void GetInset(out int left, out int top, out int right, out int bottom);
	void ApplySchemeSettings(IScheme? scheme, KeyValues inResourceData);
	ReadOnlySpan<char> GetName();
	void SetName(ReadOnlySpan<char> name);

	PaintBackgroundType GetBackgroundType();

	bool PaintFirst();
}
