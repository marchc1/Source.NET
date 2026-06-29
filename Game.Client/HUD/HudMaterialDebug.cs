#if DEBUG
using Game.Shared;

using Source;
using Source.Common.Commands;
using Source.Common.Formats.BSP;
using Source.Common.Engine;
using Source.Common.GUI;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Engine;
using Source.GUI.Controls;

using System.Numerics;

namespace Game.Client.HUD;

[DeclareHudElement(Name = "CHudMaterialDebug")]
public class HudMaterialDebug : Panel, IHudElement
{
	public string? ElementName { get; set; }
	public HideHudBits HiddenBits { get; set; }
	public bool Active { get; set; }
	public bool NeedsRemove { get; set; }
	public bool IsParentedToClientDLLRootPanel { get; set; }
	public List<int> HudRenderGroups { get; set; } = [];

	static readonly ConVar sdn_matdebug = new("sdn_matdebug", "0", FCvar.ClientDLL, "Show material debug info for what's under the crosshair. 1 = world surfaces, 2 = also models/entities");

	public HudMaterialDebug(string elementName) : base(null, "HudMaterialDebug") {
		((IHudElement)this).Ctor(elementName);
		SetParent(clientMode.GetViewport());
		SetProportional(false);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SetProportional(false);
		SetPaintBackgroundEnabled(false);
		surface.GetScreenSize(out int w, out int h);
		SetSize(w, h);
	}

	public bool ShouldDraw() => sdn_matdebug.GetBool() && IHudElement.DefaultShouldDraw(this);

	public void Init() { }
	public void VidInit() { }

	public override void Paint() {
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();

		MathLib.AngleVectors(player!.EyeAngles(), out Vector3 forward);

		Vector3 start = player.EyePosition();
		Vector3 end = start + forward * 8192.0f;

		Util.TraceLine(start, end, Mask.Shot, player, CollisionGroup.None, out Trace tr);

		if (!tr.DidHit())
			return;

		int lineH = surface.GetFontTall(FontSmall) + 2;
		int x = 20;
		int y = 20;

		Span<char> buf = stackalloc char[512];

		surface.DrawSetTextFont(FontSmall);

		void DrawLine(ReadOnlySpan<char> text, int r = 255, int g = 255, int b = 255) {
			surface.DrawSetTextColor(new Color(r, g, b, 255));
			surface.DrawSetTextPos(x, y);
			surface.DrawPrintText(text);
			surface.DrawSetTextColor(0, 0, 0, 150);
			surface.DrawSetTextPos(x + 2, y + 2);
			surface.DrawPrintText(text);
			y += lineH;
		}

		void DrawMaterial(IMaterial mat, scoped Span<char> buf) {
			DrawLine(sprintf(buf, "Material: %s").S(mat.GetName()).ToSpan(), 180, 255, 180);
			DrawLine(sprintf(buf, "Shader: %s").S(mat.GetShaderName() ?? "unknown").ToSpan(), 180, 255, 180);
			DrawLine(sprintf(buf, "Translucent: %s").S(mat.IsTranslucent() ? "yes" : "no").ToSpan());
			IMaterialVar[]? shaderParams = mat.GetShaderParams();
			if (shaderParams != null) {
				DrawLine("");
				DrawLine("Shader Params", 180, 180, 255);
				foreach (IMaterialVar param in shaderParams) {
					if (param == null || !param.IsDefined()) continue;
					ReadOnlySpan<char> name = param.GetName();
					switch (param.GetVarType()) {
						case MaterialVarType.Int:
							DrawLine(sprintf(buf, "  %s = %i").S(name).I(param.GetIntValue()).ToSpan());
							break;
						case MaterialVarType.Float: {
								Span<char> fStr = stackalloc char[16];
								param.GetFloatValue().TryFormat(fStr, out int fLen, "F4");
								DrawLine(sprintf(buf, "  %s = %s").S(name).S(fStr[..fLen]).ToSpan());
								break;
							}
						case MaterialVarType.Vector: {
								int n = param.VectorSize();
								Span<float> v = stackalloc float[4];
								param.GetVecValue(v);
								Span<char> v0s = stackalloc char[12]; v[0].TryFormat(v0s, out int l0, "F3");
								Span<char> v1s = stackalloc char[12]; v[1].TryFormat(v1s, out int l1, "F3");
								Span<char> v2s = stackalloc char[12]; v[2].TryFormat(v2s, out int l2, "F3");
								Span<char> v3s = stackalloc char[12]; v[3].TryFormat(v3s, out int l3, "F3");
								if (n <= 1)
									DrawLine(sprintf(buf, "  %s = [%s]").S(name).S(v0s[..l0]).ToSpan());
								else if (n == 2)
									DrawLine(sprintf(buf, "  %s = [%s %s]").S(name).S(v0s[..l0]).S(v1s[..l1]).ToSpan());
								else if (n == 3)
									DrawLine(sprintf(buf, "  %s = [%s %s %s]").S(name).S(v0s[..l0]).S(v1s[..l1]).S(v2s[..l2]).ToSpan());
								else
									DrawLine(sprintf(buf, "  %s = [%s %s %s %s]").S(name).S(v0s[..l0]).S(v1s[..l1]).S(v2s[..l2]).S(v3s[..l3]).ToSpan());
								break;
							}
						case MaterialVarType.String:
							DrawLine(sprintf(buf, "  %s = \"%s\"").S(name).S(param.GetStringValue()).ToSpan());
							break;
						case MaterialVarType.Texture: {
								ITexture? tex = param.GetTextureValue();
								DrawLine(sprintf(buf, "  %s = [tex: %s]").S(name).S(tex != null ? tex.GetName() : "null").ToSpan());
								break;
							}
						default:
							DrawLine(sprintf(buf, "  %s = <%s>").S(name).S(param.GetVarType().ToString()).ToSpan());
							break;
					}
				}
			}
		}

		C_BaseEntity? ent = tr.Ent;
		Model? model = ent?.GetModel();

		bool isStaticProp = tr.EntHandle != null && StaticPropMgrGlobals.g_StaticPropMgr.IsStaticProp(tr.EntHandle);
		if (model == null && isStaticProp)
			model = StaticPropMgrGlobals.g_StaticPropMgr.GetStaticProp(tr.EntHandle)?.GetCollisionModel();

		if (sdn_matdebug.GetInt() >= 2 && (tr.DidHitNonWorldEntity() || isStaticProp) && model != null) {
			if (isStaticProp)
				DrawLine(sprintf(buf, "Static prop: %s").S(modelinfo.GetModelName(model)).ToSpan(), 255, 220, 80);
			else {
				DrawLine(sprintf(buf, "Entity: %s (#%i)").S(ent!.GetClassname()).I(ent.EntIndex()).ToSpan(), 255, 220, 80);
				DrawLine(sprintf(buf, "Model: %s").S(modelinfo.GetModelName(model)).ToSpan(), 255, 220, 80);
			}
			DrawLine(sprintf(buf, "Contents: %i").I((int)tr.Contents).ToSpan());

			int matCount = modelinfo.GetModelMaterialCount(model);
			if (matCount <= 0) {
				DrawLine("Material: none", 255, 100, 100);
				return;
			}

			IMaterial[] mats = new IMaterial[matCount];
			modelinfo.GetModelMaterials(model, mats);
			for (int i = 0; i < matCount; i++) {
				if (mats[i] == null) continue;
				DrawLine("");
				DrawMaterial(mats[i], buf);
			}
			return;
		}

		DrawLine(sprintf(buf, "Surface: %s").S(tr.Surface.Name).ToSpan(), 255, 220, 80);
		DrawLine(sprintf(buf, "SurfaceProps: %i").I(tr.Surface.SurfaceProps).ToSpan());
		DrawLine(sprintf(buf, "Contents: %i").I((int)tr.Contents).ToSpan());

		IMaterial? mat = materials.FindMaterial(tr.Surface.Name, "World textures", false);
		if (mat != null)
			DrawMaterial(mat, buf);
		else
			DrawLine("Material: not found", 255, 100, 100);
	}

	[PanelAnimationVar("ItemFont", "DefaultSmall")] public IFont? FontSmall;
}
#endif