using Source.Common;
using Source.Common.Client;
using Source.Common.GameUI;
using Source.GUI.Controls;

namespace Game.UI;

public class LoadingDialog : Frame
{
	readonly public IGameUI GameUI = Singleton<IGameUI>();
	readonly public ModInfo ModInfo = Singleton<ModInfo>();

	ProgressBar Progress;
	ProgressBar Progress2;
	Label InfoLabel;
	Label TimeRemainingLabel;
	Button CancelButton;
	Panel? LoadingBackground;

	bool ShowingSecondaryProgress;
	float SecondaryProgress;
	double LastSecondaryProgressUpdateTime;
	double SecondaryProgressStartTime;
	bool ShowingVACInfo;
	bool Center;
	bool ConsoleStyle;
	float ProgressFraction;

	[PanelAnimationVar("0")] int AdditionalIndentX;
	[PanelAnimationVar("0")] int AdditionalIndentY;

	public override void PerformLayout() {
		if (Center) {
			MoveToCenterOfScreen();
		}
		else {
			Surface.GetWorkspaceBounds(out int x, out int y, out int screenWide, out int screenTall);
			GetSize(out int wide, out int tall);

			x = screenWide - (wide + 10);
			y = screenTall - (tall + 10);

			x -= AdditionalIndentX;
			y -= AdditionalIndentY;

			SetPos(x, y);
		}

		base.PerformLayout();
		MoveToFront();
	}
	public override void OnClose() {
		HideOtherDialogs(false);
		base.OnClose();
	}
	IEngineClient engine = Singleton<IEngineClient>();
	public override void OnCommand(ReadOnlySpan<char> command) {
		if (command.Equals("Cancel", StringComparison.OrdinalIgnoreCase)) {
			engine.ClientCmd_Unrestricted("disconnect\n");
			Close();
		}
		else
			base.OnCommand(command);
	}
	void Init() {
		SetDeleteSelfOnClose(true);

		SetSize(416, 100);
		SetTitle("#GameUI_Loading", true);

		Center = !GameUI!.HasLoadingBackgroundDialog();

		ShowingSecondaryProgress = false;
		SecondaryProgress = 0.0f;
		LastSecondaryProgressUpdateTime = 0.0f;
		SecondaryProgressStartTime = 0.0f;

		Progress = new ProgressBar(this, "Progress");
		Progress2 = new ProgressBar(this, "Progress2");
		InfoLabel = new Label(this, "InfoLabel", "");
		CancelButton = new Button(this, "CancelButton", "#GameUI_Cancel");
		TimeRemainingLabel = new Label(this, "TimeRemainingLabel", "");
		CancelButton.SetCommand("Cancel");

		if (ModInfo!.IsSinglePlayerOnly() == false && ConsoleStyle == true)
			LoadingBackground = new Panel(this, "LoadingDialogBG");
		else
			LoadingBackground = null;

		SetMinimizeButtonVisible(false);
		SetMaximizeButtonVisible(false);
		SetCloseButtonVisible(false);
		SetSizeable(false);
		SetMoveable(false);

		InfoLabel.SetBounds(20, 32, 392, 24);
		Progress.SetBounds(20, 64, 300, 24);
		CancelButton.SetBounds(330, 64, 72, 24);
		Progress2.SetVisible(false);

		SetupControlSettings(false);
	}
	public LoadingDialog() : base(null, "LoadingDialog") => Init();
	public LoadingDialog(Panel? parent) : base(parent, "LoadingDialog") => Init();


	private void SetupControlSettings(bool forceShowProgressText) {
		ShowingVACInfo = false;

		if (ModInfo.IsSinglePlayerOnly() && !forceShowProgressText)
			LoadControlSettings("resource/LoadingDialogNoBannerSingle.res");
		else
			LoadControlSettings("resource/LoadingDialogNoBanner.res");
	}

	internal void DisplayGenericError(ReadOnlySpan<char> failureReason, ReadOnlySpan<char> extendedReason) {
		Activate();

		SetupControlSettingsForErrorDisplay("resource/LoadingDialogError.res");

		if (!extendedReason.IsEmpty && extendedReason.Length > 0) {
			ReadOnlySpan<char> fail = failureReason[0] == '#' ? Localize.Find(failureReason) : failureReason;
			ReadOnlySpan<char> ext = extendedReason[0] == '#' ? Localize.Find(extendedReason) : extendedReason;

			InfoLabel.SetText(string.Concat(fail, ext));
		}
		else
			InfoLabel.SetText(failureReason.Trim('\n'));

		InfoLabel.GetContentSize(out int wide, out int tall);
		InfoLabel.GetPos(out int x, out int y);
		SetTall(tall + y + 50);

		CancelButton.GetPos(out int buttonX, out int buttonY);
		CancelButton.SetPos(buttonX, tall + y + 6);
		CancelButton.RequestFocus();

		InfoLabel.InvalidateLayout();
		SetSizeable(true);
	}

	private void SetupControlSettingsForErrorDisplay(ReadOnlySpan<char> settingsFile) {
		Center = true;
		SetTitle("#GameUI_Disconnected", true);
		LoadControlSettings(settingsFile);
		HideOtherDialogs(true);

		base.Activate();

		Progress.SetVisible(false);
		InfoLabel.SetVisible(true);
		CancelButton.SetText("#GameUI_Close");
		CancelButton.SetCommand("Close");

		InfoLabel.InvalidateLayout();
	}

	internal void Open() {
		SetTitle("#GameUI_Loading", true);

		HideOtherDialogs(true);
		base.Activate();

		Progress.SetVisible(true);
		if (ModInfo.IsSinglePlayerOnly())
			InfoLabel.SetVisible(true);

		CancelButton.SetText("#GameUI_Cancel");
		CancelButton.SetCommand("Cancel");
	}

	private void HideOtherDialogs(bool hide) {
		if (hide) {
			if (GameUI.HasLoadingBackgroundDialog()) {
				GameUI.ShowLoadingBackgroundDialog();
				MoveToFront();
				Input.SetAppModalSurface(this);
			}
			else
				Surface.RestrictPaintToSinglePanel(this);
		}
		else {
			if (GameUI.HasLoadingBackgroundDialog()) {
				GameUI.HideLoadingBackgroundDialog();
				Input.SetAppModalSurface(null);
			}
			else
				Surface.RestrictPaintToSinglePanel(null);
		}
	}

	internal bool SetProgressPoint(float progress) {
		if (!ShowingVACInfo)
			SetupControlSettings(false);


		int nOldDrawnSegments = Progress.GetDrawnSegmentCount();
		Progress.SetProgress(progress);
		int nNewDrawSegments = Progress.GetDrawnSegmentCount();
		return nOldDrawnSegments != nNewDrawSegments;
	}

	internal void SetStatusText(ReadOnlySpan<char> statusText) {
		InfoLabel.SetText(statusText);
	}
}
