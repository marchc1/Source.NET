using Game.Client.HUD;
using Game.Shared;

using Source;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Game.Client.HL2;

[DeclareHudElement(Name = "CHudHealth")]
public class HudHealth : HudNumericDisplay, IHudElement
{
	public HudHealth(string? panelName) : base(null, "HudHealth") {
		var parent = HLClient.ClientMode!.GetViewport();
		SetParent(parent);
	}

}
