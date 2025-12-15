using Source.Common.Formats.Keyvalues;
using Source.GUI.Controls;

class ScrollableEditablePanel : EditablePanel
{
	ScrollBar ScrollBar;
	EditablePanel? Child;

	public ScrollableEditablePanel(Panel? parent, EditablePanel? child, ReadOnlySpan<char> name) : base(parent, name) {

	}

	public override void ApplySettings(KeyValues resourceData) { }

	public override void PerformLayout() { }

	void OnScrollBarSliderMoved() { }

	public override void OnMouseWheeled(int delta) { }
}