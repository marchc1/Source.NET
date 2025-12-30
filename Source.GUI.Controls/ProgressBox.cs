using Source.Common.GUI;

namespace Source.GUI.Controls;

class ProgressBox : Frame
{
	Label MessageLabel;
	ProgressBar ProgressBar;
	Button CancelButton;
	string TitleString;
	string InfoString;
	string UnknownTimeString;
	float FirstProgressUpdate;
	float LastProgressUpdate;
	float CurrentProgress;

	public ProgressBox(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {

	}

	void Init() { }

	public override void ApplySchemeSettings(IScheme scheme) { }

	void DoModal(Frame frameOver) { }

	void ShowWindow(Frame frameOver) { }

	public override void PerformLayout() { }

	void SetProgress(float progress) { }

	void SetText(ReadOnlySpan<char> text) { }

	void UpdateTitle() { }

	public override void OnThink() { }

	public override void OnTick() { }

	public override void OnCommand(ReadOnlySpan<char> command) { }

	void OnCloseFrameButtonPressed() { }

	public override void OnClose() { }

	void OnShutdownRequest() { }

	void OnCancel() { }

	void SetCancelButtonVisible(bool state) { }

	void SetCancelButtonEnabled(bool state) { }
}