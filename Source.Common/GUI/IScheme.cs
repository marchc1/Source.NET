using Source.Common.Formats.Keyvalues;

namespace Source.Common.GUI;

public interface IScheme
{
	ReadOnlySpan<char> GetResourceString(ReadOnlySpan<char> stringName);
	IBorder? GetBorder(ReadOnlySpan<char> borderName);
	IFont? GetFont(ReadOnlySpan<char> fontName, bool proportional = false);
	int GetBorderCount();
	IBorder? GetBorderAtIndex(int index);
	int GetFontCount();
	IFont? GetFontAtIndex();
	Color GetColor(ReadOnlySpan<char> colorName, Color defaultColor);

	IEnumerable<IBorder> GetBorders();
	IEnumerable<IFont> GetFonts();

	KeyValues GetColorData();

	int GetProportionalScaledValue(int normalized);
	int GetProportionalNormalizedValue(int scaled);
	int GetProportionalScaledValueEx(int normalizedValue);
}

public interface ISchemeManager
{
	void Init();
	IScheme? LoadSchemeFromFile(ReadOnlySpan<char> fileName, ReadOnlySpan<char> tag);
	void ReloadSchemes();
	void ReloadFonts();
	IScheme GetDefaultScheme();
	IScheme GetScheme(ReadOnlySpan<char> tag);
	IImage? GetImage(ReadOnlySpan<char> imageName, bool hardwareFiltered);
	void Shutdown(bool full = true);
	int GetProportionalScaledValue(int normalized);
	int GetProportionalNormalizedValue(int scaled);

	IScheme? LoadSchemeFromFileEx(IPanel? sizingPanel, ReadOnlySpan<char> fileName, ReadOnlySpan<char> tag);
	bool DeleteImage(ReadOnlySpan<char> imageName);
	int GetProportionalScaledValueEx(IScheme? scheme, int normalizedValue);

	int QuickPropScaleCond(bool scale, IScheme scheme, int normalizedValue) {
		if (!scale)
			return normalizedValue;
		return GetProportionalScaledValueEx(scheme, normalizedValue);
	}
}
