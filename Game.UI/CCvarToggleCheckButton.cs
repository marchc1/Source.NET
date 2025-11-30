using Source.Common.Commands;
using Source.Common.Formats.Keyvalues;
using Source.GUI.Controls;

namespace Game.UI;

public class CCvarToggleCheckButton : CheckButton
{
	public static Panel Create_CCvarToggleCheckButton() => new CCvarToggleCheckButton(null, null, "CvarToggleCheckButton", null);

	string? CvarName;
	bool StartValue;

	public CCvarToggleCheckButton(Panel parent, ReadOnlySpan<char> name, ReadOnlySpan<char> text, ReadOnlySpan<char> cvarName) : base(parent, name, text) {
		CvarName = cvarName.Length > 0 ? new(cvarName) : null;
		if (CvarName != null)
			Reset();
		AddActionSignalTarget(this);
	}

	public override void Paint() {
		if (CvarName == null || CvarName.Length == 0) {
			base.Paint();
			return;
		}

		ConVarRef var = new(CvarName);
		if (!var.IsValid())
			return;

		bool value = var.GetBool();

		if (value != StartValue) {
			SetSelected(value);
			StartValue = value;
		}

		base.Paint();
	}

	public void ApplyChanges() {
		if (CvarName == null || CvarName.Length == 0)
			return;

		StartValue = IsSelected();
		ConVarRef var = new(CvarName);
		if (!var.IsValid())
			return;
		var.SetValue(StartValue);
	}

	public void Reset() {
		if (CvarName == null || CvarName.Length == 0)
			return;

		ConVarRef var = new(CvarName);
		if (!var.IsValid())
			return;

		StartValue = var.GetBool();
		SetSelected(StartValue);
	}

	public bool HasBeenModified() => IsSelected() != StartValue;

	static readonly KeyValues KV_ControlModified = new("ControlModified");
	public void OnButtonChecked() {
		if (HasBeenModified())
			PostActionSignal(KV_ControlModified);
	}

	public override void ApplySettings(KeyValues resourceData) {
		base.ApplySettings(resourceData);

		ReadOnlySpan<char> cvarName = resourceData.GetString("cvar_name", "");
		ReadOnlySpan<char> cvarValue = resourceData.GetString("cvar_value", "");

		if (cvarName.Equals("", StringComparison.Ordinal))
			return;

		CvarName = cvarName.Length > 0 ? new(cvarName) : null;

		if (cvarValue.Equals("1", StringComparison.Ordinal))
			StartValue = true;
		else
			StartValue = false;

		ConVarRef var = new(CvarName);
		if (var.IsValid()) {
			if (var.GetBool())
				SetSelected(true);
			else
				SetSelected(false);
		}
	}
}
