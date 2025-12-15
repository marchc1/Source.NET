using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.GUI.Controls;

class AnimatingImagePanel : Panel
{
	int CurrentImage;
	int NextFrameTime;
	int FrameTimeMillis;
	List<IImage> Frames;
	string ImageName;
	bool Animating;
	bool Filtered;
	bool ScaleImage;

	public AnimatingImagePanel(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {

	}

	public override void PerformLayout() { }

	void AddImage(IImage image) { }

	void LoadAnimation(ReadOnlySpan<char> baseName, int frameCount) { }

	public override void PaintBackground() { }

	public override void OnTick() { }

	public override void GetSettings(KeyValues outResourceData) { }

	public override void ApplySettings(KeyValues resourceData) { }

	// ReadOnlySpan<char> GetDescription() { }

	void StartAnimation() { }

	void StopAnimation() { }

	void ResetAnimation(int frame) { }
}