using Source;
using Source.Common.GUI;
using Source.Common.Utilities;
using Source.GUI.Controls;

class BudgetGroupInfo
{
	UtlSymbol Name;
	Color Color;
}

class BudgetPanelConfigData
{
	List<BudgetGroupInfo> BudgetGroupInfo;
	float HistoryRange;
	float BottomOfHistoryFraction;
	List<float> HistoryLabelValues;
	float BarGraphRange;
	float TimeLabelInterval;
	int LinesPerTimeLabel;
	float BackgroundAlpha;
	int XCoord;
	int YCoord;
	int Width;
	int Height;
}

class BaseBudgetPanel : Panel
{
	const int BUDGET_HISTORY_COUNT = 1024;

	int BudgetHistoryOffset;
	BudgetPanelConfigData ConfigData;
	List<Label> GraphLabels;
	List<Label> TimeLabels;
	List<Label> HistoryLabels;
	BudgetHistoryPanel BudgetHistoryPanel;
	BudgetBarGraphPanel BudgetBarGraphPanel;
	struct BudgetGroupTimeData()
	{
		public double[] Time = new double[BUDGET_HISTORY_COUNT];
	}
	List<BudgetGroupTimeData> BudgetGroupTimes;
	int CachedNumTimeLabels;
	IFont Font;
	bool Dedicated;
	public BaseBudgetPanel(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}

	float GetBudgetGroupPercent(float value) { return 0.0f; }

	double GetBudgetGroupData(int groups, int samplesPerGroup, int sampleOffset) { return 0.0; }

	void ClearTimesForAllGroupsForThisFrame() { }

	void ClearAllTimesForGroup(int groupID) { }

	void OnConfigDataChanged(BudgetPanelConfigData data) { }

	void ResetAll() { }

	void Rebuild(BudgetPanelConfigData data) { }

	void UpdateWindowGeometry() { }

	public override void PerformLayout() { }

	public override void ApplySchemeSettings(IScheme scheme) { }

	public override void PaintBackground() { }

	public override void Paint() { }

	void MarkForFullRepaint() { }

	void GetGraphLabelScreenSpaceTopAndBottom(int id, int top, int bottom) { }
}