using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

namespace Source.GUI.Controls;

class URLLabel : Label
{
	string? URL;
	bool Underline;

	public URLLabel(Panel? parent, ReadOnlySpan<char> panelName, ReadOnlySpan<char> text, ReadOnlySpan<char> url) : base(parent, panelName, text) {
		URL = null;
		Underline = false;

		if (!url.IsEmpty && strlen(url) > 0)
			SetURL(url);
	}

	void SetURL(ReadOnlySpan<char> url) => URL = url.ToString();

	public override void OnMousePressed(ButtonCode code) {
		if (code == ButtonCode.MouseLeft && URL != null)
			System.ShellExecute("open", URL);
	}

	public override void ApplySettings(KeyValues resourceData) {
		base.ApplySettings(resourceData);

		ReadOnlySpan<char> url = resourceData.GetString("URLText", null);
		if (!url.IsEmpty && strlen(url) > 0) {
			if (url[0] == '#') {
				ReadOnlySpan<char> localized = Localize.Find(url);
				if (!localized.IsEmpty)
					SetURL(localized);
			}
			else
				SetURL(url);
		}
	}

	public override void GetSettings(KeyValues outResourceData) {
		base.GetSettings(outResourceData);
		if (URL != null)
			outResourceData.SetString("URLText", URL);
	}

	// ReadOnlySpan<char> GetDescription() { }

	public override void ApplySchemeSettings(IScheme scheme) {
		SetFont(scheme.GetFont("DefaultUnderline", IsProportional()));
		base.ApplySchemeSettings(scheme);
		SetCursor(CursorCode.Hand);
	}
}