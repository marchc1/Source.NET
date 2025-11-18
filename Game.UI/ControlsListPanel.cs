using Source.Common.GUI;
using Source.Common.Input;
using Source.GUI.Controls;

namespace Game.UI;

class InlineEditPanel : Panel
{
	public InlineEditPanel() : base(null, "InlineEditPanel") { }

	public override void Paint() {
		GetSize(out int wide, out int tall);
		Surface.DrawSetColor(255, 165, 0, 255);
		Surface.DrawFilledRect(0, 0, wide, tall);
	}

	public override void OnKeyCodeTyped(ButtonCode code) {
		if (GetParent() != null)
			GetParent()!.OnKeyCodeTyped(code);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SetBorder(scheme.GetBorder("DepressedButtonBorder"));
	}

	public override void OnMousePressed(ButtonCode code) {
		if (GetParent() != null)
			GetParent()!.OnMousePressed(code);
	}
}

public class ControlsListPanel : SectionedListPanel
{
	InlineEditPanel InlineEditPanel;
	bool CaptureMode;
	int ClickRow;
	IFont Font;
	int MouseX;
	int MouseY;

	public ControlsListPanel(Panel? parent, string? name) : base(parent, name) {
		CaptureMode = false;
		ClickRow = 0;
		InlineEditPanel = new();
		// Font = INVALID_FONT;
	}

	~ControlsListPanel() => InlineEditPanel.MarkForDeletion();

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		Font = scheme.GetFont("Default", IsProportional())!;
	}

	public void StartCaptureMode(HCursor cursor) {
		// todo
	}

	public void EndCaptureMode(HCursor cursor) {
		// todo
	}

	public void SetItemOfInterest(int itemID) => ClickRow = itemID;
	public int GetItemOfInterest() => ClickRow;
	public bool IsCapturing() => CaptureMode;

	public override void OnMousePressed(ButtonCode code) {
		if (IsCapturing()) {
			if (GetParent() != null)
				GetParent()!.OnMousePressed(code);
		}
		else
			base.OnMousePressed(code);
	}

	public override void OnMouseDoublePressed(ButtonCode code) {
		// if (IsItemIDValid(GetSelectedItem()))
		// OnKeyCodePressed(ButtonCode.KeyEnter);
		// else
		// base.OnMouseDoublePressed(code);
	}
}
