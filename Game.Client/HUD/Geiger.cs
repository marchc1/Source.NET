using Game.Shared;

using Source.Common.Bitbuffers;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Game.Client.HUD;

[DeclareHudElement(Name = "CHudGeiger")]
class HudGeiger : EditableHudElement, IHudElement
{
	int GeigerRange;
	TimeUnit_t LastSoundTestTime;

	public HudGeiger(string panelName) : base(null, "HudGeiger") {
		Panel parent = clientMode.GetViewport();
		SetParent(parent);

		LastSoundTestTime = -9999;

		((IHudElement)this).SetHiddenBits(HideHudBits.Health);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SetPaintBackgroundEnabled(false);
	}

	public override void Init() {
		IHudElement.HookMessage("Geiger", MsgFunc_Geiger);
		GeigerRange = 0;
	}

	public void VidInit() => GeigerRange = 0;

	private void MsgFunc_Geiger(bf_read msg) {
		GeigerRange = msg.ReadByte();
		GeigerRange <<= 2;
	}

	public bool ShouldDraw() => GeigerRange > 0 && GeigerRange < 1000 && IHudElement.DefaultShouldDraw(this);

	public override void Paint() {
		int pct;
		float vol = 0;
		bool highsound = false;

		if (gpGlobals.CurTime - LastSoundTestTime < 0.06)
			return;

		LastSoundTestTime = gpGlobals.CurTime;

		if (GeigerRange > 800)
			pct = 0;
		else if (GeigerRange > 600) {
			pct = 2;
			vol = 0.2f;
		}
		else if (GeigerRange > 500) {
			pct = 4;
			vol = 0.25f;
		}
		else if (GeigerRange > 400) {
			pct = 8;
			vol = 0.3f;
			highsound = true;
		}
		else if (GeigerRange > 300) {
			pct = 8;
			vol = 0.35f;
			highsound = true;
		}
		else if (GeigerRange > 200) {
			pct = 28;
			vol = 0.39f;
			highsound = true;
		}
		else if (GeigerRange > 150) {
			pct = 40;
			vol = 0.40f;
			highsound = true;
		}
		else if (GeigerRange > 100) {
			pct = 60;
			vol = 0.425f;
			highsound = true;
		}
		else if (GeigerRange > 75) {
			pct = 80;
			vol = 0.45f;
			highsound = true;
		}
		else if (GeigerRange > 50) {
			pct = 90;
			vol = 0.475f;
		}
		else {
			pct = 95;
			vol = 0.5f;
		}

		vol = (vol * random.RandomInt(0, 127) / 255) + 0.25f;

		if (random.RandomInt(0, 127) < pct) {
			Span<char> sz = stackalloc char[256];
			if (highsound)
				strcpy(sz, "Geiger.BeepHigh");
			else
				strcpy(sz, "Geiger.BeepLow");

			// TODO finish
		}
	}
}
