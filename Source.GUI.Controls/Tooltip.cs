using Source.Common.GUI;
using Source.GUI.Controls;

class BaseTooltip
{
	Panel Parent;
	List<string> Text;
	int Delay;
	int TooltipDelay;
	bool MakeVisible;
	bool DisplayOnOneLine;
	bool IsDirty;
	bool Enabled;

	public BaseTooltip(Panel parent, ReadOnlySpan<char> text) { }

	void ResetDelay() { }

	void SetTooltipDelay(int tooltipDelay) { }

	// int GetTooltipDelay() { }

	void SetTooltipFormatToSingleLine() { }

	void SetTooltipFormatToMultiLine() { }

	void ShowTooltip(Panel currentPanel) { }

	void SetEnabled(bool state) { }

	// bool ShouldLayout() { }

	void HideTooltip() { }

	void SetText(ReadOnlySpan<char> text) { }

	// ReadOnlySpan<char> GetText() { }

	void PositionWindow(Panel tipPanel) { }
}

class TextTooltip : BaseTooltip
{
	public TextTooltip(Panel parent, ReadOnlySpan<char> text) : base(parent, text) {

	}

	void SetText(ReadOnlySpan<char> text) { }

	void ApplySchemeSettings(IScheme pScheme) { }

	void ShowTooltip(Panel currentPanel) { }

	void PerformLayout() { }

	void SizeTextWindow() { }

	void HideTooltip() { }
}