using Source;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Game.Client;

public class NavProgress : Frame, IViewPortPanel
{
	IViewPort ViewPort;
	int NumTicks;
	int CurrentTick;
	Label Title;
	Label Text;
	Panel ProgressBarBorder;
	Panel ProgressBar;
	Panel ProgressBarSizer;

	public NavProgress(IViewPort viewPort) : base(null, "nav_progress") {
		ViewPort = viewPort;

		SetScheme(vguiSchemeManager.GetScheme("ClientScheme"));
		SetMoveable(false);
		SetSizeable(false);
		SetProportional(true);

		SetTitleBarVisible(false);

		Title = new(this, "TitleLabel", "");
		Text = new(this, "TextLabel", "");
		Text.SetPaintBackgroundEnabled(false);

		ProgressBarBorder = new(this, "ProgressBarBorder");
		ProgressBar = new(ProgressBarBorder, "ProgressBar");
		ProgressBarSizer = new(ProgressBar, "ProgressBarSizer");

		LoadControlSettings("resource/ui/NavProgress.res");

		Reset();
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetPaintBackgroundType(PaintBackgroundType.Box);

		ProgressBarSizer.SetVisible(false);

		ProgressBarBorder.SetBorder(scheme.GetBorder("ButtonDepressedBorder"));
		ProgressBarBorder.SetBgColor(new Color(0, 0, 0, 0));

		ProgressBar.SetBorder(scheme.GetBorder("ButtonBorder"));
		ProgressBar.SetBgColor(scheme.GetColor("ProgressBar.FgColor", new Color(100, 100, 100, 255)));
	}

	public override void PerformLayout() {
		base.PerformLayout();

		if (NumTicks > 0) {
			int w = ProgressBarBorder.GetWide();
			w *= CurrentTick / NumTicks;
			ProgressBarSizer.SetWide(w);
		}
	}

	public void Init(ReadOnlySpan<char> title, int numTicks, int startTick) {
		Text.SetText(title);

		numTicks = Math.Max(numTicks, 1);
		CurrentTick = Math.Clamp(startTick, 0, numTicks);

		InvalidateLayout();
	}

	public void SetData(KeyValues? data) => Init(data.GetString("msg"), data.GetInt("total"), data.GetInt("current"));

	public void ShowPanel(bool show) {
		if (IsVisible() == show)
			return;

		ViewPort.ShowBackGround(show);

		if (show) {
			Activate();
			SetMouseInputEnabled(false);
		}
		else {
			SetVisible(false);
			SetMouseInputEnabled(false);
		}
	}

	public void Reset() { }
	public void Update() { }
	public bool NeedsUpdate() => false;
	public bool HasInputElements() => false;
	public GameActionSet GetPreferredActionSet() => GameActionSet.None;
}