using Source;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.GUI.Controls;

class BitmapImagePanel : Panel
{
	Alignment ContentAlignment;
	bool PrserveAspectRatio;
	bool HardwareFiltered;
	IImage Image;
	Color BgColor;
	string ImageName;
	string ColorName;

	public BitmapImagePanel(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {

	}

	void ComputeImagePosition(int x, int y, int w, int h) { }

	public override void PaintBorder() { }

	public override void PaintBackground() { }

	void SetTexture(ReadOnlySpan<char> filename, bool hardwareFiltered) { }

	void SetImageColor(Color color) => BgColor = color;

	void SetContentAlignment(Alignment alignment) { }

	public override void GetSettings(KeyValues outResourceData) { }

	public override void ApplySettings(KeyValues resourceData) { }

	public override void ApplySchemeSettings(IScheme scheme) { }

	// ReadOnlySpan<char> GetDescription() { }
}