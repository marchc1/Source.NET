using Game.Client.HUD;
using Game.Shared;

using Source;
using Source.Common.GUI;
using Source.Common.MaterialSystem;
using Source.GUI.Controls;

namespace Game.Client.HL2;

[DeclareHudElement(Name = "CHudZoom")]
public class HudZoom : HudNumericDisplay, IHudElement
{
	const float ZOOM_FADE_TIME = 0.4f;

	bool ZoomOn;
	TimeUnit_t ZoomStartTime;
	bool Painted;
	[PanelAnimationVar("Circle1Radius", "66", "proportional_float")] protected float Circle1Radius;
	[PanelAnimationVar("Circle2Radius", "72", "proportional_float")] protected float Circle2Radius;
	[PanelAnimationVar("DashGap", "16", "proportional_float")] protected float DashGap;
	[PanelAnimationVar("DashHeight", "4", "proportional_float")] protected float DashHeight;
	MaterialReference ZoomMaterial = new();

	struct Coord
	{
		public float x, y;
		public float u, v;
	}

	public HudZoom(string? panelName) : base(null, "HudZoom") {
		var parent = clientMode.GetViewport();
		SetParent(parent);

		((IHudElement)this).SetHiddenBits(HideHudBits.Health | HideHudBits.PlayerDead | HideHudBits.NeedSuit);
	}

	public void Init() {
		ZoomOn = false;
		Painted = false;
		ZoomStartTime = -999.0f;
		ZoomMaterial.Init("vgui/zoom", "VGUI textures");
	}

	public void LevelInit() => Init();

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
		SetFgColor(scheme.GetColor("ZoomReticleColor", GetFgColor()));

		// SetForceStereoRenderToFrameBuffer(true);

		surface.GetFullscreenViewport(out _, out _, out int wide, out int tall);
		SetBounds(0, 0, wide, tall);
	}

	public bool ShouldDraw() {
		bool needsDraw = false;

		C_BaseHLPlayer? player = (C_BaseHLPlayer?)BasePlayer.GetLocalPlayer();
		if (player == null)
			return false;

		if (player.HL2Local.Zooming)
			needsDraw = true;
		else if (Painted)
			needsDraw = true;

		return needsDraw && ((IHudElement)this).ShouldDraw();
	}

	public override void Paint() {
		Painted = false;

		C_BaseHLPlayer? player = (C_BaseHLPlayer?)BasePlayer.GetLocalPlayer();
		if (player == null)
			return;

		if (player.HL2Local.Zooming && !ZoomOn) {
			ZoomOn = true;
			ZoomStartTime = gpGlobals.CurTime;
		}
		else if (!player.HL2Local.Zooming && ZoomOn) {
			ZoomOn = false;
			ZoomStartTime = gpGlobals.CurTime;
		}

		TimeUnit_t deltaTime = gpGlobals.CurTime - ZoomStartTime;
		float scale = (int)Math.Clamp(deltaTime / ZOOM_FADE_TIME, 0.0f, 1.0f);
		float alpha;

		if (ZoomOn)
			alpha = (float)scale;
		else {
			if (scale >= 1.0f)
				return;

			alpha = (1.0f - scale) * 0.25f;
			scale = 1.0f - (scale * 0.5f);
		}

		Color col = GetFgColor();
		col[3] = (byte)(alpha * 64);

		surface.DrawSetColor(col);

		HudCrosshair.GetDrawPosition(out float x, out float y, out bool behindCrosshair);
		if (behindCrosshair)
			return;
		int xCrosshair = (int)x;
		int yCrosshair = (int)y;
		GetSize(out int wide, out int tall);

		surface.DrawOutlinedCircle(xCrosshair, yCrosshair, (int)(Circle1Radius * scale), 48);
		surface.DrawOutlinedCircle(xCrosshair, yCrosshair, (int)(Circle2Radius * scale), 64);

		int dashCount = 2;
		int ypos = (int)(yCrosshair - (DashHeight / 2.0f));
		float gap = DashGap * Math.Max(scale, 0.1f);
		int dashMax = (int)(Math.Max(x, wide - x) / gap);
		while (dashCount < dashMax) {
			int xpos = (int)(x - gap * dashCount + 0.5f);
			surface.DrawFilledRect(xpos, ypos, xpos + 1, ypos + (int)DashHeight);
			xpos = (int)(x + gap * dashCount + 0.5f);
			surface.DrawFilledRect(xpos, ypos, xpos + 1, ypos + (int)DashHeight);
			dashCount++;
		}

		using MatRenderContextPtr renderContext = new(materials);
		renderContext.Bind(ZoomMaterial as IMaterial);
		IMesh mesh = renderContext.GetDynamicMesh(true, null, null, null);

		float x0 = 0.0f, x1 = x, x2 = wide;
		float y0 = 0.0f, y1 = y, y2 = tall;
		float uv1 = 1.0f - (1.0f / 255.0f);
		float uv2 = 1.0f + (1.0f / 255.0f);

		Coord[] coords = [
			// top-left
			new () { x = x0, y = y0, u = uv1, v = uv2 },
			new () { x = x1, y = y0, u = uv2, v = uv2 },
			new () { x = x1, y = y1, u = uv2, v = uv1 },
			new () { x = x0, y = y1, u = uv1, v = uv1 },

			// top-right
			new () { x = x1, y = y0, u = uv2, v = uv2 },
			new () { x = x2, y = y0, u = uv1, v = uv2 },
			new () { x = x2, y = y1, u = uv1, v = uv1 },
			new () { x = x1, y = y1, u = uv2, v = uv1 },

			// bottom-right
			new() { x = x1, y = y1, u = uv2, v = uv1 },
			new () { x = x2, y = y1, u = uv1, v = uv1 },
			new () { x = x2, y = y2, u = uv1, v = uv2 },
			new () { x = x1, y = y2, u = uv2, v = uv2 },

			// bottom-left
			new () { x = x0, y = y1, u = uv1, v = uv1 },
			new () { x = x1, y = y1, u = uv2, v = uv1 },
			new () { x = x1, y = y2, u = uv2, v = uv2 },
			new () { x = x0, y = y2, u = uv1, v = uv2 },
		];

		MeshBuilder meshBuilder = new();
		meshBuilder.Begin(mesh, MaterialPrimitiveType.Quads, 4);

		foreach (var coord in coords) {
			meshBuilder.Color4f(0.0f, 0.0f, 0.0f, alpha);
			meshBuilder.TexCoord2f(0, coord.u, coord.v);
			meshBuilder.Position3f(coord.x, coord.y, 0.0f);
			meshBuilder.AdvanceVertex();
		}

		meshBuilder.End();
		mesh.Draw();

		Painted = true;
	}
}
