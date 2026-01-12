using Source.Common.GUI;
using Source.Common.Utilities;
using Source.GUI.Controls;

namespace Source.Engine;

class BudgetGroupInfo
{
	public UtlSymbol Name;
	public Color Color;
}

class BudgetPanelConfigData
{
	public List<BudgetGroupInfo> BudgetGroupInfo = [];
	public float HistoryRange;
	public float BottomOfHistoryFraction;
	public List<float> HistoryLabelValues = [];
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
	public const int BUDGET_HISTORY_COUNT = 1024;

	int BudgetHistoryOffset;
	public BudgetPanelConfigData ConfigData;
	List<Label> GraphLabels = [];
	public List<Label> TimeLabels = [];
	public List<Label> HistoryLabels = [];
	BudgetHistoryPanel? BudgetHistoryPanel;
	BudgetBarGraphPanel? BudgetBarGraphPanel;
	public struct BudgetGroupTimeData()
	{
		public double[] Time = new double[BUDGET_HISTORY_COUNT];
	}
	public List<BudgetGroupTimeData> BudgetGroupTimes = [];
	int CachedNumTimeLabels;
	public IFont Font;
	bool Dedicated;
	public BaseBudgetPanel(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {
		BudgetHistoryOffset = 0;
		SetProportional(false);
		SetKeyboardInputEnabled(false);
		SetMouseInputEnabled(false);
		SetVisible(true);

		IScheme scheme = SchemeManager.LoadSchemeFromFile("resource/SourceScheme.res", "Client")!;
		SetScheme(scheme);

		BudgetHistoryPanel = null;
		BudgetBarGraphPanel = null;
		SetZPos(1001);

		Dedicated = false;
	}

	public float GetBudgetGroupPercent(float value) {
		if (ConfigData.BarGraphRange == 0.0f)
			return 1.0f;
		return value / ConfigData.BarGraphRange;
	}

	public double[]? GetBudgetGroupData(out int groups, out int samplesPerGroup, out int sampleOffset) {
		groups = ConfigData.BudgetGroupInfo.Count;
		samplesPerGroup = BUDGET_HISTORY_COUNT;
		sampleOffset = BudgetHistoryOffset;

		if (BudgetGroupTimes.Count == 0)
			return null;

		return BudgetGroupTimes[0].Time;
	}

	void ClearTimesForAllGroupsForThisFrame() {
		for (int i = 0; i < ConfigData.BudgetGroupInfo.Count; i++)
			BudgetGroupTimes[i].Time[BudgetHistoryOffset] = 0.0;
	}

	void ClearAllTimesForGroup(int groupID) {
		for (int i = 0; i < BUDGET_HISTORY_COUNT; i++)
			BudgetGroupTimes[groupID].Time[i] = 0.0;
	}

	public void OnConfigDataChanged(BudgetPanelConfigData data) {
		Rebuild(data);

		if (ConfigData.BudgetGroupInfo.Count > BudgetGroupTimes.Count) {
			for (int i = BudgetGroupTimes.Count; i < ConfigData.BudgetGroupInfo.Count; i++) {
				BudgetGroupTimes.Add(new());
				ClearAllTimesForGroup(i);
			}
		}
		else {
			while (BudgetGroupTimes.Count > ConfigData.BudgetGroupInfo.Count)
				BudgetGroupTimes.RemoveAt(BudgetGroupTimes.Count - 1);

			for (int i = 0; i < BudgetGroupTimes.Count; i++)
				ClearAllTimesForGroup(i);
		}

		InvalidateLayout(false, true);
	}

	public virtual void ResetAll() {
		ConfigData.BudgetGroupInfo.Clear();

		foreach (var label in GraphLabels) label.MarkForDeletion();
		GraphLabels.Clear();

		foreach (var label in TimeLabels) label.MarkForDeletion();
		TimeLabels.Clear();
	}

	void Rebuild(BudgetPanelConfigData data) {
		int oldNumBudgetGroups = ConfigData?.BudgetGroupInfo.Count ?? 0;
		int oldNumHistoryLabels = ConfigData?.HistoryLabelValues.Count ?? 0;
		int oldNumTimeLabels = TimeLabels.Count;

		ConfigData = data;

		GetParent()!.GetSize(out int parentWidth, out int parentHeight);
		if (ConfigData.Width > parentWidth)
			ConfigData.Width = parentWidth;
		if (ConfigData.Height > parentHeight)
			ConfigData.Height = parentHeight;
		if (ConfigData.XCoord + ConfigData.Width > parentWidth)
			ConfigData.XCoord = parentWidth - ConfigData.Width;
		if (ConfigData.YCoord + ConfigData.Height > parentHeight)
			ConfigData.YCoord = parentHeight - ConfigData.Height;

		BudgetHistoryPanel?.MarkForDeletion();
		BudgetHistoryPanel = new(this, "FrametimeHistory");

		BudgetBarGraphPanel?.MarkForDeletion();
		BudgetBarGraphPanel = new(this, "BudgetBarGraph");

		int i;
		if (ConfigData.BudgetGroupInfo.Count > GraphLabels.Count) {
			while (GraphLabels.Count < ConfigData.BudgetGroupInfo.Count) GraphLabels.Add(null!);
			for (i = oldNumBudgetGroups; i < ConfigData.BudgetGroupInfo.Count; i++) {
				ReadOnlySpan<char> BudgetGroupName = ConfigData.BudgetGroupInfo[i].Name.String();
				GraphLabels[i] = new(this, BudgetGroupName, BudgetGroupName);
			}
		}
		else {
			while (GraphLabels.Count > ConfigData.BudgetGroupInfo.Count) {
				GraphLabels[^1].MarkForDeletion();
				GraphLabels.RemoveAt(GraphLabels.Count - 1);
			}
		}
		Assert(GraphLabels.Count == ConfigData.BudgetGroupInfo.Count);


		if (ConfigData.HistoryLabelValues.Count > HistoryLabels.Count) {
			while (HistoryLabels.Count < ConfigData.HistoryLabelValues.Count) HistoryLabels.Add(null!);
			for (i = oldNumHistoryLabels; i < HistoryLabels.Count; i++)
				HistoryLabels[i] = new(this, "history label", "history label");
		}
		else {
			while (HistoryLabels.Count > ConfigData.HistoryLabelValues.Count) {
				HistoryLabels[^1].MarkForDeletion();
				HistoryLabels.RemoveAt(HistoryLabels.Count - 1);
			}
		}

		SetHistoryLabelText();


		int nTimeLabels = (int)(ConfigData.BarGraphRange + data.TimeLabelInterval);
		if (data.TimeLabelInterval != 0.0f) {
			nTimeLabels /= (int)data.TimeLabelInterval;
		}

		if (nTimeLabels > TimeLabels.Count) {
			Span<char> name = stackalloc char[1024];
			while (TimeLabels.Count < nTimeLabels) TimeLabels.Add(null!);
			for (i = oldNumTimeLabels; i < TimeLabels.Count; i++) {
				sprintf(name, "time_label_%d").D(i);
				TimeLabels[i] = new(this, name, "TEXT NOT SET YET");
			}
		}
		else {
			while (TimeLabels.Count > nTimeLabels) {
				TimeLabels[^1].MarkForDeletion();
				TimeLabels.RemoveAt(TimeLabels.Count - 1);
			}
		}

		SetTimeLabelText();
	}

	void UpdateWindowGeometry() {
		if (ConfigData.Width > BUDGET_HISTORY_COUNT)
			ConfigData.Width = BUDGET_HISTORY_COUNT;

		SetPos(ConfigData.XCoord, ConfigData.YCoord);
		SetSize(ConfigData.Width, ConfigData.Height);
	}

	public override void PerformLayout() {
		if (BudgetHistoryPanel == null || BudgetBarGraphPanel == null)
			return;


		int maxFPSLabelWidth = 0;
		int i;
		for (i = 0; i < HistoryLabels.Count; i++) {
			HistoryLabels[i].GetContentSize(out int labelWidth, out int labelHeight);
			if (labelWidth > maxFPSLabelWidth)
				maxFPSLabelWidth = labelWidth;
		}

		BudgetHistoryPanel.SetRange(0, ConfigData.HistoryRange);


		float bottomOfHistoryPercentage = ConfigData.BottomOfHistoryFraction;
		UpdateWindowGeometry();
		int totalHeightMinusTimeLabels;
		GetSize(out int totalWidth, out int totalHeight);

		int maxTimeLabelHeight = 0;
		for (i = 0; i < TimeLabels.Count; i++) {
			TimeLabels[i].GetContentSize(out _, out int labelHeight);
			maxTimeLabelHeight = Math.Max(maxTimeLabelHeight, labelHeight);
		}

		totalHeightMinusTimeLabels = totalHeight - maxTimeLabelHeight;

		BudgetHistoryPanel.SetPos(0, 0);
		int budgetHistoryHeight = (int)(totalHeightMinusTimeLabels * bottomOfHistoryPercentage);
		BudgetHistoryPanel.SetSize(totalWidth - maxFPSLabelWidth, budgetHistoryHeight);

		int maxLabelWidth = 0;
		for (i = 0; i < GraphLabels.Count; i++) {
			GraphLabels[i].GetContentSize(out int width, out int height);
			if (maxLabelWidth < width)
				maxLabelWidth = width;
		}

		BudgetBarGraphPanel.SetPos(maxLabelWidth, (int)(totalHeightMinusTimeLabels * bottomOfHistoryPercentage));
		BudgetBarGraphPanel.SetSize(totalWidth - maxLabelWidth, (int)(totalHeightMinusTimeLabels * (1 - bottomOfHistoryPercentage)));

		for (i = 0; i < GraphLabels.Count; i++) {
			GraphLabels[i].SetPos(0, (int)((bottomOfHistoryPercentage * totalHeightMinusTimeLabels) + i * totalHeightMinusTimeLabels * (1 - bottomOfHistoryPercentage) / ConfigData.BudgetGroupInfo.Count));
			GraphLabels[i].SetSize(maxLabelWidth, (int)(1 + totalHeightMinusTimeLabels * (1 - bottomOfHistoryPercentage) / ConfigData.BudgetGroupInfo.Count));
			GraphLabels[i].SetContentAlignment(Alignment.East);
		}

		float range = ConfigData.BarGraphRange;
		for (i = 0; i < TimeLabels.Count; i++) {
			TimeLabels[i].GetContentSize(out int labelWidth, out int labelHeight);
			int x = maxLabelWidth + (int)(i * ConfigData.TimeLabelInterval / range * (totalWidth - maxLabelWidth));
			TimeLabels[i].SetPos(x - (int)(labelWidth * 0.5), totalHeight - labelHeight);
			TimeLabels[i].SetSize(labelWidth, labelHeight);
			TimeLabels[i].SetContentAlignment(Alignment.East);
		}

		range = ConfigData.HistoryRange;
		for (i = 0; i < HistoryLabels.Count; i++) {
			HistoryLabels[i].GetContentSize(out int labelWidth, out int labelHeight);
			int y = (int)((range != 0) ? budgetHistoryHeight * ConfigData.HistoryLabelValues[i] / (float)range : 0.0f);
			int top = (int)(budgetHistoryHeight - y - 1 - labelHeight * 0.5f);
			HistoryLabels[i].SetPos(totalWidth - maxFPSLabelWidth, top);
			HistoryLabels[i].SetSize(labelWidth, labelHeight);
			HistoryLabels[i].SetContentAlignment(Alignment.East);
		}
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		int i;
		for (i = 0; i < ConfigData.BudgetGroupInfo.Count; i++) {
			GraphLabels[i].SetFgColor(ConfigData.BudgetGroupInfo[i].Color);
			GraphLabels[i].SetBgColor(new(0, 0, 0, 255));
			GraphLabels[i].SetPaintBackgroundEnabled(false);
			GraphLabels[i].SetFont(scheme.GetFont("BudgetLabel", IsProportional()));
			if (Dedicated)
				GraphLabels[i].SetBgColor(scheme.GetColor("ControlBG", new(0, 0, 0, 255)));
		}

		for (i = 0; i < TimeLabels.Count; i++) {
			int red, green, blue, alpha;
			red = green = blue = alpha = 255;
			TimeLabels[i].SetFgColor(new(red, green, blue, alpha));
			TimeLabels[i].SetBgColor(new(0, 0, 0, 255));
			TimeLabels[i].SetPaintBackgroundEnabled(false);
			TimeLabels[i].SetFont(scheme.GetFont("BudgetLabel", IsProportional()));
			if (Dedicated)
				TimeLabels[i].SetBgColor(scheme.GetColor("ControlBG", new(0, 0, 0, 255)));
		}

		for (i = 0; i < HistoryLabels.Count; i++) {
			int red, green, blue, alpha;
			red = green = blue = alpha = 255;
			HistoryLabels[i].SetFgColor(new(red, green, blue, alpha));
			HistoryLabels[i].SetBgColor(new(0, 0, 0, 255));
			HistoryLabels[i].SetPaintBackgroundEnabled(false);
			HistoryLabels[i].SetFont(scheme.GetFont("BudgetLabel", IsProportional()));
			if (Dedicated)
				HistoryLabels[i].SetBgColor(scheme.GetColor("ControlBG", new(0, 0, 0, 255)));
		}

		Font = scheme.GetFont("DefaultFixed")!;

		if (Dedicated)
			SetBgColor(scheme.GetColor("ControlBG", new(0, 0, 0, 255)));

		SetPaintBackgroundEnabled(true);
	}

	public override void PaintBackground() {
		if (Dedicated)
			BudgetBarGraphPanel!.SetBgColor(GetBgColor());
		else
			SetBgColor(new(0, 0, 0, (int)ConfigData.BackgroundAlpha));

		base.PaintBackground();
	}

	public override void Paint() {
		BudgetHistoryPanel!.SetData(BudgetGroupTimes[0].Time, GetNumCachedBudgetGroups(), BUDGET_HISTORY_COUNT, BudgetHistoryOffset);
		base.Paint();
	}

	void MarkForFullRepaint() {
		Repaint();
		BudgetHistoryPanel!.Repaint();
		BudgetBarGraphPanel!.Repaint();
	}

	public void GetGraphLabelScreenSpaceTopAndBottom(int id, out int top, out int bottom) {
		int x = 0;
		int y = 0;
		GraphLabels[id].LocalToScreen(ref x, ref y);
		top = y;
		bottom = y + GraphLabels[id].GetTall();
	}

	public BudgetPanelConfigData GetConfigData() => ConfigData;
	public int GetNumCachedBudgetGroups() => ConfigData.BudgetGroupInfo.Count;
	public void MarkAsDedicatedServer() => Dedicated = true;
	public bool IsDedicated() => Dedicated;
	public virtual void SetTimeLabelText() { }
	public virtual void SetHistoryLabelText() { }
}