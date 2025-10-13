using Source.Common.Input;

namespace Source.GUI.Controls;

public class PropertyPage : EditablePanel
{
	// Handle PageTab;
	Panel? PageTab;

	public PropertyPage(Panel? parent, string? name) : base(parent, name) {

	}

	public void OnResetData() { }
	public void OnApplyChanges() { }
	public void OnPageShow() { }
	public void OnPageHide() { }

	public void OnPageTabActivated(Panel pageTab) {
		PageTab = pageTab;
	}

	public override void OnKeyCodeTyped(ButtonCode code) {
		switch (code) {
			case ButtonCode.KeyRight:
			case ButtonCode.KeyLeft:
				if (PageTab != null && PageTab.HasFocus())
					base.OnKeyCodeTyped(code);
				break;
			default:
				base.OnKeyCodeTyped(code);
				break;
		}
	}

	public override void SetVisible(bool state) {
		if (IsVisible() && state)
			if (GetFocusNavGroup().GetCurrentDefaultButton() != null)
				GetFocusNavGroup().SetCurrentDefaultButton(null);

		base.SetVisible(state);
	}
}
