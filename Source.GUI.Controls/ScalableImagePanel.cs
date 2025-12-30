using Source.Common.Formats.Keyvalues;

namespace Source.GUI.Controls;

class ScalableImagePanel : Panel
{
	int SrcCornerHeight;
	int SrcCornerWidth;
	int CornerHeight;
	int CornerWidth;
	int TextureID;
	float CornerWidthPercent;
	float CornerHeightPercent;
	string ImageName;
	string DrawColorName;
	Color DrawColor;

	public ScalableImagePanel(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {

	}

	void SetImage(ReadOnlySpan<char> imageName) { }

	public override void PaintBackground() { }

	public override void GetSettings(KeyValues outResourceData) { }

	public override void ApplySettings(KeyValues resourceData) { }

	public override void PerformLayout() { }

	// ReadOnlySpan<char> GetDescription() { }
}