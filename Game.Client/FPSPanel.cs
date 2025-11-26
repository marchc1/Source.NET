using System.Numerics;

using Source;
using Source.Common.Commands;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Game.Client;

public class FPSPanel : Panel
{
	static readonly ConVar cl_showfps = new("0", 0, "Draw fps meter at top of screen (1 = fps, 2 = smooth fps)");
	static readonly ConVar cl_showpos = new("0", 0, "Draw current position at top of screen");
	static readonly ConVar cl_showbattery = new("0", 0, "Draw current battery level at top of screen when on battery power");

	private void ComputeSize() {
		GetParent()!.GetSize(out int wide, out int tall);
		int x = wide - FPS_PANEL_WIDTH;
		int y = 0;
		SetPos(x, y);
		SetSize(FPS_PANEL_WIDTH, 4 * Surface.GetFontTall(Font) + 8);
	}
	private void InitAverages() {
		AverageFPS = -1;
		LastRealTime = -1;
		High = -1;
		Low = -1;
	}

	IFont? Font;
	double AverageFPS;
	double LastRealTime;
	int High;
	int Low;
	bool LastDraw;
	int BatteryPercent;
	float LastBatteryPercent;

	public const int FPS_PANEL_WIDTH = 300;

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		Font = scheme.GetFont("DefaultFixedOutline");
		Assert(Font != null);
		ComputeSize();
	}
	public override void Paint() {
		int i = 0;
		int x = 2;

		double realFrameTime = gpGlobals.RealTime - LastRealTime;

		if (cl_showfps.GetInt() != 0 && realFrameTime > 0.0) {
			if (LastRealTime != -1.0) {
				i++;
				int fps = -1;
				Color color;
				if (cl_showfps.GetInt() == 2) {
					const double NewWeight = 0.1;
					double newFrame = 1.0 / realFrameTime;

					if (AverageFPS < 0.0f) {
						AverageFPS = newFrame;
						High = (int)(long)AverageFPS;
						Low = (int)(long)AverageFPS;
					}
					else {
						AverageFPS *= 1.0f - NewWeight;
						AverageFPS += newFrame * NewWeight;
					}

					int newFrameInt = (int)newFrame;
					if (newFrameInt < Low) Low = newFrameInt;
					if (newFrameInt > High) High = newFrameInt;

					fps = (int)(long)AverageFPS;
					double frameMS = realFrameTime * 1000.0;
					GetFPSColor(fps, out color);
					Surface.DrawColoredText(Font, x, 2, color[0], color[1], color[2], 255, $"{fps} fps ({Low}, {High}) {frameMS:F1} ms on {engine.GetLevelName()}");
				}
				else {
					AverageFPS = -1;
					fps = (int)(long)(1.0 / realFrameTime);
					GetFPSColor(fps, out color);
					Surface.DrawColoredText(Font, x, 2, color[0], color[1], color[2], 255, $"{fps} fps on {engine.GetLevelName()}");
				}
			}
		}

		LastRealTime = gpGlobals.RealTime;

		int fontTall = Surface.GetFontTall(Font);

		if (cl_showpos.GetInt() != 0) {
			C_BasePlayer? localplayer = C_BasePlayer.GetLocalPlayer();
			if (localplayer != null) {
				Vector3 pos = MainViewOrigin();
				Vector3 ang = MainViewAngles();
				Vector3 vel = localplayer.Velocity;
				Surface.DrawColoredText(Font, x, 2 + i * fontTall, 255, 255, 255, 255, $"pos: {pos.X:F2} {pos.Y:F2} {pos.Z:F2}");
				i++;
				Surface.DrawColoredText(Font, x, 2 + i * fontTall, 255, 255, 255, 255, $"ang: {ang.X:F2} {ang.Y:F2} {ang.Z:F2}");
				i++;
				Surface.DrawColoredText(Font, x, 2 + i * fontTall, 255, 255, 255, 255, $"vel: {vel.X:F2} {vel.Y:F2} {vel.Z:F2}");
				i++;
			}
		}

		// todo: showbattery mode
	}
	public override void OnTick() {
		bool visible = ShouldDraw();
		if (IsVisible() != visible)
			SetVisible(visible);
	}
	// todo: ScreenSizeChanged
	public virtual bool ShouldDraw() {
		if ((cl_showfps.GetInt() == 0 || (gpGlobals.AbsoluteFrameTime <= 0)) && (cl_showpos.GetInt() == 0)) {
			LastDraw = false;
			return false;
		}

		if (!LastDraw) {
			LastDraw = true;
			InitAverages();
		}

		return true;
	}
	public void GetFPSColor(int fps, out Color color) {
		color = default;
		color[0] = color[3] = 255;

		int fpsThreshold1 = 60;
		int fpsThreshold2 = 50;

		if (fps >= fpsThreshold1) {
			color[0] = 0;
			color[1] = 255;
		}
		else if (fps >= fpsThreshold2) {
			color[1] = 255;
		}
	}

	public const double cpuMonitoringWarning1 = 80;
	public const double cpuMonitoringWarning2 = 50;
	public void GetCPUColor(double cpuPercentage, out Color color) {
		color = default;
		color[0] = color[3] = 255;

		if (cpuPercentage >= cpuMonitoringWarning1) {
			color[0] = 10;
			color[1] = 200;
		}
		else if (cpuPercentage >= cpuMonitoringWarning2) {
			color[0] = 220;
			color[1] = 220;
		}
	}
	public FPSPanel(Panel? parent) : base(null, "FPSPanel") {
		SetParent(parent);
		SetVisible(false);
		SetCursor(0);
		SetFgColor(new(0, 0, 0, 255));
		SetPaintBackgroundEnabled(false);
		Font = null;
		BatteryPercent = -1;
		LastBatteryPercent = -1.0f;
		ComputeSize();

		VGui.AddTickSignal(this, 250);
		LastDraw = false;
	}
}

public interface IFPSPanel
{
	void Create(IPanel parent);
	void Destroy();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	public static IFPSPanel FPS;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}

public class FPS : IFPSPanel
{
	static FPS() {
		IFPSPanel.FPS = new FPS();
	}

	FPSPanel? fpsPanel;
	public void Create(IPanel parent) {
		fpsPanel = new FPSPanel((Panel)parent);
	}
	public void Destroy() {
		if (fpsPanel != null) {
			fpsPanel.SetParent(null);
			fpsPanel.MarkForDeletion();
			fpsPanel = null;
		}
	}
}
