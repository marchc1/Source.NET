using Source;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.GUI.Controls;

enum ProgressTextures
{
	ProgressTextureFg,
	ProgressTextureBg,
	NumProgressTextures
}

class CircularProgressBar : ProgressBar
{
	enum CircularProgressDir
	{
		Clockwise,
		CounterClockwise
	}

	int ProgressDirection;
	int StartSegment;
	bool ReverseProgress;

	int[] TextureIds = new int[(int)ProgressTextures.NumProgressTextures];
	string[] ImageName = new string[(int)ProgressTextures.NumProgressTextures];
	int[] LenImageName = new int[(int)ProgressTextures.NumProgressTextures];

	public CircularProgressBar(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {

	}

	public override void ApplySettings(KeyValues resourceData) { }

	public override void ApplySchemeSettings(IScheme scheme) { }

	void SetImage(ReadOnlySpan<char> imageName, ProgressTextures pos) { }

	public override void PaintBackground() { }

	public override void Paint() { }

	void DrawCircleSegment(Color color, float endProgress, bool clockwise) { }
}