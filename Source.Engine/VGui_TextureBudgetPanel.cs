using Source.Common.Commands;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Source.Engine;

enum CounterGroup
{
	Default = 0,
	NoReset,
	TextureGlobal,
	TexturePerFrame,
	Telemetry
}

class TextureBudgetPanel : BaseBudgetPanel
{
	static TextureBudgetPanel? g_TextureBudgetPanel;

	public static TextureBudgetPanel? GetTextureBudgetPanel() => g_TextureBudgetPanel;
	private static void TextureCVarChangedCallBack(IConVar conVar, in ConVarChangeContext ctx) => GetTextureBudgetPanel()?.OnCVarStateChanged();
	static ConVar texture_budget_panel_global = new("texture_budget_panel_global", "0", FCvar.None, "Show global times in the texture budget panel.");
	static ConVar showbudget_texture = new("showbudget_texture", "0", FCvar.Cheat, "Enable the texture budget panel.");
	static ConVar showbudget_texture_global_sum = new("showbudget_texture_global_sum", 0.0f);
	static ConVar texture_budget_panel_x = new("texture_budget_panel_x", "0", FCvar.Archive, "number of pixels from the left side of the game screen to draw the budget panel", callback: TextureCVarChangedCallBack);
	static ConVar texture_budget_panel_y = new("texture_budget_panel_y", "450", FCvar.Archive, "number of pixels from the top side of the game screen to draw the budget panel", callback: TextureCVarChangedCallBack);
	static ConVar texture_budget_panel_width = new("texture_budget_panel_width", "512", FCvar.Archive, "width in pixels of the budget panel", callback: TextureCVarChangedCallBack);
	static ConVar texture_budget_panel_height = new("texture_budget_panel_height", "284", FCvar.Archive, "height in pixels of the budget panel", callback: TextureCVarChangedCallBack);
	static ConVar texture_budget_panel_bottoof_history_fraction = new("texture_budget_panel_bottoof_history_fraction", ".25", FCvar.Archive, "number between 0 and 1", callback: TextureCVarChangedCallBack);
	static ConVar texture_budget_background_alpha = new("texture_budget_background_alpha", "128", FCvar.Archive, "how translucent the budget panel is");

	Label ModeLabel;
	int LastCounterGroup;
	int MaxValue;
	int SumOfValues;

	public TextureBudgetPanel(Panel parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {
		LastCounterGroup = -1;
		g_TextureBudgetPanel = this;
		MaxValue = 1000;
		SumOfValues = 0;

		ModeLabel = new(this, "mode label", "");
		ModeLabel.SetParent(this);
		SetVisible(false);
		VGui.AddTickSignal(this, 0);
	}

	public override void OnTick() {
		base.OnTick();
		if (showbudget_texture.GetBool()) {
			ModeLabel.SetVisible(true);
			SetVisible(true);
		}
		else {
			ModeLabel.SetVisible(false);
			SetVisible(false);
		}
	}

	public override void Paint() {
		SnapshotTextureHistory();
		// g_VProfCurrentProfile.ResetCounters(CounterGroup.TexturePerFrame);
		base.Paint();
	}

	void SendConfigDataToBase() {
		BudgetPanelConfigData data = new();

		// for (int i = 0; i < g_VProfCurrentProfile.GetNumCounters(); i++) {

		// }


		data.BottomOfHistoryFraction = texture_budget_panel_bottoof_history_fraction.GetFloat();

		data.BarGraphRange = MaxValue * 4 / 3;
		data.TimeLabelInterval = data.BarGraphRange / 4;
		data.LinesPerTimeLabel = 4;

		data.HistoryRange = SumOfValues * 4 / 3;

		data.HistoryLabelValues.SetSize(3);
		for (int i = 0; i < data.HistoryLabelValues.Count; i++)
			data.HistoryLabelValues[i] = (i + 1) * data.HistoryRange / 4;

		data.BackgroundAlpha = texture_budget_background_alpha.GetFloat();

		data.XCoord = texture_budget_panel_x.GetInt();
		data.YCoord = texture_budget_panel_y.GetInt();
		data.Width = texture_budget_panel_width.GetInt();
		data.Height = texture_budget_panel_height.GetInt();

		if (data.XCoord + data.Width > ((VideoMode_Common)videoMode).GetModeStereoWidth())
			data.XCoord = ((VideoMode_Common)videoMode).GetModeStereoWidth() - data.Width;

		if (data.YCoord + data.Height > ((VideoMode_Common)videoMode).GetModeStereoHeight())
			data.YCoord = ((VideoMode_Common)videoMode).GetModeStereoHeight() - data.Height;

		OnConfigDataChanged(data);
	}

	public override void PerformLayout() {
		base.PerformLayout();

		ReadOnlySpan<char> str = "Per-frame texture stats";
		if (texture_budget_panel_global.GetBool())
			str = "Global texture stats";

		ModeLabel.SetText(str);
		int width = ((IMatSystemSurface)Surface).DrawTextLen(ModeLabel.GetFont()!, str);
		ModeLabel.SetSize(width + 10, ModeLabel.GetTall());

		GetPos(out int x, out int y);
		ModeLabel.SetPos(x, y - ModeLabel.GetTall());
		ModeLabel.SetFgColor(new(255, 255, 255, 255));
		ModeLabel.SetBgColor(new(0, 0, 0, texture_budget_background_alpha.GetInt()));
	}

	void OnCVarStateChanged() => SendConfigDataToBase();

	CounterGroup GetCurrentCounterGroup() => texture_budget_panel_global.GetBool() ? CounterGroup.TextureGlobal : CounterGroup.TexturePerFrame;

	void SnapshotTextureHistory() {
		// todo
	}

	public override void SetTimeLabelText() {
		Span<char> buf = stackalloc char[32];
		for (int i = 0; i < TimeLabels.Count; i++) {
			sprintf(buf, "%.1fM").D((int)(i * GetConfigData().TimeLabelInterval) / 1024);
			TimeLabels[i].SetText(buf);
		}
	}

	public override void SetHistoryLabelText() {
		Span<char> buf = stackalloc char[32];
		for (int i = 0; i < HistoryLabels.Count; i++) {
			sprintf(buf, "%.1fM").D((int)(i * GetConfigData().HistoryLabelValues[i] / 1024));
			HistoryLabels[i].SetText(buf);
		}
	}

	public override void ResetAll() {
		base.ResetAll();
		MaxValue = 0;
		SumOfValues = 0;
	}

	[ConCommand("showbudget_texture_global_dumpstats", "Dump all items in +showbudget_texture_global in a text form")]
	static void DumpGlobalTextureStats(in TokenizedCommand args) {
		// todo
	}

	[ConCommand("+showbudget_texture", "", FCvar.Cheat)]
	static void ShowBudget_Texture_on() {
		texture_budget_panel_global.SetValue(0);
		showbudget_texture.SetValue(1);
	}

	[ConCommand("-showbudget_texture", "", FCvar.Cheat)]
	static void ShowBudget_Texture_off() => showbudget_texture.SetValue(0);

	[ConCommand("+showbudget_texture_global", "", FCvar.Cheat)]
	static void ShowBudget_Texture_Global_on() {
		texture_budget_panel_global.SetValue(1);
		showbudget_texture.SetValue(1);
	}

	[ConCommand("-showbudget_texture_global", "", FCvar.Cheat)]
	static void ShowBudget_Texture_Global_off() => showbudget_texture.SetValue(0);
}