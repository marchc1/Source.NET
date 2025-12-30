using Source.Common.Formats.Keyvalues;

namespace Source.GUI.Controls;

class BaseInputDialog : Frame
{
	KeyValues ContextKeyValues;
	Button CancelButton;
	Button OKButton;

	public BaseInputDialog(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}

	public override void PerformLayout() { }

	void PerformLayout(int x, int y, int w, int h) { }

	public override void OnCommand(ReadOnlySpan<char> command) { }

	void CleanUpContextKeyValues() { }
}

class InputMessageBox : BaseInputDialog
{
	Label Prompt;

	public InputMessageBox(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}

	void CleanUpContextKeyValues() { }

	void DoModal(KeyValues contextKeyValues) { }

	public override void PerformLayout() { }

	public override void OnCommand(ReadOnlySpan<char> command) { }
}

class InputDialog : BaseInputDialog
{
	Label Prompt;
	TextEntry Input;

	public InputDialog(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {

	}

	void SetMultiline(bool state) { }

	void AllowNumericInputOnly(bool onlyNumeric) { }

	void PerformLayout(int x, int y, int w, int h) { }

	public override void OnCommand(ReadOnlySpan<char> command) { }
}