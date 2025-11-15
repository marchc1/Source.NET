using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Mathematics;

using System.Numerics;
using System.Reflection;

using FIELD = Source.FIELD<Game.Client.C_BaseAnimating>;
using FIELD_ILR = Source.FIELD<Game.Client.C_InfoLightingRelative>;

namespace Game.Client;

public partial class C_InfoLightingRelative : C_BaseEntity
{
	public static readonly RecvTable DT_InfoLightingRelative = new(DT_BaseEntity, [
		RecvPropEHandle(FIELD_ILR.OF(nameof(LightingLandmark))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("InfoLightingRelative", DT_InfoLightingRelative).WithManualClassID(StaticClassIndices.CInfoLightingRelative);

	public readonly EHANDLE LightingLandmark = new();
}


public partial class C_BaseAnimating : C_BaseEntity, IModelLoadCallback
{
	static readonly ConVar r_drawothermodels = new("1", FCvar.Cheat, "0=Off, 1=Normal, 2=Wireframe" );


	public static readonly RecvTable DT_ServerAnimationData = new([
		RecvPropFloat(FIELD.OF(nameof(Cycle))),
	]);
	public static readonly ClientClass CC_ServerAnimationData = new ClientClass("ServerAnimationData", null, null, DT_ServerAnimationData);
	public static readonly RecvTable DT_BaseAnimating = new(DT_BaseEntity, [
		RecvPropInt( FIELD.OF(nameof(ForceBone))),
		RecvPropVector( FIELD.OF(nameof(Force))),

		RecvPropInt( FIELD.OF(nameof(Skin))),
		RecvPropInt( FIELD.OF(nameof(Body))),

		RecvPropInt( FIELD.OF(nameof(HitboxSet))),

		RecvPropFloat( FIELD.OF(nameof(ModelScale))),

		RecvPropArray3( FIELD.OF_ARRAY(nameof(PoseParameter)), RecvPropFloat(null!)),

		RecvPropInt( FIELD.OF(nameof(Sequence))),
		RecvPropFloat( FIELD.OF(nameof(PlaybackRate))),

		RecvPropArray3(FIELD.OF_ARRAY(nameof(EncodedController)), RecvPropFloat(null!) ),

		RecvPropInt( FIELD.OF(nameof( ClientSideAnimation ))),
		RecvPropInt( FIELD.OF(nameof( ClientSideFrameReset ))),

		RecvPropInt( FIELD.OF(nameof( NewSequenceParity) )),
		RecvPropInt( FIELD.OF(nameof( ResetEventsParity ))),
		RecvPropInt( FIELD.OF(nameof( MuzzleFlashParity ))),

		RecvPropEHandle( FIELD.OF(nameof( LightingOrigin )) ),
		RecvPropEHandle( FIELD.OF(nameof( LightingOriginRelative )) ),

		RecvPropDataTable( "serveranimdata", DT_ServerAnimationData ),

		RecvPropFloat( FIELD.OF(nameof(FadeMinDist) )),
		RecvPropFloat( FIELD.OF(nameof(FadeMaxDist ))),
		RecvPropFloat( FIELD.OF(nameof(FadeScale ))),

		// Gmod specific
		RecvPropEHandle(FIELD.OF(nameof(BoneManipulator))),
		RecvPropEHandle(FIELD.OF(nameof(FlexManipulator))),
		RecvPropVector(FIELD.OF(nameof(OverrideViewTarget))),
	]);

	private static void RecvProxy_Sequence(ref readonly RecvProxyData data, object instance, FieldInfo field) {
		throw new NotImplementedException();
	}

	public static readonly new ClientClass ClientClass = new ClientClass("BaseAnimating", null, null, DT_BaseAnimating).WithManualClassID(StaticClassIndices.CBaseAnimating);

	public void OnModelLoadComplete(Model model) {
		throw new NotImplementedException();
	}
	public override void PostDataUpdate(DataUpdateType updateType) {
		base.PostDataUpdate(updateType);
	}

	internal static void UpdateClientSideAnimations() {

	}

	public int InternalDrawModel(StudioFlags flags) {
		throw new NotImplementedException();
	}

	public C_BaseAnimating? FindFollowedEntity() {
		C_BaseEntity? follow = GetFollowedEntity();

		if (follow == null)
			return null;

		if (follow.IsDormant())
			return null;

		if (follow.GetModel() == null) {
			Warning("ModelType.Studio: MoveType.Follow with no model.\n");
			return null;
		}

		if (modelinfo.GetModelType(follow.GetModel()) != ModelType.Studio) {
			Warning($"Attached {modelinfo.GetModelName(GetModel())} (ModelType.Studio) to {modelinfo.GetModelName(follow.GetModel())} ({modelinfo.GetModelType(follow.GetModel())})\n");
			return null;
		}

		return (C_BaseAnimating?)follow;
	}

	public override int DrawModel(StudioFlags flags) {
		if (!ReadyToDraw)
			return 0;

		int drawn = 0;

		if (r_drawothermodels.GetInt() != 0) {
			StudioFlags extraFlags = 0;
			if (r_drawothermodels.GetInt() == 2) 
				extraFlags |= StudioFlags.Wireframe;

			if ((flags & StudioFlags.ShadowDepthTexture) != 0)
				extraFlags |= StudioFlags.ShadowDepthTexture;

			if ((flags & StudioFlags.SSAODepthTexture) != 0)
				extraFlags |= StudioFlags.SSAODepthTexture;

			// todo: g_pStudioStatsEntity
			if ((flags & (StudioFlags.SSAODepthTexture | StudioFlags.ShadowDepthTexture)) == 0 && false)
				extraFlags |= StudioFlags.GenerateStats;

			if ((flags & StudioFlags.NoOverrideForAttach) != 0) 
				extraFlags |= StudioFlags.NoOverrideForAttach;

			// Necessary for lighting blending
			CreateModelInstance();

			if (!IsFollowingEntity()) {
				drawn = InternalDrawModel(flags | extraFlags);
			}
			else {
				// this doesn't draw unless master entity is visible and it's a studio model!!!
				C_BaseAnimating? follow = FindFollowedEntity();
				if (follow != null) {
					// recompute master entity bone structure
					int baseDrawn = follow.DrawModel(0);

					// draw entity
					// FIXME: Currently only draws if aiment is drawn.  
					// BUGBUG: Fixup bbox and do a separate cull for follow object
					if (baseDrawn != 0) 
						drawn = InternalDrawModel(StudioFlags.Render | extraFlags);
				}
			}
		}

		DrawBBoxVisualizations();

		return drawn;
	}

	void DisableMuzzleFlash() {

	}

	int PrevNewSequenceParity;
	int PrevResetEventsParity;

	public int GetSequence() => Sequence;
	protected override void UpdateVisibility() {
		base.UpdateVisibility();

		// todo
	}
	public virtual bool IsViewModel() => false;
	public override void NotifyShouldTransmit(ShouldTransmiteState state) {
		base.NotifyShouldTransmit(state);

		if(state == ShouldTransmiteState.Start) {
			DisableMuzzleFlash();

			PrevResetEventsParity = ResetEventsParity;
			EventSequence = GetSequence();
		}
	}
	public override void GetAimEntOrigin(C_BaseEntity attachedTo, out Vector3 origin, out QAngle angles) {
		C_BaseEntity? moveParent = null;
		if(IsEffectActive(EntityEffects.BoneMerge) && IsEffectActive(EntityEffects.BoneMergeFastCull) && (moveParent = GetMoveParent()) != null) {
			origin = moveParent.GetAbsOrigin(); //TODO:  moveParent.WorldSpaceCenter();
			angles = moveParent.GetRenderAngles();
		}
		else {
			// TODO: Bone merge cache
			base.GetAimEntOrigin(attachedTo, out origin, out angles);
		}
	}

	public int Sequence;
	public int ForceBone;
	public Vector3 Force;
	public int Skin;
	public int Body;
	public int HitboxSet;
	public float ModelScale;
	public float PlaybackRate;
	public bool ClientSideAnimation;
	public bool ClientSideFrameReset;
	public int NewSequenceParity;
	public int ResetEventsParity;
	public byte MuzzleFlashParity;
	public readonly EHANDLE LightingOrigin = new();
	public readonly EHANDLE LightingOriginRelative = new();
	public readonly EHANDLE BoneManipulator = new();
	public readonly EHANDLE FlexManipulator = new();
	public Vector3 OverrideViewTarget;
	public float FadeMinDist;
	public float FadeMaxDist;
	public float FadeScale;
	public float Cycle;

	public InlineArrayMaxStudioPoseParam<float> PoseParameter;
	public InlineArrayMaxStudioPoseParam<float> OldPoseParameters;
	public float PrevEventCycle;
	public int EventSequence;
	public InlineArrayMaxStudioBoneCtrls<float> EncodedController;
	public InlineArrayMaxStudioBoneCtrls<float> OldEncodedController;
}
