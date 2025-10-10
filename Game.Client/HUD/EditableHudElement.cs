using Game.Shared;

using Source;
using Source.Common.GUI;
using Source.Common.Hashing;
using Source.GUI.Controls;
namespace Game.Client.HUD;

/// <summary>
/// Combo of HudElement and EditablePanel
/// </summary>
public class EditableHudElement : EditablePanel, IHudElement
{
	public string? ElementName { get; set; }
	public HideHudBits HiddenBits { get; set; }
	public bool Active { get; set; }
	public bool NeedsRemove { get; set; }
	public bool IsParentedToClientDLLRootPanel { get; set; }
	public List<int> HudRenderGroups { get; set; } = [];

	/// <summary>
	/// 
	/// </summary>
	/// <param name="panelName">Panel name comes from the overall name</param>
	/// <param name="elementName">Element name comes from the constructor as its single argument</param>
	public EditableHudElement(string? panelName, string? elementName) : base(null, panelName) {
		ElementName = elementName;
	}

	public virtual void Init() { }
}


public class HudNumericDisplay : Panel
{
	// We define this stuff here, which will be consumed by the hud elements...
	// which is annoying, but no multiple inheritance 
	public string? ElementName { get; set; }
	public HideHudBits HiddenBits { get; set; }
	public bool Active { get; set; } 
	public bool NeedsRemove { get; set; }
	public bool IsParentedToClientDLLRootPanel { get; set; }
	public List<int> HudRenderGroups { get; set; } = [];

	/// <summary>
	/// 
	/// </summary>
	/// <param name="panelName">Panel name comes from the overall name</param>
	/// <param name="elementName">Element name comes from the constructor as its single argument</param>
	public HudNumericDisplay(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {
		Panel vParent = clientMode.GetViewport();
		SetParent(vParent);

		Value = 0;
		LabelText[0] = '\0';
		SecondaryValue = 0;
		DisplayValue = true;
		DisplaySecondaryValue = false;
		Indent = false;
		IsTime = false;
	}
	protected int Value;
	protected int SecondaryValue;
	protected InlineArray32<char> LabelText;
	bool DisplayValue, DisplaySecondaryValue;
	bool Indent;
	bool IsTime;

	[PanelAnimationVar("0")] protected float Blur;
	[PanelAnimationVar("FgColor")] protected Color TextColor;
	[PanelAnimationVar("FgColor")] protected Color Ammo2Color;
	[PanelAnimationVar("HudNumbers")] protected IFont NumberFont;
	[PanelAnimationVar("HudNumbersGlow")] protected IFont NumberGlowFont;
	[PanelAnimationVar("HudNumbersSmall")] protected IFont SmallNumberFont;
	[PanelAnimationVar("Default")] protected IFont TextFont;
	[PanelAnimationVarAliasType("text_xpos", "8", "proportional_float")] protected float text_xpos;
	[PanelAnimationVarAliasType("text_ypos", "20", "proportional_float")] protected float text_ypos;
	[PanelAnimationVarAliasType("digit_xpos", "50", "proportional_float")] protected float digit_xpos;
	[PanelAnimationVarAliasType("digit_ypos", "2", "proportional_float")] protected float digit_ypos;
	[PanelAnimationVarAliasType("digit2_xpos", "98", "proportional_float")] protected float digit2_xpos;
	[PanelAnimationVarAliasType("digit2_ypos", "16", "proportional_float")] protected float digit2_ypos;

	public void SetDisplayValue(int value) {
		Value = value;
	}
	public void SetSecondaryValue(int value) {
		SecondaryValue = value;
	}

	public override void Paint() {
		if (DisplayValue) {
			// draw our numbers
			Surface.DrawSetTextColor(GetFgColor());
			PaintNumbers(NumberFont, digit_xpos, digit_ypos, Value);

			// draw the overbright blur
			for (float fl = Blur; fl > 0.0f; fl -= 1.0f) {
				if (fl >= 1.0f) {
					PaintNumbers(NumberGlowFont, digit_xpos, digit_ypos, Value);
				}
				else {
					Color col = GetFgColor();
					col[3] = (byte)(col[3] * fl);
					Surface.DrawSetTextColor(col);
					PaintNumbers(NumberGlowFont, digit_xpos, digit_ypos, Value);
				}
			}
		}

		if (DisplaySecondaryValue) {
			Surface.DrawSetTextColor(GetFgColor());
			PaintNumbers(SmallNumberFont, digit2_xpos, digit2_ypos, SecondaryValue);
		}

		PaintLabel();
	}

	private void PaintLabel() {
		Surface.DrawSetTextFont(TextFont);
		Surface.DrawSetTextColor(GetFgColor());
		Surface.DrawSetTextPos((int)text_xpos, (int)text_ypos);
		Surface.DrawString(LabelText);
	}

	public void PaintNumbers(IFont font, float xpos, float ypos, float value)
		=> PaintNumbers(font, (int)xpos, (int)ypos, (int)value);
	public void PaintNumbers(IFont font, int xpos, int ypos, int value) {
		Surface.DrawSetTextFont(font);
		Span<char> unicode = stackalloc char[6];
		if (!IsTime)
			sprintf(unicode, "%d").D(value);
		else {
			int minutes = value / 60;
			int seconds = value - minutes * 60;

			if (seconds < 10)
				sprintf(unicode, "%d`0%d").D(minutes).D(seconds);
			else
				sprintf(unicode, "%d`%d").D(minutes).D(seconds);
		}

		int charWidth = Surface.GetCharacterWidth(font, '0');
		if (value < 100 && Indent) 
			xpos += charWidth;
		
		if (value < 10 && Indent) 
			xpos += charWidth;
		
		Surface.DrawSetTextPos(xpos, ypos);
		Surface.DrawString(unicode);
	}
}
