using Source.Common.Commands;
using Source.Common.GUI;
using Source.GUI.Controls;

[Flags]
public enum BudgetFlags : ushort
{
	Client = 1 << 0,    // Shows up in the client panel
	Server = 1 << 1,    // Shows up in the server panel
	Other = 1 << 2,    // Unclassified
	Hidden = 1 << 15,
	All = 0xFFFF
}

class BudgetPanelShared : BaseBudgetPanel
{

	static ConVar budget_history_range_ms = new("budget_history_range_ms", "66.666666667", FCvar.Archive, "budget history range in milliseconds", callback: PanelGeometryChangedCallBack);
	static ConVar budget_panel_bottoof_history_fraction = new("budget_panel_bottoof_history_fraction", ".25", FCvar.Archive, "number between 0 and 1", callback: PanelGeometryChangedCallBack);
	static ConVar budget_bargraph_range_ms = new("budget_bargraph_range_ms", "16.6666666667", FCvar.Archive, "budget bargraph range in milliseconds", callback: PanelGeometryChangedCallBack);
	static ConVar budget_background_alpha = new("budget_background_alpha", "128", FCvar.Archive, "how translucent the budget panel is");

	static ConVar budget_panel_x = new("budget_panel_x", "0", FCvar.Archive, "number of pixels from the left side of the game screen to draw the budget panel", callback: PanelGeometryChangedCallBack);
	static ConVar budget_panel_y = new("budget_panel_y", "50", FCvar.Archive, "number of pixels from the top side of the game screen to draw the budget panel", callback: PanelGeometryChangedCallBack);
	static ConVar budget_panel_width = new("budget_panel_width", "512", FCvar.Archive, "width in pixels of the budget panel", callback: PanelGeometryChangedCallBack);
	static ConVar budget_panel_height = new("budget_panel_height", "384", FCvar.Archive, "height in pixels of the budget panel", callback: PanelGeometryChangedCallBack);
	static void PanelGeometryChangedCallBack(IConVar var, in ConVarChangeContext ctx) => g_BudgetPanelShared?.SendConfigDataToBase();

	static BudgetPanelShared? g_BudgetPanelShared;
	static double FrameTimeLessBudget;
	static double FrameRate;
	public BudgetPanelShared(Panel? parent, ReadOnlySpan<char> name, int budgetFlagsFilter) : base(parent, name) {
		Assert(g_BudgetPanelShared == null);
		g_BudgetPanelShared = this;

		// if (g_VProfExport)..

		SendConfigDataToBase();
		SetZPos(1001);
		SetVisible(false);
		VGui.AddTickSignal(this);
		SetPostChildPaintEnabled(false);
	}

	// FIXME #37
	public override void Dispose() {
		base.Dispose();
		Assert(g_BudgetPanelShared == this);
		g_BudgetPanelShared = null;
	}

	void OnNumBudgetGroupsChanged() => SendConfigDataToBase();

	private void SetupCustomConfigData(BudgetPanelConfigData data) {
		data.XCoord = budget_panel_x.GetInt();
		data.YCoord = budget_panel_y.GetInt();
		data.Width = budget_panel_width.GetInt();
		data.Height = budget_panel_height.GetInt();
	}

	void SendConfigDataToBase() {
		BudgetPanelConfigData data = new();

		int groups = 0;
		// if (g_pVProfExport != null) {
		// }

		for (int i = 0; i < groups; i++) {
			data.BudgetGroupInfo.Add(
				new() {
					// Name = g_TempBudgetGroupSpace[i].pName,
					// Color = g_TempBudgetGroupSpace[i].Color
				}
			);
		}

		data.HistoryLabelValues.Add(1000.0f / 20);
		data.HistoryLabelValues.Add(1000.0f / 30);
		data.HistoryLabelValues.Add(1000.0f / 60);

		data.HistoryRange = budget_history_range_ms.GetFloat();
		data.BottomOfHistoryFraction = budget_panel_bottoof_history_fraction.GetFloat();

		data.BarGraphRange = budget_bargraph_range_ms.GetFloat();
		data.TimeLabelInterval = 5;
		data.LinesPerTimeLabel = 5;

		data.BackgroundAlpha = budget_background_alpha.GetFloat();

		SetupCustomConfigData(data);
		// OnConfigDataChanged(data);
	}

	void DrawColoredText(IFont font, int x, int y, int r, int g, int b, int a, ReadOnlySpan<char> text) { }

	public override void PaintBackground() {
		// if (g_VProfExport != null)
		// 	g_VProfExport.PauseProfile();
		base.PaintBackground();
	}

	static bool TimerInitialized = false;
	public override void Paint() {
		if (BudgetGroupTimes.Count == 0)
			return;

		if (!TimerInitialized) {
			// g_TimerLessBudget.Start();
			TimerInitialized = true;
		}

		// g_TimerLessBudget.End();

		base.Paint();

		// FrameTimeLessBudget = g_TimerLessBudget.GetDuration().GetSeconds();
		FrameRate = 1.0 / FrameTimeLessBudget;
	}

	public override void PostChildPaint() {
		// g_TimerLessBudget.Start();

		// if (g_VProfExport != null)
		// g_VProfExport->ResumeProfile();
	}

	void SnapshotVProfHistory(float filteredtime) { }

	void SetTimeLabelText() {
		Span<char> text = stackalloc char[512];
		for (int i = 0; i < TimeLabels.Count; i++) {
			text.Clear();
			sprintf(text, "%dms").D((int)(i * GetConfigData().TimeLabelInterval));
			TimeLabels[i].SetText(text);
		}
	}

	void SetHistoryLabelText() {
		Assert(HistoryLabels.Count == 3);
		HistoryLabels[0].SetText("20 fps (50 ms)");
		HistoryLabels[1].SetText("30 fps (33 1/3 ms)");
		HistoryLabels[2].SetText("60 fps (16 2/3 ms)");
	}

	private BudgetPanelConfigData GetConfigData() => ConfigData;
}