using Source;
using Source.Common.GUI;
using Source.Common.Utilities;
using Source.GUI.Controls;

class BudgetGroupInfo
{
	public UtlSymbol Name;
	public Color Color;
}

class BudgetPanelConfigData
{
	public List<BudgetGroupInfo> BudgetGroupInfo;
	public float HistoryRange;
	public float BottomOfHistoryFraction;
	public List<float> HistoryLabelValues;
	public float BarGraphRange;
	public float TimeLabelInterval;
	public int LinesPerTimeLabel;
	public float BackgroundAlpha;
	public int XCoord;
	public int YCoord;
	public int Width;
	public int Height;
}

class BaseBudgetPanel : Panel
{
	const int BUDGET_HISTORY_COUNT = 1024;

	int BudgetHistoryOffset;
	public BudgetPanelConfigData ConfigData;
	List<Label> GraphLabels;
	public List<Label> TimeLabels;
	public List<Label> HistoryLabels;
	BudgetHistoryPanel BudgetHistoryPanel;
	BudgetBarGraphPanel BudgetBarGraphPanel;
	public struct BudgetGroupTimeData()
	{
		public double[] Time = new double[BUDGET_HISTORY_COUNT];
	}
	public List<BudgetGroupTimeData> BudgetGroupTimes = [];
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