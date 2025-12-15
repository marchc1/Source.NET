using Source;
using Source.Common.GUI;
using Source.GUI.Controls;

class ExpandButton : ToggleButton
{
	bool Expandable;
	IFont Font;
	Color Color;

	public ExpandButton(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName, "") {

	}

	public override void ApplySchemeSettings(IScheme scheme) { }

	// public override IBorder GetBorder(bool depressed, bool armed, bool selected, bool keyfocus) { }

	public override void SetSelected(bool state) { }

	void SetExpandable(bool state) { }

	public override void Paint() { }

	void OnExpanded(Panel panel) { }
}