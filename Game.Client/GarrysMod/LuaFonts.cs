using Source.Common.GUI;
using Source.GUI.Controls;

namespace Game.Client.GarrysMod;

public struct LuaFont
{
	public string? Name;
	public string? Font;
	public bool Extended;
	public float Size;
	public int Weight;
	public SurfaceFontFlags Flags;
	public int BlurSize;
	public int Scanlines;
	public IFont? Handle;
}

public static class LuaFonts
{
	static readonly SortedDictionary<string, LuaFont> Fonts = new(StringComparer.Ordinal);

	public static bool IsLuaFont(ReadOnlySpan<char> name) => Fonts.ContainsKey(new string(name));

	public static LuaFont? FindFont(IFont handle) {
		foreach (LuaFont font in Fonts.Values)
			if (font.Handle == handle)
				return font;
		return null;
	}

	public static void CreateFont(ReadOnlySpan<char> name, ReadOnlySpan<char> fontName, bool extended = false, float size = 13, float weight = 500, float blurSize = 0, float scanlines = 0, bool antialias = true, bool underline = false, bool italic = false, bool strikeout = false, bool symbol = false, bool rotary = false, bool shadow = false, bool additive = false, bool outline = false) {
		if (strlen(name) >= 32) {
			// todo: lua arg error "font name is too long"
			return;
		}

		if (IsLuaFont(fontName)) {
			DevWarning($"Tried to create font '{name}' from a game font '{fontName}', not supported!\n");
			fontName = "Tahoma";
		}

		Panel basePanel = GModBase.GetGModBasePanel(true)!;
		IFont? schemeFont = basePanel.GetScheme()!.GetFont(fontName);
		if (schemeFont != null && stricmp(Surface.GetFontName(schemeFont), fontName) != 0) {
			DevWarning($"Tried to create font '{name}' from a game font '{fontName}', not supported!\n");
			fontName = "Tahoma";
		}

		string key = new(name);
		if (!Fonts.TryGetValue(key, out LuaFont font))
			font.Handle = Surface.CreateFont();

		font.Name = key;
		font.Font = new string(fontName);
		font.Extended = extended;
		font.Size = size;
		font.Weight = (int)weight;
		font.BlurSize = (int)blurSize;
		font.Flags = 0;
		font.Scanlines = (int)scanlines;

		if (font.Size > 255)
			font.Size = 255;
		else if (font.Size < 4)
			font.Size = 4;

		if (font.BlurSize > 80)
			font.BlurSize = 80;
		else if (font.BlurSize < 0)
			font.BlurSize = 0;

		if (antialias)
			font.Flags |= SurfaceFontFlags.Antialias;
		if (underline)
			font.Flags |= SurfaceFontFlags.Underline;
		if (italic)
			font.Flags |= SurfaceFontFlags.Italic;
		if (strikeout)
			font.Flags |= SurfaceFontFlags.Strikeout;
		if (symbol)
			font.Flags |= SurfaceFontFlags.Symbol;
		if (rotary)
			font.Flags |= SurfaceFontFlags.Rotary;
		if (shadow)
			font.Flags |= SurfaceFontFlags.DropShadow;
		if (additive)
			font.Flags |= SurfaceFontFlags.Additive;
		if (outline)
			font.Flags |= SurfaceFontFlags.Outline;


		Fonts[key] = font;

		if (!Surface.SetFontGlyphSet(font.Handle!, font.Font, (int)font.Size, font.Weight, font.BlurSize, font.Scanlines, font.Flags)) {
			// todo: lua Msg($"Failed to create font '{font.Name}' from '{font.Font}'!\n")
			return;
		}

		Surface.ClearTemporaryFontCache();
	}

	public static void RecreateFonts() {
		foreach (LuaFont font in Fonts.Values)
			Surface.SetFontGlyphSet(font.Handle!, font.Font, (int)font.Size, font.Weight, font.BlurSize, font.Scanlines, font.Flags);
	}
}
