using Source.GUI.Controls;

class BudgetPanelEngine : BudgetPanelShared
{
	public BudgetPanelEngine(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}

	public override void PostChildPaint() { }

	void UserCmd_ShowBudgetPanel() { }

	void UserCmd_HideBudgetPanel() { }

	public override void OnTick() { }

	void SetTimeLabelText() { }

	void SetHistoryLabelText() { }

	bool IsBudgetPanelShown() { return false; }
}