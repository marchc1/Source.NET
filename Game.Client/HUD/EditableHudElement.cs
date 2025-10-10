using Game.Shared;

using Source;
using Source.Common.GUI;
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
	public HudNumericDisplay(string? panelName, string? elementName) : base(null, panelName) {
		ElementName = elementName;
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
}
