using Source.Common.Input;

namespace Source.GUI.Controls;

class ToolWindow : Frame
{
	PropertySheet PropertySheet;
	// IToolWwinowFactory Factory;

	public ToolWindow(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {

	}

	// int GetToolWindowCount() { }

	// ToolWindow GetToolWindow(int index) { }

	// bool IsDraggableTabContainer() { }

	// PropertySheet GetPropertySheet() { }

	// Panel GetActivePage() { }

	void SetActivePage(Panel page) { }

	void AddPage(Panel page, ReadOnlySpan<char> title, bool contextMenu) { }

	void RemovePage(Panel page) { }

	public override void PerformLayout() { }

	public override void ActivateBuildMode() { }

	public override void RequestFocus(int direction) { }

	// void SetToolWindowFactory(IToolWindowFactory factory) { }

	// IToolWindowFactory GetToolWindowFactory() { }

	void Grow(int edge, int from_x, int from_y) { }

	void GrowFromClick() { }

	public override void OnMouseDoublePressed(ButtonCode code) { }

	public override void OnMousePressed(ButtonCode code) { }

}