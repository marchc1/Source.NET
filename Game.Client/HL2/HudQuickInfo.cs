using Game.Client.HUD;
using Game.Shared;

using Source;
using Source.Common.Commands;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Game.Client.HL2;

[DeclareHudElement(Name = "CHUDQuickInfo")]
public class HUDQuickInfo : HudNumericDisplay, IHudElement
{
	static ConVar hud_quickinfo = new("hud_quickinfo", "1", FCvar.Archive);

	const float QUICKINFO_EVENT_DURATION = 1.0f;
	const int QUICKINFO_BRIGHTNESS_FULL = 255;
	const int QUICKINFO_BRIGHTNESS_DIM = 64;
	const float QUICKINFO_FADE_IN_TIME = 0.5f;
	const float QUICKINFO_FADE_OUT_TIME = 2.0f;
	const int HEALTH_WARNING_THRESHOLD = 25;

	int LastAmmo;
	int LastHealth;
	TimeUnit_t AmmoFade;
	TimeUnit_t HealthFade;
	bool WarnAmmo;
	bool WarnHealth;
	bool FadedOut;
	bool Dimmed;
	TimeUnit_t LastEventTime;
	HudTexture? IconCrosshair;
	HudTexture? IconRightFull;
	HudTexture? IconLeftFull;
	HudTexture? IconRightEmpty;
	HudTexture? IconLeftEmpty;
	HudTexture? IconRight;
	HudTexture? IconLeft;

	public HUDQuickInfo(string? panelName) : base(null, "HUDQuickInfo") {
		var parent = clientMode.GetViewport();
		SetParent(parent);

		((IHudElement)this).SetHiddenBits(HideHudBits.Crosshair);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SetPaintBackgroundEnabled(false);
		SetForceStereoRenderToFrameBuffer(true);
	}

	public void Init() {
		AmmoFade = 0;
		HealthFade = 0;
		LastAmmo = 0;
		LastHealth = 100;
		WarnAmmo = false;
		WarnHealth = false;
		FadedOut = false;
		Dimmed = false;
		LastEventTime = 0.0f;
	}

	public void VidInit() {
		Init();

		IconCrosshair = gHUD.GetIcon("crosshair");
		IconRightFull = gHUD.GetIcon("crosshair_right_full");
		IconLeftFull = gHUD.GetIcon("crosshair_left_full");
		IconRightEmpty = gHUD.GetIcon("crosshair_right_empty");
		IconLeftEmpty = gHUD.GetIcon("crosshair_left_empty");
		IconRight = gHUD.GetIcon("crosshair_right");
		IconLeft = gHUD.GetIcon("crosshair_left");
	}

	void DrawWarning(int x, int y, HudTexture Icon, ref TimeUnit_t time) {
		float scale = (int)(MathF.Abs(MathF.Sin((float)(gpGlobals.CurTime * 8.0f))) * 128.0f);

		if (time <= (gpGlobals.CurTime * 200.0f)) {
			if (scale < 40) {
				time = 0.0f;
				return;
			}
			else
				time += gpGlobals.FrameTime * 200.0f;
		}

		time -= gpGlobals.FrameTime * 200.0f;

		Color caution = gHUD.ClrCaution;
		caution[3] = (byte)(scale * 255);

		Icon.DrawSelf(x, y, caution);
	}

	public bool ShouldDraw() {
		if (IconCrosshair == null || IconRightFull == null || IconLeftFull == null || IconRightEmpty == null || IconLeftEmpty == null || IconRight == null || IconLeft == null)
			return false;

		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return false;

		if (!HudCrosshair.crosshair.GetBool())
			return false;

		return /*((IHudElement)this).ShouldDraw() &&*/ !engine.IsDrawingLoadingImage();
	}

	public override void OnThink() {
		base.OnThink();

		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return;

		bool fadeOut = false;//player.IsZoomed(); // todo

		if (FadedOut != fadeOut) {
			FadedOut = fadeOut;

			Dimmed = false;

			if (fadeOut)
				clientMode.GetViewportAnimationController()!.RunAnimationCommand(this, "Alpha", 0.0f, 0.0f, 0.25f, Interpolators.Linear);
			else
				clientMode.GetViewportAnimationController()!.RunAnimationCommand(this, "Alpha", QUICKINFO_BRIGHTNESS_FULL, 0.0f, QUICKINFO_FADE_IN_TIME, Interpolators.Linear);
		}
		else if (!FadedOut) {
			if (EventTimeElapsed()) {
				if (!Dimmed) {
					Dimmed = true;
					clientMode.GetViewportAnimationController()!.RunAnimationCommand(this, "Alpha", QUICKINFO_BRIGHTNESS_DIM, 0.0f, QUICKINFO_FADE_OUT_TIME, Interpolators.Linear);
				}
			}
			else if (Dimmed) {
				Dimmed = false;
				clientMode.GetViewportAnimationController()!.RunAnimationCommand(this, "Alpha", QUICKINFO_BRIGHTNESS_FULL, 0.0f, QUICKINFO_FADE_IN_TIME, Interpolators.Linear);
			}
		}
	}

	public override void Paint() {
		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return;

		BaseCombatWeapon? weapon = BaseCombatWeapon.GetActiveWeapon();
		if (weapon == null)
			return;

		HudCrosshair.GetDrawPosition(out float fX, out float fY, out bool bBehindCamera);

		if (bBehindCamera)
			return;

		int xCenter = (int)fX;
		int yCenter = (int)fY - IconLeftFull!.Height() / 2;

		float scalar = 138.0f / 255.0f;

		int health = player.GetHealth();
		if (health != LastHealth) {
			UpdateEventTime();
			LastHealth = health;

			if (health <= HEALTH_WARNING_THRESHOLD) {
				if (WarnHealth == false) {
					HealthFade = 255;
					WarnHealth = true;

					// CLocalPlayerFilter filter;
					// C_BaseEntity.EmitSound(filter, -1, "HUDQuickInfo.LowHealth");//SOUND_FROM_LOCAL_PLAYER
				}
			}
			else
				WarnHealth = false;
		}

		int ammo = weapon.Clip1;//();
		if (ammo != LastAmmo) {
			UpdateEventTime();
			LastAmmo = ammo;

			float ammoPerc;
			if (weapon.GetMaxClip1() <= 0)
				ammoPerc = 0.0f;
			else
				ammoPerc = ammo / weapon.GetMaxClip1();

			if ((weapon.GetMaxClip1() > 1) && (ammoPerc <= (1.0f - 0.75f/*CLIP_PERC_THRESHOLD*/))) {
				if (WarnAmmo == false) {
					AmmoFade = 255;
					WarnAmmo = true;

					// CLocalPlayerFilter filter;
					// C_BaseEntity.EmitSound(filter, -1, "HUDQuickInfo.LowAmmo");//SOUND_FROM_LOCAL_PLAYER
				}
			}
			else
				WarnAmmo = false;
		}

		Color clrNormal = gHUD.ClrNormal;
		clrNormal[3] = (byte)(255 * scalar);
		IconCrosshair!.DrawSelf(xCenter, yCenter, clrNormal);

		if (hud_quickinfo.GetInt() == 0)
			return;

		int sinScale = (int)(MathF.Abs(MathF.Sin((float)(gpGlobals.CurTime * 8.0f))) * 128.0f);

		if (HealthFade > 0.0f) {
			DrawWarning(xCenter - (IconLeftFull.Width() * 2), yCenter, IconLeftFull, ref HealthFade);
		}
		else {
			float healthPerc = health / 100.0f;
			healthPerc = Math.Clamp(healthPerc, 0.0f, 1.0f);

			Color healthColor = WarnHealth ? gHUD.ClrCaution : gHUD.ClrNormal;

			if (WarnHealth)
				healthColor[3] = (byte)(255 * sinScale);
			else
				healthColor[3] = (byte)(255 * scalar);

			gHUD.DrawIconProgressBar(xCenter - (IconLeftFull.Width() * 2), yCenter, IconLeftFull, IconLeftEmpty!, (1.0f - healthPerc), healthColor, ProgressBarType.Vertical);
		}

		if (AmmoFade > 0.0f)
			DrawWarning(xCenter + IconRightFull!.Width(), yCenter, IconRightFull, ref AmmoFade);
		else {
			float ammoPerc;

			if (weapon.GetMaxClip1() <= 0)
				ammoPerc = 0.0f;
			else {
				ammoPerc = 1.0f - (ammo / weapon.GetMaxClip1());
				ammoPerc = Math.Clamp(ammoPerc, 0.0f, 1.0f);
			}

			Color ammoColor = WarnAmmo ? gHUD.ClrCaution : gHUD.ClrNormal;

			if (WarnAmmo)
				ammoColor[3] = (byte)(255 * sinScale);
			else
				ammoColor[3] = (byte)(255 * scalar);

			gHUD.DrawIconProgressBar(xCenter + IconRightFull!.Width(), yCenter, IconRightFull, IconRightEmpty!, ammoPerc, ammoColor, ProgressBarType.Vertical);
		}
	}

	void UpdateEventTime() => LastEventTime = gpGlobals.CurTime;
	bool EventTimeElapsed() => gpGlobals.CurTime - LastEventTime > QUICKINFO_EVENT_DURATION;
}
