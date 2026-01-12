using Source.Common.Commands;
using Source.Common.Mathematics;
using Source.GUI.Controls;

namespace Source.Engine;

class BudgetPanelEngine : BudgetPanelShared
{
	static BudgetPanelEngine Instance;
	bool ShowBudgetPanelHeld;

	public BudgetPanelEngine(Panel? parent, ReadOnlySpan<char> name) : base(parent, name, (int)(BudgetFlags.Client | BudgetFlags.Other | BudgetFlags.Hidden)) {
		Instance = this;
		ShowBudgetPanelHeld = false;
	}

	static BudgetPanelEngine GetBudgetPanel() => Instance;

	public override void PostChildPaint() {
		byte r = 255;
		byte g = 0;
		int dxDupportLevel = HardwareConfig.GetDXSupportLevel();

		if ((FrameRate >= 60) || (dxDupportLevel <= 80 && FrameRate >= 30) || (dxDupportLevel <= 70 && FrameRate >= 20)) {
			r = 0;
			g = 255;
		}

		int yPos = 20;
		Span<char> txt = stackalloc char[64];
		sprintf(txt, "%3i fps (showbudget 3D driver time included)").I(MathLib.RoundFloatToInt((int)FrameRate));
		Surface.DrawColoredText(Font, 600, yPos, r, g, 0, 255, txt);
	}

	void UserCmd_ShowBudgetPanel() {
		cbuf.AddText("vprof_on\n");
		ShowBudgetPanelHeld = true;
		SetVisible(true);
	}

	void UserCmd_HideBudgetPanel() {
		cbuf.AddText("vprof_off\n");
		ShowBudgetPanelHeld = false;
		SetVisible(false);
	}

	public override void OnTick() {
		// if (ShowBudgetPanelHeld && !CanCheat()) // todo
		// 	UserCmd_HideBudgetPanel();

		base.OnTick();
		SetVisible(ShowBudgetPanelHeld);
	}

	public override void SetTimeLabelText() {
		Span<char> buf = stackalloc char[32];
		for (int i = 0; i < TimeLabels.Count; i++) {
			sprintf(buf, "%dms").D((int)(i * GetConfigData().TimeLabelInterval));
			TimeLabels[i].SetText(buf);
		}
	}

	public override void SetHistoryLabelText() {
		Assert(HistoryLabels.Count == 3);
		HistoryLabels[0].SetText("20 fps (50 ms)");
		HistoryLabels[1].SetText("30 fps (33 1/3 ms)");
		HistoryLabels[2].SetText("60 fps (16 2/3 ms)");
	}

	bool IsBudgetPanelShown() => ShowBudgetPanelHeld;

	[ConCommand("vprof_adddebuggroup1", "add a new budget group dynamically for debugging")]
	static void VProfAddDebugGroup1(in TokenizedCommand args) {

	}

	[ConCommand("+showbudget")]
	static void IN_BudgetDown(in TokenizedCommand args) => GetBudgetPanel().UserCmd_ShowBudgetPanel();
	[ConCommand("-showbudget")]
	static void IN_BudgetUp(in TokenizedCommand args) => GetBudgetPanel().UserCmd_HideBudgetPanel();
}