using Source;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Game.Client.HUD;

// [DeclareHudElement(Name = "CHudBaseTimer")]
class HudBaseTimer : HudNumericDisplay, IHudElement
{
	int Minutes;
	int Seconds;

	[PanelAnimationVar("Alpha", "255")] protected float AlphaOverride;
	[PanelAnimationVar("SecondaryColor", "FgColor")] protected Color FlashColor;

	public HudBaseTimer(string panelName) : base(null, "HudBaseTimer") {
		var parent = clientMode.GetViewport();
		SetParent(parent);

		Minutes = 0;
		Seconds = 0;
		SetLabelText("");
	}

	public void SetMinutes(int mins) => Minutes = mins;
	public void SetSeconds(int secs) => Seconds = secs;

	void PaintTime(IFont font, int xpos, int ypos, int mins, int secs) {
		surface.DrawSetTextFont(font);
		surface.DrawSetTextPos(xpos, ypos);

		Span<char> buff = stackalloc char[6];
		sprintf(buff, "%d:%.2D").D(mins).D(secs);
		surface.DrawString(buff);
	}

	public override void Paint() {
		float alpha = AlphaOverride / 255.0f;
		Color fgColor = GetFgColor();
		fgColor[3] *= (byte)alpha;
		SetFgColor(fgColor);

		surface.DrawSetTextColor(GetFgColor());
		PaintTime(NumberFont, (int)digit_xpos, (int)digit_ypos, Minutes, Seconds);

		for (float fl = Blur; fl > 0.0f; fl -= 1.0f) {
			if (fl >= 1.0f)
				PaintTime(NumberGlowFont, (int)digit_xpos, (int)digit_ypos, Minutes, Seconds);
			else {
				Color col = GetFgColor();
				col[3] = (byte)(col[3] * fl);
				surface.DrawSetTextColor(col);
				PaintTime(NumberGlowFont, (int)digit_xpos, (int)digit_ypos, Minutes, Seconds);
			}
		}

		PaintLabel();
	}

	public void SetToPrimaryColor() => SetFgColor(TextColor);
	public void SetToSecondaryColor() => SetFgColor(FlashColor);
}