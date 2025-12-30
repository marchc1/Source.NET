using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;

namespace Source.GUI.Controls;

class AnimatingImagePanel : Panel
{
	int CurrentImage;
	long NextFrameTime;
	long FrameTimeMillis;
	List<IImage> Frames = [];
	string? ImageName;
	bool Animating;
	bool Filtered;
	bool ScaleImage;

	public AnimatingImagePanel(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {
		CurrentImage = 0;
		FrameTimeMillis = 100;
		NextFrameTime = 0;
		ImageName = null;
		Filtered = false;
		ScaleImage = false;
		Animating = false;
		VGui.AddTickSignal(this);
	}

	public override void PerformLayout() {
		base.PerformLayout();
		Repaint();
	}

	void AddImage(IImage image) {
		Frames.Add(image);

		if (!ScaleImage && image != null) {
			image.GetSize(out int wide, out int tall);
			SetSize(wide, tall);
		}
	}

	void LoadAnimation(ReadOnlySpan<char> baseName, int frameCount) {
		Frames.Clear();

		Span<char> imageName = stackalloc char[512];
		for (int i = 0; i < frameCount; i++) {
			imageName.Clear();
			sprintf(imageName, "%s%d").S(baseName).D(i);
			AddImage(SchemeManager.GetImage(imageName, Filtered));
		}
	}

	public override void PaintBackground() {
		if (CurrentImage < 0 || CurrentImage >= Frames.Count || Frames[CurrentImage] == null)
			return;

		IImage image = Frames[CurrentImage];
		Surface.DrawSetColor(255, 255, 255, 255);
		image.SetPos(0, 0);

		if (ScaleImage) {
			image.GetSize(out int imageWide, out int imageTall);
			GetSize(out int wide, out int tall);
			image.SetSize(wide, tall);
			image.SetColor(new(255, 255, 255, 255));
			image.Paint();
			image.SetSize(imageWide, imageTall);
		}
		else
			image.Paint();
	}

	public override void OnTick() {
		if (!Animating || System.GetTimeMillis() < NextFrameTime)
			return;

		NextFrameTime = System.GetTimeMillis() + FrameTimeMillis;
		CurrentImage++;
		if (CurrentImage >= Frames.Count)
			CurrentImage = 0;
		Repaint();
	}

	public override void GetSettings(KeyValues outResourceData) {
		base.GetSettings(outResourceData);
		if (ImageName != null)
			outResourceData.SetString("image", ImageName);
	}

	public override void ApplySettings(KeyValues resourceData) {
		base.ApplySettings(resourceData);

		ReadOnlySpan<char> imageName = resourceData.GetString("image");
		if (!imageName.IsEmpty) {
			ScaleImage = resourceData.GetInt("scaleImage", 0) != 0;
			Span<char> imgName = stackalloc char[(int)strlen(imageName) + 1];
			strcpy(imgName, imageName);

			LoadAnimation(imgName, resourceData.GetInt("frames"));
		}

		FrameTimeMillis = resourceData.GetInt("anim_framerate", 100);
	}

	// ReadOnlySpan<char> GetDescription() { }

	void StartAnimation() => Animating = true;

	void StopAnimation() => Animating = false;

	void ResetAnimation(int frame) {
		if (frame >= 0 && frame < Frames.Count)
			CurrentImage = frame;
		else
			CurrentImage = 0;
		Repaint();
	}
}