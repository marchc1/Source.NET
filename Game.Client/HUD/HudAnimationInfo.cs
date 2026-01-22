using Game.Client.HUD;

using Source;
using Source.Common.Commands;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.GUI.Controls;

[DeclareHudElement(Name = "CHudAnimationInfo")]
class HudAnimationInfo : EditableHudElement, IHudElement
{
	const int ANIM_INFO_START_Y = 50;
	int ANIM_INFO_WIDTH;

	[PanelAnimationVar("LabelFont", "DebugFixed")] protected IFont LabelFont;
	[PanelAnimationVar("ItemFont", "DebugFixedSmall")] protected IFont ItemFont;
	[PanelAnimationVar("LabelColor", "DebugLabel")] protected Color LabelColor;
	[PanelAnimationVar("ItemColor", "DebugText")] protected Color ItemColor;

	Panel? Watch;

	public HudAnimationInfo(string panelName) : base(null, "HudAnimationInfo") {
		ANIM_INFO_WIDTH = 300 * (ScreenWidth() / 640);

		Panel parent = clientMode.GetViewport();
		SetParent(parent);

		((IHudElement)this).SetActive(true);

		Watch = null;

		SetZPos(100);
	}

	public bool ShouldDraw() {
		return Watch != null && IHudElement.DefaultShouldDraw(this);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		LabelFont = scheme.GetFont("DebugFixed", true)!;
		ItemFont = scheme.GetFont("DebugFixedSmall", true)!;
		LabelColor = scheme.GetColor("DebugLabel", GetFgColor());
		ItemColor = scheme.GetColor("DebugText", GetFgColor());

		SetPaintBackgroundEnabled(false);
	}

	public void SetWatch(Panel? panel) => Watch = panel;

	void PaintString(ref int x, ref int y, ReadOnlySpan<char> str, Color? LegendColor) {
		// FIXME: Where is my text, its here, its drawing, ... but... not visible, defnitely in bounds and not transparent

		surface.DrawSetTextFont(LabelFont);
		surface.DrawSetTextPos(x, y);

		if (LegendColor != null)
			surface.DrawSetTextColor(LegendColor.Value);
		else
			surface.DrawSetTextColor(new(0, 0, 0, 0));

		surface.DrawPrintText("O->");

		surface.DrawSetTextColor(ItemColor);
		surface.DrawPrintText(str);

		int fontHeight = surface.GetFontTall(LabelFont);
		y += fontHeight;

		if (y + fontHeight > ScreenHeight()) {
			y = ANIM_INFO_START_Y;
			x += ANIM_INFO_WIDTH;
		}
	}

	void PaintMappingInfo(ref int x, ref int y, Panel element, PanelAnimationMap map) {
		if (map == null)
			return;

		surface.DrawSetTextFont(LabelFont);
		surface.DrawSetTextColor(LabelColor);
		surface.DrawSetTextPos(x, y);

		ReadOnlySpan<char> className = "";
		if (map.ClassName != null)
			className = map.ClassName;

		for (int i = 0; i < className.Length; i++)
			surface.DrawChar(className[i]);

		y += surface.GetFontTall(LabelFont) + 1;
		x += 10;

		int c = map.Entries.Count;

		Span<char> sz = stackalloc char[512];
		Span<char> val = stackalloc char[256];
		for (int i = 0; i < c; i++) {
			sz.Clear();
			val.Clear();

			PanelAnimationMapEntry entry = map.Entries[i];

			Color? col = new(0, 0, 0, 0);
			Color? color = null;
			KeyValues kv = new(entry.ScriptName);
			if (element.RequestInfo(kv)) {
				KeyValues? dat = kv.FindKey(entry.ScriptName);
				if (dat != null && dat.Type == KeyValues.Types.Color) {
					col = dat.GetColor();
					sprintf(val, "%i %i %i %i").I(col.Value.R).I(col.Value.G).I(col.Value.B).I(col.Value.A);
					color = col;
				}
				else
					sprintf(val, "%s").S(dat!.GetString());
			}
			else
				sprintf(val, "???");

			sprintf(sz, "%s %s (%s)").S(entry.ScriptName).S(entry.Type).S(val);

			PaintString(ref x, ref y, sz, color);
		}

		x -= 10;

		if (map.BaseMap != null)
			PaintMappingInfo(ref x, ref y, element, map.BaseMap);
	}

	public override void Paint() {
		Panel? panel = Watch;
		if (panel == null)
			return;

		PanelAnimationMap? map = panel.GetAnimMap();
		if (map == null)
			return;

		int x = 15;
		int y = ANIM_INFO_START_Y;

		PaintMappingInfo(ref x, ref y, panel, map);

		x += 10;

		int[] bounds = new int[4];
		panel.GetBounds(out bounds[0], out bounds[1], out bounds[2], out bounds[3]);

		Span<char> buf = stackalloc char[256];
		sprintf(buf, "%s %s (%i %i)").S("Position").S("pos").I(bounds[0]).I(bounds[1]);
		PaintString(ref x, ref y, buf, null);
		sprintf(buf, "%s %s (%i %i)").S("Size").S("size").I(bounds[2]).I(bounds[3]);
		PaintString(ref x, ref y, buf, null);
	}

	// static int HudElementCompletion() {
	// 	// todo
	// }

	[ConCommand("cl_animationinfo", "Hud element to examine.", FCvar.None)]
	static void func(in TokenizedCommand args) {
		if (gHUD.FindElement("HudAnimationInfo") is not HudAnimationInfo info)
			return;

		if (args.ArgC() != 2) {
			info.SetWatch(null);
			return;
		}

		IHudElement? element = null;

		for (int i = 0; i < gHUD.HudList.Count; i++) {
			if (strcmp(gHUD.HudList[i].GetName(), args[1]) == 0) {
				element = gHUD.HudList[i];
				break;
			}
		}

		if (element != null)
			info.SetWatch((Panel)element);
		else {
			// todo
		}
	}
}
