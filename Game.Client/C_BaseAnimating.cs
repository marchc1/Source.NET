using CommunityToolkit.HighPerformance;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Mathematics;

using System.Net.NetworkInformation;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

using ClientModelRenderInfo = Source.Common.Engine.ModelRenderInfo;
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
	static readonly ConVar r_drawothermodels = new("1", FCvar.Cheat, "0=Off, 1=Normal, 2=Wireframe");
	static readonly HashSet<C_BaseAnimating> PreviousBoneSetups = [];


	public TimeUnit_t GetPlaybackRate() => PlaybackRate;
	public void SetPlaybackRate(TimeUnit_t rate) => PlaybackRate = (float)rate; // todo: double?
	public ref readonly Matrix3x4 GetBone(int bone) => ref BoneAccessor.GetBone(bone);
	public ref Matrix3x4 GetBoneForWrite(int bone) => ref BoneAccessor.GetBoneForWrite(bone);

	public bool IsBoneAccessAllowed() => true; // todo

	static long ModelBoneCounter;
	long MostRecentModelBoneCounter;
	long MostRecentBoneSetupRequest;
	int PrevBoneMask;
	int AccumulatedBoneMask;
	readonly BoneAccessor BoneAccessor = new();
	// Note that Handle is not a pointer in this case so we need to initialize
	// each member in the bone attachments (when we get to this part).
	readonly List<Handle<C_BaseAnimating>> BoneAttachments = [];
	int BoneIndexAttached;
	Vector3 BonePosition;
	QAngle BoneAngles;
	readonly Handle<C_BaseAnimating> AttachedTo = new();
	readonly List<Matrix3x4> CachedBoneData = [];

	public void InvalidateBoneCache() {
		MostRecentModelBoneCounter = ModelBoneCounter - 1;
		LastBoneSetupTime = -TimeUnit_t.MaxValue;
	}
	public bool IsBoneCacheValid() => MostRecentModelBoneCounter == ModelBoneCounter;
	static void InvalidateBoneCaches() => ModelBoneCounter++;

	public TimeUnit_t LastBoneSetupTime;
	public virtual TimeUnit_t LastBoneChangedTime() => -TimeUnit_t.MaxValue;

	static TimeUnit_t SetupBones__lastWarning = 0.0;
	// TODO: REWRITE THIS FOR BONEMERGING
	public void BuildTransformations(StudioHdr? hdr, Span<Vector3> pos, Span<Quaternion> q, in Matrix3x4 cameraTransform, int boneMask, ref BoneBitList boneComputed) {
		if (hdr == null)
			return;

		Matrix3x4 bonematrix = default;
		Span<bool> boneSimulated = stackalloc bool[Studio.MAXSTUDIOBONES];

		// no bones have been simulated
		MStudioBone pbones = hdr.Bone(0);

		// For EF_BONEMERGE entities, copy the bone matrices for any bones that have matching names.
		bool boneMerge = IsEffectActive(EntityEffects.BoneMerge);

		for (int i = 0; i < hdr.NumBones(); i++) {
			// Only update bones reference by the bone mask.
			if ((hdr.BoneFlags(i) & boneMask) == 0)
				continue;

			// animate all non-simulated bones
			if (boneSimulated[i])
				continue;

			// skip bones that the IK has already setup
			else if (boneComputed.IsBoneMarked(i)) {
				// dummy operation, just used to verify in debug that this should have happened
				GetBoneForWrite(i);
			}
			else {
				MathLib.QuaternionMatrix(in q[i], in pos[i], out bonematrix);

				Assert(MathF.Abs(pos[i].X) < 100000);
				Assert(MathF.Abs(pos[i].Y) < 100000);
				Assert(MathF.Abs(pos[i].Z) < 100000);

				if (hdr.BoneParent(i) == -1)
					MathLib.ConcatTransforms(cameraTransform, bonematrix, out GetBoneForWrite(i));
				else
					MathLib.ConcatTransforms(GetBone(hdr.BoneParent(i)), bonematrix, out GetBoneForWrite(i));
			}

			if (hdr.BoneParent(i) == -1)
				// Apply client-side effects to the transformation matrix
				ApplyBoneMatrixTransform(ref GetBoneForWrite(i));
		}
	}

	protected virtual void ApplyBoneMatrixTransform(ref Matrix3x4 matrix4x4) {

	}

	public override bool SetupBones(Span<Matrix3x4> boneToWorldOut, int maxBones, int boneMask, double currentTime) {
		if (!boneToWorldOut.IsEmpty && !IsBoneAccessAllowed()) {
			if (gpGlobals.RealTime >= SetupBones__lastWarning + 1.0f) {
				DevMsg($"*** ERROR: Bone access not allowed (entity {Index}:{GetClassname()})\n");
				SetupBones__lastWarning = gpGlobals.RealTime;
			}
		}

		if (MostRecentModelBoneCounter != ModelBoneCounter) {
			if (LastBoneChangedTime() >= LastBoneSetupTime) {
				BoneAccessor.SetReadableBones(0);
				BoneAccessor.SetWritableBones(0);
				LastBoneSetupTime = currentTime;
			}
			PrevBoneMask = AccumulatedBoneMask;
			AccumulatedBoneMask = 0;
		}

		int nBoneCount = CachedBoneData.Count;

		AccumulatedBoneMask |= boneMask;

		MostRecentModelBoneCounter = ModelBoneCounter;

		if ((BoneAccessor.GetReadableBones() & boneMask) != boneMask) { 
			StudioHdr? hdr = GetModelPtr();
			if (hdr == null || !hdr.SequencesAvailable())
				return false;

			Matrix3x4 parentTransform = default;
			MathLib.AngleMatrix(GetRenderAngles(), GetRenderOrigin(), ref parentTransform);

			boneMask |= PrevBoneMask;

			int oldReadableBones = BoneAccessor.GetReadableBones();
			BoneAccessor.SetWritableBones(BoneAccessor.GetReadableBones() | boneMask);
			BoneAccessor.SetReadableBones(BoneAccessor.GetWritableBones());

			if ((hdr.Flags() & StudioHdrFlags.StaticProp) != 0)
				GetBoneForWrite(0) = parentTransform;
			else {
				CdllExts.TrackBoneSetupEnt(this);

				AddFlag((int)EFL.SettingUpBones);

				Span<Vector3> pos = stackalloc Vector3[Studio.MAXSTUDIOBONES];
				Span<Quaternion> q = stackalloc Quaternion[Studio.MAXSTUDIOBONES];
				int bonesMaskNeedRecalc = boneMask | oldReadableBones;

				StandardBlendingRules(hdr, pos, q, currentTime, bonesMaskNeedRecalc);

				BoneBitList boneComputed = new();
				BuildTransformations(hdr, pos, q, parentTransform, bonesMaskNeedRecalc, ref boneComputed);

				RemoveFlag((int)EFL.SettingUpBones);
			}

			if ((oldReadableBones & Studio.BONE_USED_BY_ATTACHMENT) == 0 && (boneMask & Studio.BONE_USED_BY_ATTACHMENT) != 0) {
				// if (!SetupBones_AttachmentHelper(hdr)) {
				// 	DevWarning(2, "SetupBones: SetupBones_AttachmentHelper failed.\n");
				// 	return false;
				// }
			}
		}

		if (!boneToWorldOut.IsEmpty) {
			if (maxBones >= CachedBoneData.Count) {
				memcpy(boneToWorldOut, CachedBoneData.AsSpan());
			}
			else {
				Warning($"SetupBones: invalid bone array size ({maxBones} - needs {CachedBoneData.Count})\n");
				return false;
			}
		}

		return true;
	}
	public TimeUnit_t GetCycle() => Cycle;
	private void StandardBlendingRules(StudioHdr hdr, Span<Vector3> pos, Span<Quaternion> q, TimeUnit_t currentTime, int boneMask) {
		Span<float> poseparam = stackalloc float[Studio.MAXSTUDIOPOSEPARAM];

		TimeUnit_t cycle = GetCycle();

		BoneSetup setup = new(hdr, boneMask, poseparam);
		setup.InitPose(pos, q);	
		setup.AccumulatePose(pos, q, GetSequence(), cycle, 1.0f, currentTime, null);	
	}

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
	protected override StudioHdr? OnNewModel() {
		InvalidateMdlCache();

		// remove transition animations playback
		// SequenceTransitioner.RemoveAll();

		// TODO: Jiggle bones
		// TODO: Dynamic model pending
		// TODO: AutoRefModelIndex

		if (GetModel() == null || modelinfo.GetModelType(GetModel()) != ModelType.Studio)
			return null;

		// TODO: Dynamic model loading

		StudioHdr? hdr = GetModelPtr();
		if (hdr == null)
			return null;

		InvalidateBoneCache();

		if(CachedBoneData.Count != hdr.NumBones()) {
			CachedBoneData.SetSize(hdr.NumBones());
			for (int i = 0; i < hdr.NumBones(); i++) {
				MathLib.SetIdentityMatrix(out CachedBoneData.AsSpan()[i]);
			}
		}
		BoneAccessor.Init(CachedBoneData.Base());

		// todo: the rest of this

		return hdr;
	}
	public C_BaseAnimating() {
		pStudioHdr = null;
		hStudioHdr = MDLHANDLE_INVALID;
	}

	public void OnModelLoadComplete(Model model) {
		OnNewModel();
		UpdateVisibility();
	}
	public override void PostDataUpdate(DataUpdateType updateType) {
		base.PostDataUpdate(updateType);
	}

	internal static void UpdateClientSideAnimations() {

	}

	StudioHdr? pStudioHdr;
	MDLHandle_t hStudioHdr;

	public void LockStudioHdr() {
		Assert(hStudioHdr == MDLHANDLE_INVALID && pStudioHdr == null);

		if (hStudioHdr != MDLHANDLE_INVALID || pStudioHdr != null) {
			Assert(pStudioHdr != null ? pStudioHdr.GetRenderHdr() == mdlcache.GetStudioHdr(hStudioHdr) : hStudioHdr == MDLHANDLE_INVALID);
			return;
		}

		Model? mdl = GetModel();
		if (mdl == null)
			return;

		hStudioHdr = modelinfo.GetCacheHandle(mdl);
		if (hStudioHdr == MDLHANDLE_INVALID)
			return;

		StudioHeader studioHdr = mdlcache.LockStudioHdr(hStudioHdr);
		if (studioHdr == null) {
			hStudioHdr = MDLHANDLE_INVALID;
			return;
		}

		StudioHdr newWrapper = new StudioHdr();
		newWrapper.Init(studioHdr, mdlcache);
		Assert(newWrapper.IsValid());

		if (newWrapper.GetVirtualModel() != null) {
			MDLHandle_t hVirtualModel = studioHdr.VirtualModel;
			mdlcache.LockStudioHdr(hVirtualModel);
		}

		pStudioHdr = newWrapper; // must be last to ensure virtual model correctly set up
	}

	public void UnlockStudioHdr() {
		if (hStudioHdr != MDLHANDLE_INVALID) {
			StudioHeader? studioHdr = mdlcache.GetStudioHdr(hStudioHdr);
			Assert(studioHdr != null && pStudioHdr!.GetRenderHdr() == studioHdr);

			{
				// Immediate-mode rendering, can unlock immediately
				if (studioHdr.GetVirtualModel() != null) {
					MDLHandle_t hVirtualModel = studioHdr.VirtualModel;
					mdlcache.UnlockStudioHdr(hVirtualModel);
				}
				mdlcache.UnlockStudioHdr(hStudioHdr);
			}
			hStudioHdr = MDLHANDLE_INVALID;

			pStudioHdr = null;
		}
	}

	public StudioHdr? GetModelPtr() {
		if (pStudioHdr == null)
			LockStudioHdr();
		return pStudioHdr;
	}

	public void InvalidateMdlCache() {
		UnlockStudioHdr();
	}

	public bool IsModelScaleFractional() => ModelScale < 1.0f;
	public bool IsModelScaled() => ModelScale > 1.0f + float.Epsilon || ModelScale < 1.0f - float.Epsilon;
	public float GetModelScale() => ModelScale;

	public int GetBody() => Body;
	public int GetSkin() => Skin;

	public int InternalDrawModel(StudioFlags flags) {
		var model = GetModel();
		if (model == null)
			return 0;

		// This should never happen, but if the server class hierarchy has bmodel entities derived from CBaseAnimating or does a
		//  SetModel with the wrong type of model, this could occur.
		if (modelinfo.GetModelType(model) != ModelType.Studio) {
			return base.DrawModel(flags);
		}

		// Make sure hdr is valid for drawing
		if (GetModelPtr() == null)
			return 0;

		UpdateBoneAttachments();

		if (IsEffectActive(EntityEffects.ItemBlink))
			flags |= StudioFlags.ItemBlink;

		ClientModelRenderInfo info = default;

		info.Flags = flags;
		info.Renderable = this;
		info.Instance = GetModelInstance();
		info.EntityIndex = Index;
		info.Model = GetModel();
		info.Origin = GetRenderOrigin();
		info.Angles = GetRenderAngles();
		info.Skin = GetSkin();
		info.Body = GetBody();
		info.HitboxSet = HitboxSet;

		if (!OnInternalDrawModel(ref info))
			return 0;

		// Turns the origin + angles into a matrix
		MathLib.AngleMatrix(info.Angles, info.Origin, ref info.ModelToWorld);

		DrawModelState state = default;
		bool markAsDrawn = modelrender.DrawModelSetup(ref info, ref state, default, out Span<Matrix3x4> boneToWorld);

		// Scale the base transform if we don't have a bone hierarchy
		if (IsModelScaled()) {
			StudioHdr? pHdr = GetModelPtr();
			if (pHdr != null && !Unsafe.IsNullRef(ref boneToWorld) && pHdr.NumBones() == 1) {
				// Scale the bone to world at this point
				float flScale = GetModelScale();
				// todo: vector scale
			}
		}

		if (markAsDrawn && (info.Flags & StudioFlags.Render) != 0)
			DoInternalDrawModel(ref info, ref state, boneToWorld);
		else
			DoInternalDrawModel(ref info, ref Unsafe.NullRef<DrawModelState>(), boneToWorld);

		OnPostInternalDrawModel(ref info);

		return markAsDrawn ? 1 : 0;
	}

	private void DoInternalDrawModel(ref ClientModelRenderInfo info, ref DrawModelState state, Span<Matrix3x4> boneToWorldArray) {
		if (!Unsafe.IsNullRef(ref state))
			modelrender.DrawModelExecute(ref state, ref info, boneToWorldArray);

		// vcollide_wireframe todo
	}

	protected virtual bool OnPostInternalDrawModel(ref ClientModelRenderInfo info) {
		return true;
	}

	protected virtual bool OnInternalDrawModel(ref ClientModelRenderInfo info) {
		var lor = LightingOriginRelative.Get();
		var lo = LightingOrigin.Get();
		if (lor != null) {
			// todo
		}

		if (lo != null) {
			info.LightingOrigin = lo.GetAbsOrigin();
		}

		return true;
	}

	private void UpdateBoneAttachments() {

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
	public override void ValidateModelIndex() {
		base.ValidateModelIndex();
		Assert(ModelIndex == 0);
	}
	public virtual bool IsViewModel() => false;
	public override void NotifyShouldTransmit(ShouldTransmiteState state) {
		base.NotifyShouldTransmit(state);

		if (state == ShouldTransmiteState.Start) {
			DisableMuzzleFlash();

			PrevResetEventsParity = ResetEventsParity;
			EventSequence = GetSequence();
		}
	}
	public override void GetAimEntOrigin(C_BaseEntity attachedTo, out Vector3 origin, out QAngle angles) {
		C_BaseEntity? moveParent = null;
		if (IsEffectActive(EntityEffects.BoneMerge) && IsEffectActive(EntityEffects.BoneMergeFastCull) && (moveParent = GetMoveParent()) != null) {
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
