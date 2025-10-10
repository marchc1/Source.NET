using Source.Common;
using Source.Common.Engine;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Launcher;
using Source.Common.Utilities;

using System.Globalization;

namespace Source.GUI;

public record struct FontRange
{
	public int Min;
	public int Max;
}

public class Scheme : IScheme
{
	readonly public ISystem System = Singleton<ISystem>();
	readonly public ISurface Surface = Singleton<ISurface>();
	readonly public ISchemeManager SchemeManager = Singleton<ISchemeManager>();
	readonly public IEngineAPI EngineAPI = Singleton<IEngineAPI>();

	IPanel? SizingPanel;
	KeyValues Data;
	KeyValues BaseSettings;
	KeyValues Colors;

	public string fileName;
	public string tag;

	struct FontAlias
	{
		public string TrueFontName;
		public ulong TrueFontSymbol;
		public IFont Font;
		public bool Proportional;
	}

	struct SchemeBorder
	{
		public IBorder? Border;
		public string BorderName;
		public ulong BorderSymbol;
		public bool SharedBorder;
	}
	List<SchemeBorder> BorderList = [];
	IBorder? BaseBorder;
	KeyValues Borders;

	Dictionary<UtlSymbol, FontAlias> FontAliases = [];

	public IBorder? GetBorder(ReadOnlySpan<char> borderName) {
		var hash = borderName.Hash();
		foreach (var b in BorderList) {
			if (b.BorderSymbol == hash)
				return b.Border;
		}
		return null;
	}

	public IBorder? GetBorderAtIndex(int index) {
		if (index < 0)
			return null;
		if (index >= BorderList.Count)
			return null;
		return BorderList[index].Border;
	}

	public int GetBorderCount() => BorderList.Count;
	public IEnumerable<IBorder> GetBorders() {
		foreach (var b in BorderList)
			if (b.Border != null)
				yield return b.Border;
	}

	public KeyValues GetColorData() {
		throw new NotImplementedException();
	}

	public IFont? GetFont(ReadOnlySpan<char> fontName, bool proportional = false) {
		return FindFontInAliasList(GetMungedFontName(fontName, tag, proportional));
	}

	private IFont? FindFontInAliasList(ReadOnlySpan<char> name) {
		if (FontAliases.TryGetValue(name, out FontAlias alias))
			return alias.Font;

		return null;
	}

	public IFont? GetFontAtIndex() {
		throw new NotImplementedException();
	}

	public int GetFontCount() => FontAliases.Count;

	public IEnumerable<IFont> GetFonts() {
		foreach (var fontAlias in FontAliases) {
			var font = fontAlias.Value.Font;
			if (font != null)
				yield return font;
		}
	}

	public ReadOnlySpan<char> GetResourceString(ReadOnlySpan<char> stringName) {
		return BaseSettings.GetString(stringName);
	}

	internal void LoadFromFile(IPanel? sizingPanel, ReadOnlySpan<char> fileName, ReadOnlySpan<char> inTag, KeyValues inKeys) {
		Data = inKeys;
		BaseSettings = Data.FindKey("BaseSettings", true)!;
		Colors = Data.FindKey("Colors", true)!;

		KeyValues name = Data.FindKey("Name", true)!;
		name.SetString("Name", inTag);

		if (!inTag.IsEmpty)
			tag = new(inTag);

		LoadFonts();
		LoadBorders();
	}

	private void LoadBorders() {
		Borders = Data.FindKey("Borders", true)!;

		for (KeyValues? kv = Borders.GetFirstSubKey(); kv != null; kv = kv.GetNextKey()) {
			if (kv.Type != KeyValues.Types.String) {
				IBorder? border = null;
				ReadOnlySpan<char> borderType = kv.GetString("bordertype", null);
				if (!borderType.IsEmpty && borderType.Length > 0) {
					if (borderType.Equals("image", StringComparison.OrdinalIgnoreCase))
						border = new ImageBorder();
					else if (borderType.Equals("scalable_image", StringComparison.OrdinalIgnoreCase))
						border = new ScalableImageBorder();
					else
						Assert(false);
				}

				if (border == null)
					border = new Border();

				border.SetName(kv.Name);
				border.ApplySchemeSettings(this, kv);

				BorderList.Add(new() {
					Border = border,
					SharedBorder = false,
					BorderName = new(kv.Name),
					BorderSymbol = kv.Name.Hash()
				});
			}
		}

		for (KeyValues? kv = Borders.GetFirstSubKey(); kv != null; kv = kv.GetNextKey()) {
			if (kv.Type == KeyValues.Types.String) {
				Border border = (Border)GetBorder(kv.GetString());
				Assert(border != null);

				BorderList.Add(new() {
					Border = border,
					SharedBorder = true,
					BorderName = new(kv.Name),
					BorderSymbol = kv.Name.Hash()
				});
			}
		}

		BaseBorder = GetBorder("BaseBorder");
	}

	private void LoadFonts() {
		Span<char> language = stackalloc char[64];
		System.GetUILanguage(language);
		if (language[0] == '\0')
			"english".CopyTo(language);

		for (var kv = Data.FindKey("CustomFontFiles", true)!.GetFirstSubKey(); kv != null; kv = kv.GetNextKey()) {
			ReadOnlySpan<char> fontFile = kv.GetString();
			if (!fontFile.IsEmpty && fontFile[0] != 0) {
				Surface.AddCustomFontFile(null, fontFile);
			}
			else {
				int rangeMin = 0, rangeMax = 0;
				ReadOnlySpan<char> name = null;
				bool useRange = false;

				for (KeyValues? data = kv.GetFirstSubKey(); data != null; data = data.GetNextKey()) {
					ReadOnlySpan<char> key = data.Name;
					if (key.Equals("font", StringComparison.OrdinalIgnoreCase))
						fontFile = data.GetString();
					else if (key.Equals("name", StringComparison.OrdinalIgnoreCase))
						name = data.GetString();
					else {
						if (key.Equals(language, StringComparison.OrdinalIgnoreCase)) {
							KeyValues? pRange = data.FindKey("range");
							if (pRange != null) {
								useRange = true;
								// Untested since none of the fonts I had loaded at the time used these, sorry if this breaks horribly
								if (!new ScanF(pRange.GetString(), "%x %x").Read(out rangeMin).Read(out rangeMax).Overflowed)
									if (rangeMin > rangeMax)
										(rangeMax, rangeMin) = (rangeMin, rangeMax);
									else
										Warning("ScanF range failure while parsing font range\n");
							}
						}
					}

					if (!fontFile.IsEmpty && fontFile[0] != 0) {
						Surface.AddCustomFontFile(name, fontFile);

						if (useRange)
							SetFontRange(name, rangeMin, rangeMax);
					}
				}
			}
		}

		for (KeyValues? kv = Data.FindKey("BitmapFontFiles", true)!.GetFirstSubKey(); kv != null; kv = kv.GetNextKey()) {
			ReadOnlySpan<char> fontFile = kv.GetString();
			if (!fontFile.IsEmpty && fontFile[0] != 0) {
				bool success = Surface.AddBitmapFontFile(fontFile);
				if (success)
					Surface.SetBitmapFontName(kv.Name, fontFile);
			}
		}

		// create the fonts
		for (KeyValues? kv = Data.FindKey("Fonts", true)!.GetFirstSubKey(); kv != null; kv = kv.GetNextKey()) {
			for (int i = 0; i < 2; i++) {
				// create the base font
				bool proportionalFont = i > 0;
				ReadOnlySpan<char> fontName = GetMungedFontName(kv.Name, tag, proportionalFont);
				IFont font = Surface.CreateFont();

				FontAlias alias = new();
				alias.TrueFontName = new(kv.Name);
				alias.TrueFontSymbol = fontName.Hash();
				alias.Font = font;
				alias.Proportional = proportionalFont;
				FontAliases[fontName] = alias;
			}
		}

		// load in the font glyphs
		ReloadFontGlyphs();
	}

	static char[] mungeBuffer = new char[64];
	private ReadOnlySpan<char> GetMungedFontName(ReadOnlySpan<char> fontName, ReadOnlySpan<char> scheme, bool proportional) {
		memset(mungeBuffer.AsSpan(), '\0');
		if (!scheme.IsEmpty)
			sprintf(mungeBuffer, "%s%s-%s").S(fontName).S(scheme).S(proportional ? "p" : "no");
		else
			sprintf(mungeBuffer, "%s-%s").S(fontName).S(proportional ? "p" : "no");

		return mungeBuffer.AsSpan().SliceNullTerminatedString();
	}

	int ScreenWide, ScreenTall;

	private void ReloadFontGlyphs() {
		if (SizingPanel != null) {
			SizingPanel.GetSize(out ScreenWide, out ScreenTall);
		}
		else {
			Surface.GetScreenSize(out ScreenWide, out ScreenTall);
		}

		int minimumFontHeight = GetMinimumFontHeightForCurrentLanguage();

		KeyValues fonts = Data.FindKey("Fonts", true)!;
		foreach (var kvp in FontAliases) {
			ulong k = kvp.Key;
			FontAlias v = kvp.Value;

			KeyValues kv = fonts.FindKey(v.TrueFontName, true)!;

			for (KeyValues? fontdata = kv.GetFirstSubKey(); fontdata != null; fontdata = fontdata.GetNextKey()) {
				new ScanF(fontdata.GetString(), "%d %d")
					.Read(out int fontYResMin)
					.Read(out int fontYResMax);
				if (fontYResMin > 0) {
					if (fontYResMax == 0)
						fontYResMax = fontYResMin;

					if (ScreenTall < fontYResMin || ScreenTall > fontYResMax)
						continue;
				}

				SurfaceFontFlags flags = 0;
				if (fontdata.GetInt("italic") != 0)
					flags |= SurfaceFontFlags.Italic;
				if (fontdata.GetInt("underline") != 0)
					flags |= SurfaceFontFlags.Underline;
				if (fontdata.GetInt("strikeout") != 0)
					flags |= SurfaceFontFlags.Strikeout;
				if (fontdata.GetInt("symbol") != 0)
					flags |= SurfaceFontFlags.Symbol;
				if (fontdata.GetInt("antialias") != 0 && Surface.SupportsFeature(SurfaceFeature.AntialiasedFonts))
					flags |= SurfaceFontFlags.Antialias;
				if (fontdata.GetInt("dropshadow") != 0 && Surface.SupportsFeature(SurfaceFeature.DropShadowFonts))
					flags |= SurfaceFontFlags.DropShadow;
				if (fontdata.GetInt("outline") != 0 && Surface.SupportsFeature(SurfaceFeature.OutlineFonts))
					flags |= SurfaceFontFlags.Outline;
				if (fontdata.GetInt("custom") != 0)
					flags |= SurfaceFontFlags.Custom;
				if (fontdata.GetInt("bitmap") != 0)
					flags |= SurfaceFontFlags.Bitmap;
				if (fontdata.GetInt("rotary") != 0)
					flags |= SurfaceFontFlags.Rotary;
				if (fontdata.GetInt("additive") != 0)
					flags |= SurfaceFontFlags.Additive;

				int tall = fontdata.GetInt("tall");
				int blur = fontdata.GetInt("blur");
				int scanlines = fontdata.GetInt("scanlines");
				float scalex = fontdata.GetFloat("scalex", 1.0f);
				float scaley = fontdata.GetFloat("scaley", 1.0f);

				// only grow this font if it doesn't have a resolution filter specified
				if ((fontYResMin == 0 && fontYResMax == 0) && v.Proportional) {
					tall = GetProportionalScaledValueEx(tall);
					blur = GetProportionalScaledValueEx(blur);
					scanlines = GetProportionalScaledValueEx(scanlines);
					scalex = GetProportionalScaledValueEx((int)(scalex * 10000.0f)) * 0.0001f;
					scaley = GetProportionalScaledValueEx((int)(scaley * 10000.0f)) * 0.0001f;
				}

				// clip the font size so that fonts can't be too big
				if (tall > 127) {
					tall = 127;
				}

				// check our minimum font height
				if (tall < minimumFontHeight) {
					tall = minimumFontHeight;
				}

				if ((flags & SurfaceFontFlags.Bitmap) != 0) {
					// add the new set
					Surface.SetBitmapFontGlyphSet(
						v.Font,
						Surface.GetBitmapFontName(fontdata.GetString("name")),
						scalex,
						scaley,
						flags);
				}
				else {
					int nRangeMin, nRangeMax;

					if (GetFontRange(fontdata.GetString("name"), out int rangeMin, out int rangeMax)) {
						// add the new set
						Surface.SetFontGlyphSet(
							v.Font,
							fontdata.GetString("name"),
							tall,
							fontdata.GetInt("weight"),
							blur,
							scanlines,
							flags,
							rangeMin,
							rangeMax);
					}
					else {
						// add the new set
						Surface.SetFontGlyphSet(
							v.Font,
							fontdata.GetString("name"),
							tall,
							fontdata.GetInt("weight"),
							blur,
							scanlines,
							flags);
					}
				}

				// don't add any more
				break;
			}
		}
	}

	private bool GetFontRange(ReadOnlySpan<char> font, out int rangeMin, out int rangeMax) {
		if (FontRanges.TryGetValue(font.Hash(), out FontRange range)) {
			rangeMin = range.Min;
			rangeMax = range.Max;
			return true;
		}

		rangeMin = 0;
		rangeMax = 0;
		return false;
	}

	public int GetProportionalScaledValueEx(int normalized) {
		var sizing = SizingPanel;
		if (sizing == null)
			return GetProportionalScaledValue(normalized);

		sizing.GetSize(out int w, out int h);
		return GetProportionalScaledValue_(w, h, normalized);
	}

	public int GetProportionalScaledValue(int normalized) {
		Surface.GetScreenSize(out int wide, out int tall);
		return GetProportionalScaledValue_(wide, tall, normalized);
	}

	public int GetProportionalScaledValue_(int w, int rootTall, int normalized) {
		Surface.GetProportionalBase(out int proW, out int proH);
		float scale = (float)rootTall / proH;

		return (int)(normalized * scale);
	}

	public int GetMinimumFontHeightForCurrentLanguage() {
		Span<char> language = stackalloc char[64];
		System.GetUILanguage(language);
		ReadOnlySpan<char> lang = language.SliceNullTerminatedString();

		if (lang.Equals("korean", StringComparison.OrdinalIgnoreCase)
			|| lang.Equals("tchinese", StringComparison.OrdinalIgnoreCase)
			|| lang.Equals("schinese", StringComparison.OrdinalIgnoreCase)
			|| lang.Equals("japanese", StringComparison.OrdinalIgnoreCase)
		)
			return 13;

		if (lang.Equals("thai", StringComparison.OrdinalIgnoreCase))
			return 18;

		return 0;
	}

	Dictionary<ulong, FontRange> FontRanges = [];

	private void SetFontRange(ReadOnlySpan<char> name, int rangeMin, int rangeMax) {
		ulong symbol = name.Hash();
		FontRanges[symbol] = new() {
			Min = rangeMin,
			Max = rangeMax
		};
	}

	public Color GetColor(ReadOnlySpan<char> colorName, Color defaultColor) {
		ReadOnlySpan<char> schemeValue = LookupSchemeSetting(colorName);
		if (schemeValue.IsEmpty)
			return defaultColor;

		if (new ScanF(schemeValue, "%d %d %d %d").Read(out int r).Read(out int g).Read(out int b).Read(out int a).ReadArguments >= 3)
			return new(r, g, b, a);

		return defaultColor;
	}

	private ReadOnlySpan<char> LookupSchemeSetting(ReadOnlySpan<char> setting) {
		int res = new ScanF(setting, "%d %d %d %d")
			.Read(out int r)
			.Read(out int g)
			.Read(out int b)
			.Read(out int a)
			.ReadArguments;

		if (res >= 3)
			return setting;

		ReadOnlySpan<char> colStr = Colors.GetString(setting, null);
		if (!colStr.IsEmpty)
			return colStr;

		colStr = BaseSettings.GetString(setting, null);
		if (!colStr.IsEmpty)
			return LookupSchemeSetting(colStr);

		return setting;
	}
	public IPanel? GetSizingPanel() => SizingPanel;

	public int GetProportionalNormalizedValue(int scaled) => SchemeManager.GetProportionalNormalizedValue(scaled);
}
