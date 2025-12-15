using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.GUI.Controls;

class RotatingProgressBar : ProgressBar
{
	int TextureId;
	string ImageName;
	float StartRadians;
	float EndRadians;
	float LastAngle;
	float TickDelay;
	float ApproachSpeed;
	float RotOriginX;
	float RotOriginY;
	float RotatingX;
	float RotatingY;
	float RotatingWide;
	float RotatingTall;

	public RotatingProgressBar(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}

	public override void ApplySettings(KeyValues resourceData) { }

	public override void ApplySchemeSettings(IScheme scheme) { }

	void SetImage(ReadOnlySpan<char> imageName) { }

	public override void PaintBackground() { }

	public override void OnTick() { }

	public override void Paint() { }
}