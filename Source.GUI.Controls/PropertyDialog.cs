using Source.Common.Formats.Keyvalues;

namespace Source.GUI.Controls;

public class PropertyDialog : Frame
{
	private PropertySheet? PropertySheet;
	private Button OkButton;
	private Button CancelButton;
	private Button ApplyButton;
	[PanelAnimationVar("sheetinset_bottom", "32")] protected int SheetInsetBottom;

	public PropertyDialog(Panel? parent, string? name) : base(parent, name) {
		PropertySheet = new(this, "Sheet");
		PropertySheet.AddActionSignalTarget(this);
		PropertySheet.SetTabPosition(1);

		OkButton = new(this, "OKButton", "#PropertyDialog_OK");
		OkButton.AddActionSignalTarget(this);
		OkButton.SetTabPosition(2);
		OkButton.SetCommand("OK");
		GetFocusNavGroup().SetDefaultButton(OkButton);

		CancelButton = new(this, "CancelButton", "#PropertyDialog_Cancel");
		CancelButton.AddActionSignalTarget(this);
		CancelButton.SetTabPosition(3);
		CancelButton.SetCommand("Cancel");

		ApplyButton = new(this, "ApplyButton", "#PropertyDialog_Apply");
		ApplyButton.AddActionSignalTarget(this);
		ApplyButton.SetTabPosition(4);
		ApplyButton.SetVisible(false);
		ApplyButton.SetEnabled(false);
		ApplyButton.SetCommand("Apply");

		SetSizeable(false);
	}

	public PropertySheet GetPropertySheet() => PropertySheet!;

	// public Panel GetActivePage() => PropertySheet!.GetActivePage();

	public void AddPage(Panel page, ReadOnlySpan<char> title) {
		// PropertySheet!.AddPage(page, title);
	}

	public void ApplyChanges() {
		OnCommand("Apply");
	}

	public override void PerformLayout() {
		base.PerformLayout();

		int Bottom = SheetInsetBottom;
		if (IsProportional())
			Bottom = SchemeManager.GetProportionalScaledValueEx(GetScheme()!, Bottom);

		GetClientArea(out int x, out int y, out int wide, out int tall);
		PropertySheet!.SetBounds(x, y, wide, tall - Bottom);

		int xpos = x + wide - 80;
		int ypos = tall + y - 28;

		if (ApplyButton.IsVisible()) {
			ApplyButton.SetBounds(xpos, ypos, 72, 24);
			xpos -= 80;
		}

		if (CancelButton.IsVisible()) {
			CancelButton.SetBounds(xpos, ypos, 72, 24);
			xpos -= 80;
		}

		OkButton.SetBounds(xpos, ypos, 72, 24);

		PropertySheet!.InvalidateLayout();
		Repaint();
	}

	public override void OnCommand(ReadOnlySpan<char> command) {
		if (command.Equals("OK", StringComparison.OrdinalIgnoreCase)) {
			if (OnOK(false))
				OnCommand("Close");
			ApplyButton.SetEnabled(false);
		} else if (command.Equals("Cancel", StringComparison.OrdinalIgnoreCase)) {
			OnCancel();
			Close();
		} else if (command.Equals("Apply", StringComparison.OrdinalIgnoreCase)) {
			OnOK(true);
			ApplyButton.SetEnabled(false);
			InvalidateLayout();
		} else
			base.OnCommand(command);
	}

	public void OnCancel() {
		// designed to be overridden
	}

	// public override void OnKeyCodeTyped(ButtonCode code) {
	// 	base.OnKeyCodeTyped(code);
	// }

	public bool OnOK(bool applyOnly) {
		// PropertySheet!.ApplyChanges();

		PostActionSignal(new KeyValues("ApplyChanges"));// todo: make static kv

		return true;
	}

	public void ActivateBuildMode() {
		// EditablePanel panel = PropertySheet!.GetActivePage();

		// if (panel == null)
			// return;

		// panel.ActivateBuildMode();
	}

	public void SetOKButtonText(ReadOnlySpan<char> text) {
		OkButton.SetText(text);
	}

	public void SetCancelButtonText(ReadOnlySpan<char> text) {
		CancelButton.SetText(text);
	}

	public void SetApplyButtonText(ReadOnlySpan<char> text) {
		ApplyButton.SetText(text);
	}

	public void SetOKButtonVisible(bool state) {
		OkButton.SetVisible(state);
		InvalidateLayout();
	}

	public void SetCancelButtonVisible(bool state) {
		CancelButton.SetVisible(state);
		InvalidateLayout();
	}

	public void SetApplyButtonVisible(bool state) {
		ApplyButton.SetVisible(state);
		InvalidateLayout();
	}

	public void OnApplyButtonEnable(bool state) {
		if (ApplyButton.IsEnabled())
			return;

		EnableApplyButton(state);
	}

	public void EnableApplyButton(bool state) {
		ApplyButton.SetEnabled(state);
		InvalidateLayout();
	}

	public override void RequestFocus(int direction) {
		PropertySheet!.RequestFocus(direction);
	}
}
