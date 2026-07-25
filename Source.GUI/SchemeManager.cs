using Microsoft.Extensions.DependencyInjection;

using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;

using System.Diagnostics.CodeAnalysis;

namespace Source.GUI;

public class SchemeManager : ISchemeManager
{
	readonly IFileSystem fileSystem;
	readonly IServiceProvider services;
	ISurface? surface;

	[ConCommand("vgui_spew_fonts")]
	void vgui_spew_fonts(){
		SpewFonts();
	}

	public void SpewFonts(){
		foreach (var s in Schemes)
			s.SpewFonts();
	}

	public SchemeManager(IFileSystem fileSystem, IServiceProvider services) {
		this.services = services;
		this.fileSystem = fileSystem;
	}

	[MemberNotNull(nameof(surface))]
	public void ValidateSurface() {
		surface ??= services.GetRequiredService<ISurface>();
	}


	public void Init() {
		Schemes.Add(new Scheme());
	}

	public bool DeleteImage(ReadOnlySpan<char> imageName) {
		throw new NotImplementedException();
	}

	public IScheme GetDefaultScheme() {
		return Schemes[0];
	}

	bool initializedFirstScheme;
	struct CachedBitmapHandle
	{
		public Bitmap? Bitmap;
		public bool IsProportional;
	}

	readonly List<CachedBitmapHandle> Bitmaps = [];

	static string? searchString;

	public IImage? GetImage(ReadOnlySpan<char> imageName, bool hardwareFiltered) {
		return GetImage(imageName, hardwareFiltered, false);
	}

	public IImage? GetImage(ReadOnlySpan<char> imageName, bool hardwareFiltered, bool proportional) {
		if (imageName.IsEmpty)
			return null;

		CachedBitmapHandle searchBitmap = new() { Bitmap = null };

		Span<char> fileName = stackalloc char[MAX_PATH];
		int len;
		if (imageName.IndexOf(".pic", StringComparison.OrdinalIgnoreCase) != -1)
			len = sprintf(fileName, "%s").S(imageName);
		else
			len = sprintf(fileName, "vgui/%s").S(imageName);
		fileName = fileName[..len];

		searchString = new(fileName);
		int i = Bitmaps.FindIndex(b => !BitmapHandleSearchFunc(b, searchBitmap) && !BitmapHandleSearchFunc(searchBitmap, b));
		if (i >= 0)
			return Bitmaps[i].Bitmap;

		Bitmap bitmap = new(fileName, hardwareFiltered);
		if (proportional) {
			bitmap.GetSize(out int wide, out int tall);
			bitmap.SetSize(GetProportionalScaledValue(wide), GetProportionalScaledValue(tall));
		}
		CachedBitmapHandle hBitmap = new() { Bitmap = bitmap, IsProportional = proportional };
		Bitmaps.Add(hBitmap);
		return hBitmap.Bitmap;
	}

	static bool BitmapHandleSearchFunc(CachedBitmapHandle lhs, CachedBitmapHandle rhs) {
		if (lhs.Bitmap != null && rhs.Bitmap != null)
			return stricmp(lhs.Bitmap.GetName(), rhs.Bitmap.GetName()) > 0 && lhs.IsProportional && !rhs.IsProportional;
		else if (lhs.Bitmap != null)
			return stricmp(lhs.Bitmap.GetName(), searchString) > 0;
		return stricmp(searchString, rhs.Bitmap!.GetName()) > 0;
	}

	public int GetProportionalNormalizedValue(int scaled) {
		ValidateSurface();
		surface.GetScreenSize(out int wide, out int tall);
		return GetProportionalNormalizedValue_(wide, tall, scaled);
	}

	private int GetProportionalNormalizedValue_(int _, int rootTall, int scaled) {
		ValidateSurface();
		surface.GetProportionalBase(out int proW, out int proH);
		float scale = (float)rootTall / proH;

		return (int)(scaled / scale);
	}

	public int GetProportionalScaledValue(int normalized) {
		ValidateSurface();
		surface.GetScreenSize(out int wide, out int tall);
		return GetProportionalScaledValue_(wide, tall, normalized);
	}

	private int GetProportionalScaledValue_(int _, int rootTall, int normalized) {
		ValidateSurface();
		surface.GetProportionalBase(out int proW, out int proH);
		float scale = (float)rootTall / proH;

		return (int)(normalized * scale);
	}

	public IScheme GetScheme(ReadOnlySpan<char> tag) {
		ulong tagHash = tag.Hash();
		foreach (var scheme in Schemes)
			if (scheme.tag.Hash() == tagHash)
				return scheme;

		return Schemes.First();
	}

	public IScheme? LoadSchemeFromFile(ReadOnlySpan<char> fileName, ReadOnlySpan<char> tag) {
		return LoadSchemeFromFileEx(null, fileName, tag);
	}

	public int GetProportionalScaledValueEx(IScheme? scheme, int normalizedValue) {
		if (scheme == null) {
			Assert(false);
			return GetProportionalScaledValue(normalizedValue);
		}

		Scheme? p = (Scheme?)scheme;
		if (p == null)
			throw new Exception();

		IPanel sizing = p.GetSizingPanel();
		if (sizing == null)
			return GetProportionalScaledValue(normalizedValue);

		sizing.GetSize(out int w, out int h);
		return GetProportionalScaledValue_(w, h, normalizedValue);
	}

	public IScheme? LoadSchemeFromFileEx(IPanel? sizingPanel, ReadOnlySpan<char> fileName, ReadOnlySpan<char> tag) {
		IScheme? scheme = FindLoadedScheme(fileName);
		if (scheme != null) {
			((Scheme)scheme).ReloadFontGlyphs();
			return scheme;
		}

		KeyValues? data = new("Scheme");
		data.UsesEscapeSequences(true);

		bool result = data.LoadFromFile(fileSystem, fileName, "GAME");
		if (!result)
			result = data.LoadFromFile(fileSystem, fileName, null);

		if (!result) {
			data = null;
			return null;
		}

		Scheme newScheme = initializedFirstScheme ? new Scheme() : Schemes[0];
		newScheme.LoadFromFile(sizingPanel, fileName, tag, data);
		if (initializedFirstScheme)
			Schemes.Add(newScheme);
		initializedFirstScheme = true;

		return newScheme;
	}

	readonly List<Scheme> Schemes = [];

	private IScheme? FindLoadedScheme(ReadOnlySpan<char> fileName) {
		for (int i = 0; i < Schemes.Count; i++) {
			ReadOnlySpan<char> schemeFileName = Schemes[i].GetFileName();
			if (stricmp(schemeFileName, fileName) == 0)
				return Schemes[i];
		}

		return null;
	}

	public void ReloadFonts() {
		foreach (Scheme scheme in Schemes)
			scheme.ReloadFontGlyphs();
	}

	public void ReloadSchemes() {
		int count = Schemes.Count;
		Shutdown(false);

		for (int i = 0; i < count; i++) {
			Scheme scheme = Schemes[i];
			LoadSchemeFromFile(scheme.fileName, scheme.tag);
		}
	}

	public void Shutdown(bool full = true) {
		// todo
	}
}
