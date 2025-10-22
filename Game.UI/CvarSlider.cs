using Source;
using Source.Common.Commands;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Game.UI;

public class CvarSlider : Slider
{
	[PanelAnimationVar("use_convar_minmax", "0", "bool")] bool UseCvarMinMax;
	bool AllowOutOfRange;
	bool ModifiedOnce;
	float StartValue;
	int iStartValue;
	float LastSliderValue;
	float CurrentValue;
	char[]? CvarName = new char[64];
	bool CreatedInCode;
	float MinValue;
	float MaxValue;
	public CvarSlider(Panel panel, ReadOnlySpan<char> name) : base(panel, name) {
		SetupSlider(0, 1, "", false);
		CreatedInCode = false;
		AddActionSignalTarget(this);
	}

	public CvarSlider(Panel parent, ReadOnlySpan<char> name, ReadOnlySpan<char> text, float minValue, float maxValue, ReadOnlySpan<char> cvarName, bool allowOutOfRange) : base(parent, name) {
		AddActionSignalTarget(this);
		SetupSlider(minValue, maxValue, cvarName, allowOutOfRange);
		CreatedInCode = true;
	}

	public void SetupSlider(float minValue, float maxValue, ReadOnlySpan<char> cvarName, bool allowOutOfRange) {
		ConVarRef var = new(cvarName, true);

		if (var.IsValid()) {
			float cvarMin;
			if (var.GetMin(out double CVarMin)) {
				cvarMin = (float)CVarMin;
				minValue = UseCvarMinMax ? cvarMin : Math.Max(minValue, cvarMin);
			}

			float cvarMax;
			if (var.GetMax(out double CVarMax)) {
				cvarMax = (float)CVarMax;
				maxValue = UseCvarMinMax ? cvarMax : Math.Min(maxValue, cvarMax);
			}
		}

		MinValue = minValue;
		MaxValue = maxValue;

		SetRange((int)(100.0f * MinValue), (int)(100.0f * MaxValue));

		Span<char> min = stackalloc char[32];
		Span<char> max = stackalloc char[32];

		minValue.TryFormat(min, out int minLen, "F2");
		maxValue.TryFormat(max, out int maxLen, "F2");

		SetTickCaptions(min, max);

		cvarName.CopyTo(CvarName);

		ModifiedOnce = false;
		AllowOutOfRange = allowOutOfRange;

		Reset();
	}

	public void SetTickColor(Color color) => TickColor = color;

	public override void ApplySettings(KeyValues resourceData) {
		base.ApplySettings(resourceData);

		if (!CreatedInCode) {
			float minValue = resourceData.GetFloat("minvalue", 0);
			float maxValue = resourceData.GetFloat("maxvalue", 1);
			ReadOnlySpan<char> cvarName = resourceData.GetString("cvar_name", "");
			bool allowOutOfRange = resourceData.GetInt("allow_out_of_range", 0) != 0;
			SetupSlider(minValue, maxValue, cvarName, allowOutOfRange);

			// HACK: If our parent is a property page, we want the dialog containing it
			if (GetParent() is PropertyPage && GetParent()!.GetParent() != null)
				GetParent()!.GetParent()!.AddActionSignalTarget(this);
			else
				GetParent()!.AddActionSignalTarget(this);
		}
	}

	public override void GetSettings(KeyValues outResourceData) {
		base.GetSettings(outResourceData);

		if (!CreatedInCode) {
			outResourceData.SetFloat("minvalue", MinValue);
			outResourceData.SetFloat("maxvalue", MaxValue);
			outResourceData.SetString("cvar_name", CvarName!);
			outResourceData.SetInt("allow_out_of_range", AllowOutOfRange ? 1 : 0);
		}
	}

	public void SetCVarName(ReadOnlySpan<char> cvarName) {
		cvarName.CopyTo(CvarName);
		ModifiedOnce = false;
		Reset();
	}

	public void SetMinMaxValues(float minValue, float maxValue, bool setTickDisplay) {
		SetRange((int)(100.0f * minValue), (int)(100.0f * maxValue));

		if (setTickDisplay) {
			Span<char> min = stackalloc char[32];
			Span<char> max = stackalloc char[32];

			minValue.TryFormat(min, out int minLen, "F2");
			maxValue.TryFormat(max, out int maxLen, "F2");

			SetTickCaptions(min, max);
		}

		Reset();
	}

	public override void Paint() {
		ConVarRef var = new(CvarName!, true);
		if (!var.IsValid())
			return;

		float curValue = var.GetFloat();
		if (curValue != StartValue) {
			int value = (int)(curValue * 100.0f);
			StartValue = curValue;
			CurrentValue = curValue;

			SetValue(value);
			iStartValue = GetValue();
		}
		base.Paint();
	}

	public void ApplyChanges() {
		if (ModifiedOnce) {
			iStartValue = GetValue();
			if (AllowOutOfRange)
				StartValue = CurrentValue;
			else
				StartValue = iStartValue / 100.0f;

			ConVarRef var = new(CvarName!, true);
			if (!var.IsValid())
				return;
			var.SetValue(StartValue);
		}
	}

	public float GetSliderValue() {
		if (AllowOutOfRange)
			return CurrentValue;
		else
			return GetValue() / 100.0f;
	}

	public void SetSliderValue(float value) {
		int val = (int)(value * 100.0f);
		SetValue(val, false);

		LastSliderValue = value;

		if (CurrentValue != value) {
			CurrentValue = value;
			ModifiedOnce = true;
		}
	}

	public void Reset() {
		ConVarRef var = new(CvarName!, true);

		if (!var.IsValid()) {
			CurrentValue = StartValue = 0.0f;
			SetValue(0, false);
			iStartValue = GetValue();
			LastSliderValue = iStartValue;
			return;
		}

		StartValue = var.GetFloat();
		CurrentValue = StartValue;

		int value = (int)(StartValue * 100.0f);
		SetValue(value, false);

		iStartValue = GetValue();
		LastSliderValue = iStartValue;
	}

	public bool HasBeenModified() {
		if (GetValue() != iStartValue)
			ModifiedOnce = true;
		return ModifiedOnce;
	}

	public void OnSliderMoved() {
		if (HasBeenModified()) {
			if (LastSliderValue != GetValue()) {
				LastSliderValue = GetValue();
				CurrentValue = GetValue() / 100.0f;
			}

			PostActionSignal(new KeyValues("ControlModified"));
		}
	}

	// todo complete

	public override void OnMessage(KeyValues message, IPanel? from) {
		switch (message.Name) {
			case "SliderMoved":
				OnSliderMoved();
				break;
			case "SliderDragEnd":
				// OnSliderDragEnd();
				break;
			default:
				base.OnMessage(message, from);
				break;
		}
	}
}
