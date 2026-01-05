using Source.Common.Commands;
using Source.GUI.Controls;

namespace Source.Engine;

class BudgetBarGraphPanel : Panel
{
	static ConVar budget_bargraph_background_alpha = new("budget_bargraph_background_alpha", "128", FCvar.Archive, "how translucent the budget panel is");
	static ConVar budget_peaks_window = new("budget_peaks_window", "30", FCvar.Archive, "number of frames to look at when figuring out peak frametimes");
	static ConVar budget_averages_window = new("budget_averages_window", "30", FCvar.Archive, "number of frames to look at when figuring out average frametimes");
	static ConVar budget_show_peaks = new("budget_show_peaks", "1", FCvar.Archive, "enable/disable peaks in the budget panel");
	static ConVar budget_show_averages = new("budget_show_averages", "0", FCvar.Archive, "enable/disable averages in the budget panel");

	BaseBudgetPanel BudgetPanel;
	public BudgetBarGraphPanel(BaseBudgetPanel parent, ReadOnlySpan<char> name) : base(parent, name) {
		BudgetPanel = parent;

		SetProportional(false);
		SetKeyboardInputEnabled(false);
		SetMouseInputEnabled(false);
		SetVisible(true);
		SetPaintBackgroundEnabled(true);
		SetBgColor(new(0, 0, 0, budget_bargraph_background_alpha.GetInt()));
	}

	void GetBudgetGroupTopAndBottom(int id, out int top, out int bottom) {
		BudgetPanel.GetGraphLabelScreenSpaceTopAndBottom(id, out top, out bottom);
		int tall = bottom - top;
		int x = 0;
		ScreenToLocal(ref x, ref top);
		bottom = top + tall;
	}

	void DrawBarAtIndex(int id, float percent) {
		GetSize(out int panelWidth, out _);

		GetBudgetGroupTopAndBottom(id, out int top, out int bottom);

		int left = 0;
		int right = (int)(panelWidth * percent);

		BudgetPanel.GetConfigData().BudgetGroupInfo[id].Color.GetColor(out int red, out int green, out int blue, out int alpha);

		Surface.DrawSetColor(0, 0, 0, alpha);
		Surface.DrawFilledRect(left, top, right + 2, bottom);
		Surface.DrawSetColor(255, 255, 255, alpha);
		Surface.DrawFilledRect(left, top + 1, right + 1, bottom - 1);
		Surface.DrawSetColor(red, green, blue, alpha);
		Surface.DrawFilledRect(left, top + 2, right, bottom - 2);
	}

	void DrawTickAtIndex(int id, float percent, int red, int green, int blue, int alpha) {
		if (percent > 1.0f)
			percent = 1.0f;

		GetSize(out int panelWidth, out _);

		GetBudgetGroupTopAndBottom(id, out int top, out int bottom);

		int right = (int)(panelWidth * percent + 1.0f);
		int left = right - 2;

		Surface.DrawSetColor(0, 0, 0, alpha);
		Surface.DrawFilledRect(left - 2, top, right + 2, bottom);
		Surface.DrawSetColor(255, 255, 255, alpha);
		Surface.DrawFilledRect(left - 1, top + 1, right + 1, bottom - 1);
		Surface.DrawSetColor(red, green, blue, alpha);
		Surface.DrawFilledRect(left, top + 2, right, bottom - 2);
	}

	void DrawTimeLines() {
		GetSize(out int panelWidth, out int panelHeight);
		int i;
		int left, right, top, bottom;
		top = 0;
		bottom = panelHeight;

		BudgetPanelConfigData config = BudgetPanel.GetConfigData();

		float valueInterval = config.TimeLabelInterval;
		if (config.LinesPerTimeLabel != 0.0f)
			valueInterval = config.TimeLabelInterval / config.LinesPerTimeLabel;

		int totalLines = (int)config.BarGraphRange;
		if (valueInterval != 0.0f)
			totalLines /= (int)valueInterval;
		totalLines += 2;

		for (i = 0; i < totalLines; i++) {
			int alpha;
			if (i % (config.LinesPerTimeLabel * 2) == 0)
				alpha = 150;
			else if (i % config.LinesPerTimeLabel == 0)
				alpha = 100;
			else
				alpha = 50;

			float flTemp = (config.BarGraphRange != 0.0f) ? (valueInterval / config.BarGraphRange) : valueInterval;
			left = (int)(-0.5f + panelWidth * (float)(i * flTemp));
			right = left + 1;

			Surface.DrawSetColor(0, 0, 0, alpha);
			Surface.DrawFilledRect(left - 1, top, right + 1, bottom);

			Surface.DrawSetColor(255, 255, 255, alpha);
			Surface.DrawFilledRect(left, top + 1, right, bottom - 1);
		}
	}

	void DrawInstantaneous() {
		double[]? budgetGroupTimes = BudgetPanel.GetBudgetGroupData(out int groups, out int samples, out int offset);
		if (budgetGroupTimes == null)
			return;

		for (int i = 0; i < groups; i++) {
			float percent = BudgetPanel.GetBudgetGroupPercent((float)budgetGroupTimes[samples * i + offset]);
			DrawBarAtIndex(i, percent);
		}
	}

	void DrawPeaks() {
		double[]? budgetGroupTimes = BudgetPanel.GetBudgetGroupData(out int groups, out int samples, out int sampleOffset);
		if (budgetGroupTimes == null)
			return;

		int numSamples = budget_peaks_window.GetInt();
		int i;
		for (i = 0; i < groups; i++) {
			double max = 0;
			int j;
			for (j = 0; j < numSamples; j++) {
				double tmp;
				int offset = (sampleOffset - j + BaseBudgetPanel.BUDGET_HISTORY_COUNT) % BaseBudgetPanel.BUDGET_HISTORY_COUNT;
				tmp = budgetGroupTimes[i * samples + offset];
				if (tmp > max) {
					max = tmp;
				}
			}
			float percent = BudgetPanel.GetBudgetGroupPercent((float)max);
			DrawTickAtIndex(i, percent, 255, 0, 0, 255);
		}
	}

	void DrawAverages() {
		double[]? budgetGroupTimes = BudgetPanel.GetBudgetGroupData(out int groups, out int samples, out int sampleOffset);
		if (budgetGroupTimes == null)
			return;

		int numSamples = budget_averages_window.GetInt();
		int i;
		for (i = 0; i < groups; i++) {
			BudgetPanel.GetConfigData().BudgetGroupInfo[i].Color.GetColor(out int red, out int green, out int blue, out int alpha);

			double sum = 0;
			int j;
			for (j = 0; j < numSamples; j++) {
				int offset = (sampleOffset - j + BaseBudgetPanel.BUDGET_HISTORY_COUNT) % BaseBudgetPanel.BUDGET_HISTORY_COUNT;
				sum += budgetGroupTimes[i * samples + offset];
			}
			sum *= 1.0f / numSamples;
			float percent = BudgetPanel.GetBudgetGroupPercent((float)sum);
			DrawTickAtIndex(i, percent, red, green, blue, alpha);
		}
	}

	public override void Paint() {
		if (!BudgetPanel.IsDedicated())
			SetBgColor(new(255, 0, 0, budget_bargraph_background_alpha.GetInt()));

		DrawTimeLines();
		DrawInstantaneous();
		if (budget_show_peaks.GetBool()) DrawPeaks();
		if (budget_show_averages.GetBool()) DrawAverages();
	}
}