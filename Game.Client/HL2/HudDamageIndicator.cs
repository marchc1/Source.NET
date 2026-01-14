using Game.Client.HUD;
using Game.Shared;

using Source;
using Source.Common.Bitbuffers;
using Source.Common.GUI;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.GUI.Controls;

using System.Numerics;

[DeclareHudElement(Name = "CHudDamageIndicator")]
class HudDamageIndicator : EditableHudElement, IHudElement
{
	[PanelAnimationVar("dmg_xpos", "10", "proportional_float")] protected float DmgX;
	[PanelAnimationVar("dmg_ypos", "80", "proportional_float")] protected float DmgY;
	[PanelAnimationVar("dmg_wide", "30", "proportional_float")] protected float DmgWide;
	[PanelAnimationVar("dmg_tall1", "300", "proportional_float")] protected float DmgTall1;
	[PanelAnimationVar("dmg_tall2", "240", "proportional_float")] protected float DmgTall2;
	[PanelAnimationVar("DmgColorLeft", "255 0 0 0")] protected Color DmgColorLeft;
	[PanelAnimationVar("DmgColorRight", "255 0 0 0")] protected Color DmgColorRight;
	[PanelAnimationVar("DmgHighColorLeft", "255 0 0 0")] protected Color DmgHighColorLeft;
	[PanelAnimationVar("DmgHighColorRight", "255 0 0 0")] protected Color DmgHighColorRight;
	[PanelAnimationVar("DmgFullscreenColor", "255 0 0 0")] protected Color DmgFullscreenColor;

	MaterialReference WhiteAdditiveMaterial = new();

	const int DAMAGE_ANY = 0;
	const int DAMAGE_LOW = 1;
	const int DAMAGE_HIGH = 2;

	const float ANGLE_ANY = -1.0f;

	struct DamageAnimation(string? name, DamageType bitsDamage, float angleMinimum, float angleMaximum, int damage)
	{
		public string? Name = name;
		public DamageType BitsDamage = bitsDamage;
		public float AngleMinimum = angleMinimum;
		public float AngleMaximum = angleMaximum;
		public int Damage = damage;
	}

	static readonly DamageAnimation[] DamageAnimations = [
		new("HudTakeDamageDrown",      DamageType.Drown,      ANGLE_ANY, ANGLE_ANY, DAMAGE_ANY),
		new("HudTakeDamagePoison",     DamageType.Poison,     ANGLE_ANY, ANGLE_ANY, DAMAGE_ANY),
		new("HudTakeDamageBurn",       DamageType.Burn,       ANGLE_ANY, ANGLE_ANY, DAMAGE_ANY),
		new("HudTakeDamageRadiation",  DamageType.Radiation,  ANGLE_ANY, ANGLE_ANY, DAMAGE_ANY),
		new("HudTakeDamageRadiation",  DamageType.Acid,       ANGLE_ANY, ANGLE_ANY, DAMAGE_ANY),

		new("HudTakeDamageHighLeft",   DamageType.Any,        45.0f,    135.0f,    DAMAGE_HIGH),
		new("HudTakeDamageHighRight",  DamageType.Any,        225.0f,   315.0f,    DAMAGE_HIGH),
		new("HudTakeDamageHigh",       DamageType.Any,        ANGLE_ANY, ANGLE_ANY, DAMAGE_HIGH),

		new("HudTakeDamageLeft",       DamageType.Any,        45.0f,    135.0f,    DAMAGE_ANY),
		new("HudTakeDamageRight",      DamageType.Any,        225.0f,   315.0f,    DAMAGE_ANY),
		new("HudTakeDamageBehind",     DamageType.Any,        135.0f,   225.0f,    DAMAGE_ANY),

		new("HudTakeDamageFront",      DamageType.Any,        ANGLE_ANY, ANGLE_ANY, DAMAGE_ANY),

		new(null, 0, 0, 0, 0)
	];

	public HudDamageIndicator(string? panelName) : base(null, "HudDamageIndicator") {
		Panel parent = clientMode.GetViewport();
		SetParent(parent);

		WhiteAdditiveMaterial.Init("vgui/white_additive", "VGUI Materials");

		((IHudElement)this).SetHiddenBits(HideHudBits.Health);
	}

	public void Reset() {
		DmgColorLeft[3] = 0;
		DmgColorRight[3] = 0;
		DmgHighColorLeft[3] = 0;
		DmgHighColorRight[3] = 0;
		DmgFullscreenColor[3] = 0;
	}

	public override void Init() => IHudElement.HookMessage("Damage", MsgFunc_Damage);

	public bool ShouldDraw() {
		bool needsDraw = (DmgColorLeft[3] != 0) || (DmgColorRight[3] != 0) || (DmgHighColorLeft[3] != 0) || (DmgHighColorRight[3] != 0) || (DmgFullscreenColor[3] != 0);
		return needsDraw && ((IHudElement)this).ShouldDraw();
	}

	private void DrawDamageIndicator(int side) {
		using MatRenderContextPtr renderCtx = new(materials);
		IMesh mesh = renderCtx.GetDynamicMesh(true, null, null, WhiteAdditiveMaterial.Get());

		MeshBuilder meshBuilder = new();
		meshBuilder.Begin(mesh, MaterialPrimitiveType.Quads, 1);

		int insetY = (int)(DmgTall1 - DmgTall2) / 2;
		int x1 = (int)DmgX;
		int x2 = (int)(DmgX + DmgWide);
		int[] y = [
			(int)DmgY,
			(int)(DmgY + insetY),
			(int)(DmgY + DmgTall1 - insetY),
			(int)(DmgY + DmgTall1)
		];
		int[] alpha = [0, 1, 1, 0];

		bool highDamage = false;
		if (DmgHighColorRight[3] > DmgColorRight[3] || DmgHighColorLeft[3] > DmgColorLeft[3]) {
			x1 = (int)(GetWide() * 0.0f);
			x2 = (int)(GetWide() * 0.5f);
			y[0] = 0;
			y[1] = 0;
			y[2] = GetTall();
			y[3] = GetTall();
			alpha[0] = 1;
			alpha[1] = 0;
			alpha[2] = 0;
			alpha[3] = 1;
			highDamage = true;
		}

		byte r, g, b, a;
		if (side == 1) {
			if (highDamage) {
				r = DmgHighColorRight[0];
				g = DmgHighColorRight[1];
				b = DmgHighColorRight[2];
				a = DmgHighColorRight[3];
			}
			else {
				r = DmgColorRight[0];
				g = DmgColorRight[1];
				b = DmgColorRight[2];
				a = DmgColorRight[3];
			}

			x1 = GetWide() - x1;
			x2 = GetWide() - x2;

			meshBuilder.Color4ub(r, g, b, (byte)(a * alpha[0]));
			meshBuilder.TexCoord2f(0, 0, 0);
			meshBuilder.Position3f(x1, y[0], 0);
			meshBuilder.AdvanceVertex();

			meshBuilder.Color4ub(r, g, b, (byte)(a * alpha[3]));
			meshBuilder.TexCoord2f(0, 0, 1);
			meshBuilder.Position3f(x1, y[3], 0);
			meshBuilder.AdvanceVertex();

			meshBuilder.Color4ub(r, g, b, (byte)(a * alpha[2]));
			meshBuilder.TexCoord2f(0, 1, 1);
			meshBuilder.Position3f(x2, y[2], 0);
			meshBuilder.AdvanceVertex();

			meshBuilder.Color4ub(r, g, b, (byte)(a * alpha[1]));
			meshBuilder.TexCoord2f(0, 1, 0);
			meshBuilder.Position3f(x2, y[1], 0);
			meshBuilder.AdvanceVertex();
		}
		else {
			if (highDamage) {
				r = DmgHighColorLeft[0];
				g = DmgHighColorLeft[1];
				b = DmgHighColorLeft[2];
				a = DmgHighColorLeft[3];
			}
			else {
				r = DmgColorLeft[0];
				g = DmgColorLeft[1];
				b = DmgColorLeft[2];
				a = DmgColorLeft[3];
			}

			meshBuilder.Color4ub(r, g, b, (byte)(a * alpha[0]));
			meshBuilder.TexCoord2f(0, 0, 0);
			meshBuilder.Position3f(x1, y[0], 0);
			meshBuilder.AdvanceVertex();

			meshBuilder.Color4ub(r, g, b, (byte)(a * alpha[1]));
			meshBuilder.TexCoord2f(0, 1, 0);
			meshBuilder.Position3f(x2, y[1], 0);
			meshBuilder.AdvanceVertex();

			meshBuilder.Color4ub(r, g, b, (byte)(a * alpha[2]));
			meshBuilder.TexCoord2f(0, 1, 1);
			meshBuilder.Position3f(x2, y[2], 0);
			meshBuilder.AdvanceVertex();

			meshBuilder.Color4ub(r, g, b, (byte)(a * alpha[3]));
			meshBuilder.TexCoord2f(0, 0, 1);
			meshBuilder.Position3f(x1, y[3], 0);
			meshBuilder.AdvanceVertex();
		}

		meshBuilder.End();
		mesh.Draw();
	}

	private void DrawFullscreenDamageIndicator() {
		using MatRenderContextPtr renderCtx = new(materials);
		IMesh mesh = renderCtx.GetDynamicMesh(true, null, null, WhiteAdditiveMaterial.Get());

		MeshBuilder meshBuilder = new();
		meshBuilder.Begin(mesh, MaterialPrimitiveType.Quads, 1);

		byte r = DmgFullscreenColor[0], g = DmgFullscreenColor[1], b = DmgFullscreenColor[2], a = DmgFullscreenColor[3];

		float wide = GetWide();
		float tall = GetTall();

		meshBuilder.Color4ub(r, g, b, a);
		meshBuilder.TexCoord2f(0, 0, 0);
		meshBuilder.Position3f(0.0f, 0.0f, 0);
		meshBuilder.AdvanceVertex();

		meshBuilder.Color4ub(r, g, b, a);
		meshBuilder.TexCoord2f(0, 1, 0);
		meshBuilder.Position3f(wide, 0.0f, 0);
		meshBuilder.AdvanceVertex();

		meshBuilder.Color4ub(r, g, b, a);
		meshBuilder.TexCoord2f(0, 1, 1);
		meshBuilder.Position3f(wide, tall, 0);
		meshBuilder.AdvanceVertex();

		meshBuilder.Color4ub(r, g, b, a);
		meshBuilder.TexCoord2f(0, 0, 1);
		meshBuilder.Position3f(0.0f, tall, 0);
		meshBuilder.AdvanceVertex();

		meshBuilder.End();
		mesh.Draw();
	}

	public override void Paint() {
		DrawFullscreenDamageIndicator();
		DrawDamageIndicator(0);
		DrawDamageIndicator(1);
	}

	private void MsgFunc_Damage(bf_read msg) {
		int armor = msg.ReadByte();
		int damageTaken = msg.ReadByte();
		DamageType bitsDamage = (DamageType)msg.ReadLong();

		Vector3 vecFrom;

		vecFrom.X = msg.ReadFloat();
		vecFrom.Y = msg.ReadFloat();
		vecFrom.Z = msg.ReadFloat();

		BasePlayer? player = BasePlayer.GetLocalPlayer();
		if (player == null)
			return;

		if (player.GetHealth() <= 0) {
			clientMode.GetViewportAnimationController()!.StartAnimationSequence("HudPlayerDeath");
			return;
		}

		if (vecFrom == vec3_origin && ((bitsDamage & DamageType.Drown) == 0))
			return;

		Vector3 vecDelta = vecFrom - MainViewOrigin();
		MathLib.VectorNormalize(ref vecDelta);

		int highDamage = DAMAGE_LOW;
		if (damageTaken > 25)
			highDamage = DAMAGE_HIGH;

		if (!player.IsSuitEquipped())
			highDamage = DAMAGE_HIGH;

		if (damageTaken > 0 || armor > 0) {
			GetDamagePosition(vecDelta, out float angle);

			for (int i = 0; DamageAnimations[i].Name != null; i++) {
				DamageAnimation anim = DamageAnimations[i];

				if (anim.BitsDamage != 0 && (bitsDamage & anim.BitsDamage) == 0)
					continue;

				if (anim.AngleMinimum != 0 && angle < anim.AngleMinimum)
					continue;

				if (anim.AngleMaximum != 0 && angle > anim.AngleMaximum)
					continue;

				if (anim.Damage != 0 && anim.Damage != highDamage)
					continue;

				clientMode.GetViewportAnimationController()!.StartAnimationSequence(anim.Name);
				break;
			}
		}
	}

	private void GetDamagePosition(Vector3 vecDelta, out float rotation) {
		float radius = 360.0f;

		Vector3 playerPostion = MainViewOrigin();
		QAngle playerAngles = MainViewAngles();

		Vector3 forward, right;
		Vector3 up = new(0, 0, 1);
		MathLib.AngleVectors(playerAngles, out forward, out _, out _);
		forward.Z = 0;
		MathLib.VectorNormalize(ref forward);
		MathLib.CrossProduct(up, forward, out right);
		float front = MathLib.DotProduct(vecDelta, forward);
		float side = MathLib.DotProduct(vecDelta, right);
		float xpos = radius * -side;
		float ypos = radius * -front;

		rotation = MathF.Atan2(xpos, ypos) + MathF.PI;
		rotation *= 180.0f / MathF.PI;

		// float yawRadians = -rotation * (MathF.PI / 180.0f);
		// float ca = MathF.Cos(yawRadians);
		// float sa = MathF.Sin(yawRadians);

		// xpos = (int)((GetWide() / 2) + (radius * sa));
		// ypos = (int)((GetTall() / 2) - (radius * ca));
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SetPaintBackgroundEnabled(false);

		surface.GetFullscreenViewport(out _, out _, out int wide, out int tall);

		SetForceStereoRenderToFrameBuffer(true);

		SetSize(wide, tall);
	}
}
