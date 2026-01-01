using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;

namespace Source.GUI.Controls;

class ScalableImagePanel : Panel
{
	int SrcCornerHeight;
	int SrcCornerWidth;
	int CornerHeight;
	int CornerWidth;
	TextureID TextureID;
	float CornerWidthPercent;
	float CornerHeightPercent;
	string? ImageName;
	string? DrawColorName;
	Color DrawColor;

	public ScalableImagePanel(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {
		SrcCornerHeight = 0;
		SrcCornerWidth = 0;

		CornerHeight = 0;
		CornerWidth = 0;

		ImageName = null;
		DrawColorName = null;

		DrawColor = new(255, 255, 255, 255);

		CornerWidthPercent = 0;
		CornerHeightPercent = 0;

		TextureID = Surface.CreateNewTextureID();
	}

	// FIXME #37
	public override void Dispose() {
		base.Dispose();

		if (TextureID != TextureID.INVALID) {
			// Surface.DeleteTextureID(TextureID);
			TextureID = TextureID.INVALID;
		}
	}

	void SetImage(ReadOnlySpan<char> imageName) {
		if (!imageName.IsEmpty) {
			Span<char> image = stackalloc char[MAX_PATH];
			ReadOnlySpan<char> dir = "vgui/";
			sprintf(image, "%s%s").S(dir).S(imageName);

			if (ImageName != null && stricmp(ImageName, image) == 0)
				return;

			ImageName = image.ToString();
		}
		else
			ImageName = null;

		InvalidateLayout();
	}

	public override void PaintBackground() { }

	public override void GetSettings(KeyValues outResourceData) { }

	public override void ApplySettings(KeyValues resourceData) { }

	public override void PerformLayout() { }

	// ReadOnlySpan<char> GetDescription() { }
}