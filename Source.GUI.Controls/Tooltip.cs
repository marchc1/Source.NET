using Source.Common.GUI;
using Source.Common.Launcher;
using Source.GUI.Controls;

public class BaseTooltip : IDisposable
{
	public ISystem System = Singleton<ISystem>();
	public IVGuiInput Input = Singleton<IVGuiInput>();
	public ISurface Surface = Singleton<ISurface>();

	Panel Parent;
	public string Text;
	long Delay;
	int TooltipDelay;
	public bool MakeVisible;
	public bool DisplayOnOneLine;
	public bool IsDirty;
	bool Enabled;

	public BaseTooltip(Panel parent, ReadOnlySpan<char> text) {
		SetText(text);

		DisplayOnOneLine = false;
		MakeVisible = false;
		IsDirty = false;
		Enabled = true;

		TooltipDelay = 500; // TODO: tooltip_delay from gmod
		Delay = 0;
	}

	public virtual void Dispose() {
		GC.SuppressFinalize(this);
	}

	public void ResetDelay() {
		IsDirty = true;
		Delay = System.GetTimeMillis() + TooltipDelay;
	}

	public void SetTooltipDelay(int tooltipDelay) => TooltipDelay = tooltipDelay;

	public int GetTooltipDelay() => TooltipDelay;

	public void SetTooltipFormatToSingleLine() {
		DisplayOnOneLine = true;
		IsDirty = true;
	}

	public void SetTooltipFormatToMultiLine() {
		DisplayOnOneLine = false;
		IsDirty = true;
	}

	public virtual void ShowTooltip(Panel currentPanel) {
		MakeVisible = true;
		PerformLayout();
	}

	public void SetEnabled(bool state) => Enabled = state;

	public bool ShouldLayout() {
		if (!MakeVisible)
			return false;

		if (Delay > System.GetTimeMillis())
			return false;

		return IsDirty;
	}

	public virtual void HideTooltip() {
		MakeVisible = false;
		IsDirty = true;
	}

	public virtual void SetText(ReadOnlySpan<char> text) {
		IsDirty = true;
		Text = text.ToString();
		TextTooltip.TooltipWindow?.SetText(text);
	}

	public ReadOnlySpan<char> GetText() => Text;

	public void PositionWindow(Panel tipPanel) {
		tipPanel.GetSize(out int tipW, out int tipH);
		Input.GetCursorPos(out int cursorX, out int cursorY);
		Surface.GetScreenSize(out int wide, out int tall);

		int parentX = 0, parentY = 0;
		if (!tipPanel.IsPopup()) {
			Panel Parent = tipPanel.GetParent()!;
			Parent.GetPos(out parentX, out parentY);
			Parent.LocalToScreen(ref parentX, ref parentY);
		}

		cursorX -= parentX;
		cursorY -= parentY;

		if (wide - tipW > cursorX) {
			cursorY += 20;

			if (tall - tipH > cursorY)
				tipPanel.SetPos(cursorX, cursorY);
			else
				tipPanel.SetPos(cursorX, cursorY - tipH - 20);
		}
		else {
			if (tall - tipH > cursorY)
				tipPanel.SetPos(cursorX - tipW, cursorY);
			else
				tipPanel.SetPos(cursorX - tipW, cursorY - tipH - 20);
		}

	}

	public virtual void PerformLayout() { }
}

class TextTooltip : BaseTooltip
{
	public static TextEntry? TooltipWindow;
	static int TooltipWindowCount = 0;

	public TextTooltip(Panel parent, ReadOnlySpan<char> text) : base(parent, text) {
		if (TooltipWindow == null) {
			TooltipWindow = new TextEntry(null, "tooltip");
			TooltipWindow.InvalidateLayout(false, true);

			IScheme scheme = TooltipWindow.GetScheme()!;
			TooltipWindow.SetBgColor(TooltipWindow.GetSchemeColor("Tooltip.BgColor", TooltipWindow.GetBgColor(), scheme));
			TooltipWindow.SetFgColor(TooltipWindow.GetSchemeColor("Tooltip.TextColor", TooltipWindow.GetFgColor(), scheme));
			TooltipWindow.SetBorder(scheme.GetBorder("ToolTipBorder"));
			TooltipWindow.SetFont(scheme.GetFont("DefaultSmall", TooltipWindow.IsProportional()));
		}

		TooltipWindowCount++;

		TooltipWindow.MakePopup();
		TooltipWindow.SetKeyboardInputEnabled(false);
		TooltipWindow.SetMouseInputEnabled(false);

		SetText(text);
		TooltipWindow.SetText(text);
		TooltipWindow.SetEditable(false);
		TooltipWindow.SetMultiline(true);
		TooltipWindow.SetVisible(false);
	}

	public override void Dispose() {
		base.Dispose();

		if (--TooltipWindowCount < 1) {
			TooltipWindow?.MarkForDeletion();
			TooltipWindow = null;
		}
	}

	public override void SetText(ReadOnlySpan<char> text) {
		base.SetText(text);
		TooltipWindow?.SetText(text);
	}

	void ApplySchemeSettings(IScheme pScheme) =>
		TooltipWindow?.SetFont(pScheme.GetFont("DefaultSmall", TooltipWindow.IsProportional())!);

	public override void ShowTooltip(Panel currentPanel) {
		if (TooltipWindow != null) {
			int len = TooltipWindow.GetTextLength();
			if (len <= 0) {
				MakeVisible = false;
				return;
			}

			Panel CurrentParent = TooltipWindow.GetParent()!;

			IsDirty = IsDirty || (CurrentParent != currentPanel);
			TooltipWindow.SetText(Text);
			TooltipWindow.SetParent(currentPanel);
		}

		base.ShowTooltip(currentPanel);
	}

	public override void PerformLayout() {
		if (!ShouldLayout())
			return;

		if (TooltipWindow == null)
			return;

		IsDirty = false;

		TooltipWindow.SetVisible(true);
		TooltipWindow.MakePopup(false, true);
		TooltipWindow.SetKeyboardInputEnabled(false);
		TooltipWindow.SetMouseInputEnabled(false);

		SizeTextWindow();
		PositionWindow(TooltipWindow);
	}

	void SizeTextWindow() {
		if (TooltipWindow == null)
			return;

		if (DisplayOnOneLine) {
			TooltipWindow.SetMultiline(false);
			TooltipWindow.SetToFullWidth();
		}
		else {
			TooltipWindow.SetMultiline(false);
			TooltipWindow.SetToFullWidth();
			TooltipWindow.GetSize(out int wide, out int tall);
			double newWide = Math.Sqrt((2.0 / 1) * wide * tall);
			double newTall = (1.0 / 2) * newWide;
			TooltipWindow.SetMultiline(true);
			TooltipWindow.SetSize((int)newWide, (int)newTall);
			// TooltipWindow.SetToFullHeight(); // todo
			TooltipWindow.GetSize(out wide, out tall);

			if (wide < 100) { // && (TooltipWindow.GetNumLines() == 2) todo
				TooltipWindow.SetMultiline(false);
				TooltipWindow.SetToFullWidth();
			}
			else {
				while (((float)wide / (float)tall) < 2.0f) {
					TooltipWindow.SetSize(wide + 1, tall);
					// TooltipWindow.SetToFullHeight();
					TooltipWindow.GetSize(out wide, out tall);
				}
			}
		}
	}

	public override void HideTooltip() {
		// TooltipWindow?.SetVisible(false);
		// base.HideTooltip();
	}
}