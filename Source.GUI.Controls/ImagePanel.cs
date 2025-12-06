using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;

namespace Source.GUI.Controls;

public class ImagePanel : Panel
{
	public static Panel Create_ImagePanel() => new ImagePanel(null, null);

	IImage? Image;
	string? ImageName;
	string? FillColorName;
	string? DrawColorName;
	bool PositionImage;
	bool CenterImage;
	bool ScaleImage;
	bool TileImage;
	bool TileHorizontally;
	bool TileVertically;
	float ScaleAmount;
	Color FillColor;
	Color DrawColor;
	IImageRotation Rotation;
	public ImagePanel(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {
		Image = null;
		ImageName = null;
		FillColorName = null;
		DrawColorName = null;
		PositionImage = false;
		CenterImage = false;
		ScaleImage = false;
		TileImage = false;
		TileHorizontally = false;
		TileVertically = false;
		ScaleAmount = 0.0f;
		FillColor = new(0, 0, 0, 0);
		DrawColor = new(255, 255, 255, 255);
		Rotation = IImageRotation.Unrotated;

		SetImage(Image);
	}

	public void SetImage(IImage? image) {
		Image = image;
		Repaint();
	}

	public void SetImage(ReadOnlySpan<char> imageName) {
		if (!imageName.IsEmpty && streq(imageName, ImageName))
			return;

		ImageName = new(imageName);
		InvalidateLayout(false, true);
	}

	public IImage? GetImage() => Image;
	public Color GetDrawColor() => DrawColor;
	public void SetDrawColor(Color drawColor) => DrawColor = drawColor;

	public override void PaintBackground() {

	}

	public override void GetSettings(KeyValues outResourceData) {
		base.GetSettings(outResourceData);
		//
	}

	public override void ApplySettings(KeyValues resourceData) {
		base.ApplySettings(resourceData);
		//
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		//
	}

	public void SetShouldScaleImage(bool scale) => ScaleImage = scale;
	public bool GetShouldScaleImage() => ScaleImage;

	public void SetScaleAmount(float scale) => ScaleAmount = scale;
	public float GetScaleAmount() => ScaleAmount;

	public void SetFillColor(Color color) => FillColor = color;
	public Color GetFillColor() => FillColor;

	public ReadOnlySpan<char> GetImageName() => ImageName;

	public bool EvictImage() {
		return false; // todo
	}

	public int GetNumFrames() {
		return 0; // todo
	}

	public void SetFrame(int frame) {

	}
}
