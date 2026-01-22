using Game.Client.HUD;
using Game.Shared;

using Source;
using Source.Common.Commands;
using Source.Common.GUI;
using Source.Common.Mathematics;
using Source.GUI.Controls;

using System.Numerics;

namespace Game.Client.HUD;

[DeclareHudElement(Name = "CHudCrosshair")]
public class HudCrosshair : EditableHudElement, IHudElement
{
	public static ConVar crosshair = new("crosshair", "1", FCvar.Archive);
	static ConVar cl_observercrosshair = new("cl_observercrosshair", "1", FCvar.Archive);

	HudTexture? Crosshair;
	HudTexture? DefaultCrosshair;
	Color ClrCrosshair;
	QAngle CrosshairOffsetAngle;
	[PanelAnimationVar("never_draw", "false", "bool")] protected bool HideCrosshair;

	public HudCrosshair(string? panelName) : base(null, "CHudCrosshair") {
		var parent = clientMode.GetViewport();
		SetParent(parent);
		Crosshair = null;
		ClrCrosshair = new(0, 0, 0, 0);
		CrosshairOffsetAngle.Init();

		((IHudElement)this).SetHiddenBits(HideHudBits.PlayerDead | HideHudBits.Crosshair);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		DefaultCrosshair = gHUD.GetIcon("crosshair_default");
		SetPaintBackgroundEnabled(false);

		SetSize(ScreenWidth(), ScreenHeight());
		SetForceStereoRenderToFrameBuffer(true);
	}

	public bool ShouldDraw() {
		bool needsDraw;

		if (HideCrosshair)
			return false;

		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return false;

		BaseCombatWeapon? weapon = player.GetActiveWeapon();
		if (weapon != null /*&& !weapon.ShouldDrawCrosshair()*/) // todo
			return false;

		// A lot of this needs to be added (todo)
		// needsDraw = Crosshair != null &&
		// 			!engine.IsDrawingLoadingImage() &&
		// 			!engine.IsPaused() &&
		// 			clientMode.ShouldDrawCrosshair() &&
		// 			(player.GetFlags() & PlayerFlags.Frozen) == 0 &&
		// 			(player.GetEntityIndex() == render.GetViewEntity()) &&
		// 			!player.IsInVGuiInputMode() &&
		// 			(player.IsAlive() || (player.GetObserverMode() == ObserverMode.InEye) || (cl_observercrosshair.GetBool() && player.GetObserverMode() == OBS_MODE_ROAMING));

		// so for now, ill just do this
		needsDraw = Crosshair != null && !engine.IsDrawingLoadingImage() && !engine.IsPaused() && crosshair.GetBool();
		return needsDraw && IHudElement.DefaultShouldDraw(this);
	}

	public static void GetDrawPosition(out float px, out float py, out bool behindCamera, QAngle angleCrosshairOffset = default) {
		QAngle curViewAngles = CurrentViewAngles();
		Vector3 curViewOrigin = CurrentViewOrigin();

		surface.GetFullscreenViewport(out _, out _, out int vw, out int vh);

		float screenWidth = vw;
		float screenHeight = vh;
		float x = screenWidth / 2;
		float y = screenHeight / 2;

		bool bBehindCamera = false;

		if (angleCrosshairOffset != vec3_angle) {
			QAngle angles;
			Vector3 screen = default;

			angles = curViewAngles + angleCrosshairOffset;
			MathLib.AngleVectors(in angles, out Vector3 forward, out _, out _);
			MathLib.VectorAdd(in curViewOrigin, in forward, out Vector3 point);
			// ScreenTransform(in point, out screen); todo

			x += 0.5f * screen.X * screenWidth + 0.5f;
			y += 0.5f * screen.Y * screenHeight + 0.5f;
		}

		px = x;
		py = y;
		behindCamera = bBehindCamera;
	}

	public override void Paint() {
		if (Crosshair == null)
			return;

		if (!IsCurrentViewAccessAllowed())
			return;

		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return;

		GetDrawPosition(out float x, out float y, out bool behindCamera, CrosshairOffsetAngle);

		if (behindCamera)
			return;

		float weaponScale = 1.0f;
		int textureW = Crosshair.Width();
		int textureH = Crosshair.Height();
		// BaseCombatWeapon? weapon = player.GetActiveWeapon();
		// weapon?.GetWeaponCrosshairScale(ref weaponScale); todo

		float playerScale = 1.0f;
		Color clr = ClrCrosshair;
		float width = weaponScale * playerScale * textureW;
		float height = weaponScale * playerScale * textureH;
		int iWidth = (int)(width + 0.5f);
		int iHeight = (int)(height + 0.5f);
		int iX = (int)(x + 0.5f);
		int iY = (int)(y + 0.5f);

		Crosshair.DrawSelfCropped(
			iX - (iWidth / 2), iY - (iHeight / 2),
			0, 0,
			textureW, textureH,
			iWidth, iHeight,
			clr);
	}

	public void SetCrosshair(HudTexture texture, Color clr) {
		Crosshair = texture;
		ClrCrosshair = clr;
	}

	public void ResetCrosshair() => SetCrosshair(DefaultCrosshair!, new(255, 255, 255, 255));
}
