using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Numerics;

using static Game.Client.FlashlightEffectGlobals;

namespace Game.Client;

static class FlashlightEffectGlobals
{
	public static ConVar r_newflashlight = new("r_newflashlight", "1", FCvar.Cheat, "");
	public static ConVar r_swingflashlight = new("r_swingflashlight", "1", FCvar.Cheat);
	public static ConVar r_flashlightlockposition = new("r_flashlightlockposition", "0", FCvar.Cheat);
	public static ConVar r_flashlightfov = new("r_flashlightfov", "45.0", FCvar.Cheat);
	public static ConVar r_flashlightoffsetx = new("r_flashlightoffsetx", "10.0", FCvar.Cheat);
	public static ConVar r_flashlightoffsety = new("r_flashlightoffsety", "-20.0", FCvar.Cheat);
	public static ConVar r_flashlightoffsetz = new("r_flashlightoffsetz", "24.0", FCvar.Cheat);
	public static ConVar r_flashlightnear = new("r_flashlightnear", "4.0", FCvar.Cheat);
	public static ConVar r_flashlightfar = new("r_flashlightfar", "750.0", FCvar.Cheat);
	public static ConVar r_flashlightconstant = new("r_flashlightconstant", "0.0", FCvar.Cheat);
	public static ConVar r_flashlightlinear = new("r_flashlightlinear", "100.0", FCvar.Cheat);
	public static ConVar r_flashlightquadratic = new("r_flashlightquadratic", "0.0", FCvar.Cheat);
	public static ConVar r_flashlightvisualizetrace = new("r_flashlightvisualizetrace", "0", FCvar.Cheat);
	public static ConVar r_flashlightambient = new("r_flashlightambient", "0.0", FCvar.Cheat);
	public static ConVar r_flashlightshadowatten = new("r_flashlightshadowatten", "0.35", FCvar.Cheat);
	public static ConVar r_flashlightladderdist = new("r_flashlightladderdist", "40.0", FCvar.Cheat);
	public static ConVar mat_slopescaledepthbias_shadowmap = new("mat_slopescaledepthbias_shadowmap", "16", FCvar.Cheat);
	public static ConVar mat_depthbias_shadowmap = new("mat_depthbias_shadowmap", "0.0005", FCvar.Cheat);
}

struct TraceFilterSkipPlayerAndViewModel : ITraceFilter
{
	public bool ShouldHitEntity(IHandleEntity serverEntity, Contents contentsMask) {
		C_BaseEntity? entity = (C_BaseEntity?)EntityFromEntityHandle(serverEntity);
		if (entity == null)
			return true;

		if (entity is C_BaseViewModel ||
			entity is C_BasePlayer ||
			entity.GetCollisionGroup() == CollisionGroup.Debris ||
			entity.GetCollisionGroup() == CollisionGroup.InteractiveDebris) {
			return false;
		}

		return true;
	}
}

class FlashlightEffect : IDisposable
{
	bool IsOn;
	int EntIndex;
	ClientShadowHandle_t FlashlightHandle;
	// dlight todo
	float DistMod;
	protected TextureReference FlashlightTexture = new();

	public FlashlightEffect(int entIndex) {
		FlashlightHandle = CLIENTSHADOW_INVALID_HANDLE;
		EntIndex = entIndex;

		IsOn = false;
		// PointLight = NULL;
		DistMod = 0;

		// if (g_pMaterialSystemHardwareConfig->SupportsBorderColor())
		FlashlightTexture.Init("effects/flashlight_border", MaterialDefines.TEXTURE_GROUP_OTHER, true);
		// else
		// 	m_FlashlightTexture.Init("effects/flashlight001", TEXTURE_GROUP_OTHER, true);
	}

	public void Dispose() {
		LightOff();
	}

	public virtual void UpdateLight(in Vector3 pos, in Vector3 dir, in Vector3 right, in Vector3 up, int distance) {
		if (!IsOn)
			return;

		if (r_newflashlight.GetBool())
			UpdateLightNew(in pos, in dir, in right, in up);
		else
			UpdateLightOld(in pos, in dir, distance);
	}

	public void TurnOn() {
		IsOn = true;
		DistMod = 1.0f;
	}

	public void TurnOff() {
		if (IsOn) {
			IsOn = false;
			LightOff();
		}
	}

	public bool GetIsOn() => IsOn;

	public ClientShadowHandle_t GetFlashlightHandle() => FlashlightHandle;

	public void SetFlashlightHandle(ClientShadowHandle_t handle) => FlashlightHandle = handle;

	protected void UpdateLightNew(in Vector3 pos, in Vector3 forward, in Vector3 right, in Vector3 up) {
		FlashlightState state = new();

		bool playerOnLadder = C_BasePlayer.GetLocalPlayer()!.GetMoveType() == MoveType.Ladder;

		const float epsilon = 0.1f;
		const float distCutoff = 128.0f;
		const float distDrag = 0.2f;

		TraceFilterSkipPlayerAndViewModel traceFilter = new();
		float offsetY = r_flashlightoffsety.GetFloat();

		if (r_swingflashlight.GetBool()) {
			Vector3 swingLight = pos + forward * -12.0f;
			if (swingLight.Z > pos.Z)
				offsetY += swingLight.Z - pos.Z;
		}

		Vector3 origin = pos + offsetY * up;

		if (!playerOnLadder) {
			Util.TraceHull(in pos, in origin, new Vector3(-4, -4, -4), new Vector3(4, 4, 4), Mask.Solid & ~(Mask)Contents.HitBox, ref traceFilter, out Trace originTrace);

			if (originTrace.DidHit())
				origin = pos;
		}
		else
			origin = pos;

		Mask mask = Mask.OpaqueAndNPCs;
		mask &= ~(Mask)Contents.HitBox;
		mask |= (Mask)Contents.Window;

		Vector3 target = pos + forward * r_flashlightfar.GetFloat();

		Vector3 dir = target - origin;
		Vector3 vRight = right;
		Vector3 vUp = up;
		MathLib.VectorNormalize(ref dir);
		MathLib.VectorNormalize(ref vRight);
		MathLib.VectorNormalize(ref vUp);

		vUp -= MathLib.DotProduct(dir, vUp) * dir;
		MathLib.VectorNormalize(ref vUp);
		vRight -= MathLib.DotProduct(dir, vRight) * dir;
		MathLib.VectorNormalize(ref vRight);
		vRight -= MathLib.DotProduct(vUp, vRight) * vUp;
		MathLib.VectorNormalize(ref vRight);

		AssertFloatEquals(MathLib.DotProduct(dir, vRight), 0.0f, 1e-3f);
		AssertFloatEquals(MathLib.DotProduct(dir, vUp), 0.0f, 1e-3f);
		AssertFloatEquals(MathLib.DotProduct(vRight, vUp), 0.0f, 1e-3f);

		Util.TraceHull(in origin, in target, new Vector3(-4, -4, -4), new Vector3(4, 4, 4), mask, ref traceFilter, out Trace directionTrace);

		if (r_flashlightvisualizetrace.GetBool() == true) {
			if (debugoverlay != null) {
				debugoverlay.AddBoxOverlay(in directionTrace.EndPos, new Vector3(-4, -4, -4), new Vector3(4, 4, 4), new QAngle(0, 0, 0), 0, 0, 255, 16, 0);
				debugoverlay.AddLineOverlay(in origin, in directionTrace.EndPos, 255, 0, 0, false, 0);
			}
		}

		float dist = (directionTrace.EndPos - origin).Length();
		if (dist < distCutoff) {
			float pullBackDist = playerOnLadder ? r_flashlightladderdist.GetFloat() : distCutoff - dist;
			DistMod = MathLib.Lerp(distDrag, DistMod, pullBackDist);

			if (!playerOnLadder) {
				Util.TraceHull(in origin, origin - dir * (pullBackDist - epsilon), new Vector3(-4, -4, -4), new Vector3(4, 4, 4), mask, ref traceFilter, out Trace backTrace);
				if (backTrace.DidHit()) {
					float maxDist = (backTrace.EndPos - origin).Length() - epsilon;
					if (DistMod > maxDist)
						DistMod = maxDist;
				}
			}
		}
		else
			DistMod = MathLib.Lerp(distDrag, DistMod, 0.0f);
		origin -= dir * DistMod;

		state.LightOrigin = origin;

		MathLib.BasisToQuaternion(in dir, in vRight, in vUp, out state.Orientation);

		state.QuadraticAtten = r_flashlightquadratic.GetFloat();

		const bool flicker = false;

		// HL2_EPISODIC todo

		if (flicker == false) {
			state.LinearAtten = r_flashlightlinear.GetFloat();
			state.HorizontalFOVDegrees = r_flashlightfov.GetFloat();
			state.VerticalFOVDegrees = r_flashlightfov.GetFloat();
		}

		state.ConstantAtten = r_flashlightconstant.GetFloat();
		state.Color[0] = 1.0f;
		state.Color[1] = 1.0f;
		state.Color[2] = 1.0f;
		state.Color[3] = r_flashlightambient.GetFloat();
		state.NearZ = r_flashlightnear.GetFloat() + DistMod;
		state.FarZ = r_flashlightfar.GetFloat();
		state.EnableShadows = r_flashlightdepthtexture.GetBool();
		state.ShadowMapResolution = r_flashlightdepthres.GetInt();

		state.SpotlightTexture = FlashlightTexture.Get();
		state.SpotlightTextureFrame = 0;

		state.ShadowAtten = r_flashlightshadowatten.GetFloat();
		state.ShadowSlopeScaleDepthBias = mat_slopescaledepthbias_shadowmap.GetFloat();
		state.ShadowDepthBias = mat_depthbias_shadowmap.GetFloat();

		if (FlashlightHandle == CLIENTSHADOW_INVALID_HANDLE)
			FlashlightHandle = g_ClientShadowMgr.CreateFlashlight(in state);
		else if (!r_flashlightlockposition.GetBool())
			g_ClientShadowMgr.UpdateFlashlightState(FlashlightHandle, in state);

		g_ClientShadowMgr.UpdateProjectedTexture(FlashlightHandle, true);

		LightOffOld();
	}

	protected void UpdateLightOld(in Vector3 pos, in Vector3 dir, int distance) {
		// dlight todo

		LightOffNew();
	}

	protected void LightOffNew() {
		if (FlashlightHandle != CLIENTSHADOW_INVALID_HANDLE) {
			g_ClientShadowMgr.DestroyFlashlight(FlashlightHandle);
			FlashlightHandle = CLIENTSHADOW_INVALID_HANDLE;
		}
	}

	protected void LightOffOld() {
		// dlight todo
	}

	protected void LightOff() {
		LightOffOld();
		LightOffNew();
	}
}

class HeadlightEffect : FlashlightEffect
{
	public HeadlightEffect() : base(0) { }

	public override void UpdateLight(in Vector3 pos, in Vector3 dir, in Vector3 right, in Vector3 up, int distance) {
		if (GetIsOn() == false)
			return;

		FlashlightState state = new();
		Vector3 basisX, basisY, basisZ;
		basisX = dir;
		basisY = right;
		basisZ = up;
		MathLib.VectorNormalize(ref basisX);
		MathLib.VectorNormalize(ref basisY);
		MathLib.VectorNormalize(ref basisZ);

		MathLib.BasisToQuaternion(in basisX, in basisY, in basisZ, out state.Orientation);

		state.LightOrigin = pos;

		state.HorizontalFOVDegrees = 45.0f;
		state.VerticalFOVDegrees = 30.0f;
		state.QuadraticAtten = r_flashlightquadratic.GetFloat();
		state.LinearAtten = r_flashlightlinear.GetFloat();
		state.ConstantAtten = r_flashlightconstant.GetFloat();
		state.Color[0] = 1.0f;
		state.Color[1] = 1.0f;
		state.Color[2] = 1.0f;
		state.Color[3] = r_flashlightambient.GetFloat();
		state.NearZ = r_flashlightnear.GetFloat();
		state.FarZ = r_flashlightfar.GetFloat();
		state.EnableShadows = true;
		state.SpotlightTexture = FlashlightTexture.Get();
		state.SpotlightTextureFrame = 0;

		if (GetFlashlightHandle() == CLIENTSHADOW_INVALID_HANDLE)
			SetFlashlightHandle(g_ClientShadowMgr.CreateFlashlight(in state));
		else
			g_ClientShadowMgr.UpdateFlashlightState(GetFlashlightHandle(), in state);

		g_ClientShadowMgr.UpdateProjectedTexture(GetFlashlightHandle(), true);
	}
}