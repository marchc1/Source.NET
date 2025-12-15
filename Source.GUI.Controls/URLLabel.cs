using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;
using Source.GUI.Controls;

class URLLabel : Label
{
	string URL;
	int URLSize;
	bool Underline;

	public URLLabel(Panel? parent, ReadOnlySpan<char> panelName, ReadOnlySpan<char> text, ReadOnlySpan<char> url) : base(parent, panelName, text) {

	}

	void SetURL(ReadOnlySpan<char> url) { }

	public override void OnMousePressed(ButtonCode code) { }

	public override void ApplySettings(KeyValues resourceData) { }

	public override void GetSettings(KeyValues outResourceData) { }

	// ReadOnlySpan<char> GetDescription() { }

	public override void ApplySchemeSettings(IScheme scheme) { }
}