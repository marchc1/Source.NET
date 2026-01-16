using Microsoft.Extensions.DependencyInjection;

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

	public IImage GetImage(ReadOnlySpan<char> imageName, bool hardwareFiltered) {
		return null!;// TODO todo
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

	public int GetProportionalScaledValueEx(IScheme scheme, int normalizedValue) {
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
		return null;
	}

	public void ReloadFonts() {
		throw new NotImplementedException();
	}

	public void ReloadSchemes() {
		throw new NotImplementedException();
	}

	public void Shutdown(bool full = true) {
		throw new NotImplementedException();
	}
}