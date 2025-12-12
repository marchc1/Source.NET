using Source.Common.GUI;
using Source.GUI.Controls;

class BudgetPanelShared : BaseBudgetPanel
{
	static double FrameTimeLessBudget;
	static double FrameRate;
	public BudgetPanelShared(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}

	void OnNumBudgetGroupsChanged() { }

	void SetupCustomConfigData(BudgetPanelConfigData data) { }

	void SendConfigDataToBase() { }

	void DrawColoredText(IFont font, int x, int y, int r, int g, int b, int a, ReadOnlySpan<char> text) { }

	public override void PaintBackground() { }

	public override void Paint() { }

	public override void PostChildPaint() { }

	void SnapshotVProfHistory(float filteredtime) { }

	void SetTimeLabelText() { }

	void SetHistoryLabelText() { }
}