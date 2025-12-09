using Source.Common.Formats.Keyvalues;
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
	IFont? Font;
	int MouseX;
	int MouseY;

	public ControlsListPanel(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {
		CaptureMode = false;
		ClickRow = 0;
		InlineEditPanel = new();
	}

	// FIXME #37
	public override void Dispose() {
		base.Dispose();
		InlineEditPanel.MarkForDeletion();
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		Font = scheme.GetFont("Default", IsProportional())!;
	}

	public void StartCaptureMode(CursorCode? cursor) {
		CaptureMode = true;
		// EnterEditMode(ClickRow, 1, InlineEditPanel);
		Input.SetMouseFocus(InlineEditPanel);
		Input.SetMouseCapture(InlineEditPanel);

		// InputSystem.SetNovintPure(true);

		// Engine.StartKeyTrapMode();

		if (cursor != null) {
			InlineEditPanel.SetCursor(cursor.Value);
			Input.GetCursorPos(out MouseX, out MouseY);
		}
	}

	public void EndCaptureMode(CursorCode? cursor) {
		CaptureMode = false;
		Input.SetMouseCapture(null);
		LeaveEditMode();
		RequestFocus();
		Input.SetMouseFocus(this);

		// InputSystem.SetNovintPure(false);

		if (cursor != null) {
			InlineEditPanel.SetCursor(cursor.Value);
			Input.SetCursorPos(MouseX, MouseY);

			if (cursor != CursorCode.None)
				Input.SetCursorPos(MouseX, MouseY);
		}
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
