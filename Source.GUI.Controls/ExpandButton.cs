using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;

namespace Source.GUI.Controls;

class ExpandButton : ToggleButton
{
	bool Expandable;
	IFont? Font;
	Color Color;

	public ExpandButton(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName, "") {
		Expandable = true;
		Font = null;
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		Color = GetSchemeColor("ExpandButton.Color", scheme);
		Font = scheme.GetFont("Marlett", IsProportional());

		SetPaintBackgroundEnabled(false);
	}

	public override IBorder? GetBorder(bool depressed, bool armed, bool selected, bool keyfocus) => null;

	public override void SetSelected(bool state) {
		if (Expandable && state != IsSelected()) {
			KeyValues nsg = new("Expanded", "state", state ? 1 : 0);
			PostActionSignal(nsg);
			base.SetSelected(state);
		}
	}

	public void SetExpandable(bool state) {
		Expandable = state;
		Repaint();
	}

	public override void Paint() {
		Surface.DrawSetTextFont(Font!);

		char ch = IsSelected() ? '6' : '4';

		GetSize(out int w, out int h);
		Surface.GetTextSize(Font!, ch.ToString(), out int tw, out int th);
		Surface.DrawSetTextColor(Color);
		Surface.DrawSetTextPos((w - tw) / 2, (h - th) / 2);
		Surface.DrawChar(ch);
	}

	void OnExpanded(Panel panel) { }
}