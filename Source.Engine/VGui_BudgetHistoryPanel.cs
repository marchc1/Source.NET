using Source.GUI.Controls;

class BudgetHistoryPanel : Panel
{
	BaseBudgetPanel BudgetPanel;
	double Data;
	int Groups;
	int SamplesPerGroup;
	int SampleOffset;
	float RangeMin;
	float RangeMax;
	public BudgetHistoryPanel(BaseBudgetPanel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}

	public override void Paint() { }

	void DrawBudgetLine(float val) { }

	void SetData(double data, int groups, int samplesPerGroup, int sampleOffset) { }

	void SetRange(float min, float max) { }
}