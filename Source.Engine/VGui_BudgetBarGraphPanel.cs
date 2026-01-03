using Source.GUI.Controls;

namespace Source.Engine;

class BudgetBarGraphPanel : Panel
{
	BaseBudgetPanel BudgetPanel;
	public BudgetBarGraphPanel(BaseBudgetPanel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}

	void GetBudgetGroupTopAndBottom(int id, int top, int bottom) { }

	void DrawBarAtIndex(int id, float percent) { }

	void DrawTickAtIndex(int id, float percent, int red, int green, int blue, int alpha) { }

	void DrawTimeLines() { }

	void DrawInstantaneous() { }

	void DrawPeaks() { }

	void DrawAverages() { }

	public override void Paint() { }
}