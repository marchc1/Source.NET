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
	// They did some REALLY weird stuff for access tags...
	public record struct BoneAccessTag
	{
		public nint Data;
		public string? Str;

		public static explicit operator BoneAccessTag(nint i) => new() { Data = i };
		public static explicit operator BoneAccessTag(string str) => new() { Str = str };
		public static implicit operator bool(BoneAccessTag tag) => tag.Data != 0 || tag.Str != null;
		public static implicit operator ReadOnlySpan<char>(BoneAccessTag tag) => tag.Str;
	}
	static readonly ConVar r_drawothermodels = new("1", FCvar.Cheat, "0=Off, 1=Normal, 2=Wireframe");
	static readonly HashSet<C_BaseAnimating> PreviousBoneSetups = [];

	public static void PushAllowBoneAccess(bool allowForNormalModels, bool allowForViewModels, BoneAccessTag tag) {
		lock (BoneAccessMutex) {
			BoneAccess save = BoneAccessBase;
			BoneAccessStack.Push(save);

			BoneAccessBase.AllowBoneAccessForNormalModels = allowForNormalModels;
			BoneAccessBase.AllowBoneAccessForViewModels = allowForViewModels;
			BoneAccessBase.Tag = tag;
		}
	}

	public static void PopAllowBoneAccess(BoneAccessTag tag) {
		lock (BoneAccessMutex) {
			Assert(BoneAccessBase.Tag == tag || (BoneAccessBase.Tag && BoneAccessBase.Tag != (BoneAccessTag)1 && tag && tag != (BoneAccessTag)1 && strcmp(BoneAccessBase.Tag, tag) == 0));
			int lastIndex = BoneAccessStack.Count - 1;
			if (lastIndex < 0) {
				AssertMsg(false, "C_BaseAnimating.PopBoneAccess:  Stack is empty!!!");
				return;
			}
			BoneAccessStack.Pop();
			if (lastIndex != 0)
				BoneAccessBase = BoneAccessStack.Peek();
			else
				BoneAccessBase = default;
		}
	}

	public struct BoneAccess
	{
		public bool AllowBoneAccessForNormalModels;
		public bool AllowBoneAccessForViewModels;
		public BoneAccessTag Tag;
	}

	static readonly object BoneAccessMutex = new();
	static readonly Stack<BoneAccess> BoneAccessStack = [];
	static BoneAccess BoneAccessBase;

	public ref struct AutoAllowBoneAccess : IDisposable
	{
		public AutoAllowBoneAccess(bool allowForNormalModes, bool allowForViewModels) {
			C_BaseAnimating.PushAllowBoneAccess(allowForNormalModes, allowForViewModels, (BoneAccessTag)1);
		}
		public void Dispose() {
			C_BaseAnimating.PopAllowBoneAccess((BoneAccessTag)1);
		}
	}

	public bool IsBoneAccessAllowed() => IsViewModel() ? BoneAccessBase.AllowBoneAccessForViewModels : BoneAccessBase.AllowBoneAccessForNormalModels;


	public TimeUnit_t GetPlaybackRate() => PlaybackRate;
	public void SetPlaybackRate(TimeUnit_t rate) => PlaybackRate = (float)rate; // todo: double?
	public ref readonly Matrix3x4 GetBone(int bone) => ref BoneAccessor.GetBone(bone);
	public ref Matrix3x4 GetBoneForWrite(int bone) => ref BoneAccessor.GetBoneForWrite(bone);


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
	public static void InvalidateBoneCaches() => ModelBoneCounter++;

	public TimeUnit_t LastBoneSetupTime;
	public virtual TimeUnit_t LastBoneChangedTime() => TimeUnit_t.MaxValue;

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


	static readonly DynamicAccessor DA_PoseParameter = FIELD.OF_ARRAY(nameof(PoseParameter));
	static readonly DynamicAccessor DA_Cycle = FIELD.OF(nameof(Cycle));

	public void AddBaseAnimatingInterpolatedVars() {
		AddVar(DA_PoseParameter, iv_flPoseParameter, LatchFlags.LatchAnimationVar, true);

		LatchFlags flags = LatchFlags.LatchAnimationVar;
		if (ClientSideAnimation)
			flags |= LatchFlags.ExcludeAutoInterpolate;

		AddVar(DA_Cycle, iv_Cycle, flags, true);
	}

	public void RemoveBaseAnimatingInterpolatedVars() {
		RemoveVar(DA_PoseParameter, false);
		if (!GetPredictable())
			RemoveVar(DA_Cycle, false);
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

		if ((BoneAccessor.GetReadableBones() & boneMask) != boneMask) {  // TRUE IS A HACK! Why is this not happening???
			StudioHdr? hdr = GetModelPtr();
			if (hdr == null || !hdr.SequencesAvailable())
				return false;

			Matrix3x4 parentTransform = default;
			MathLib.AngleMatrix(GetRenderAngles(), GetRenderOrigin(), ref parentTransform);
			// MathLib.AngleMatrix(new(19.56f, -145.89f, 0), new(-767, 143.9f, -12650), ref parentTransform);

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
				memset(pos.Cast<Vector3, float>(), float.NaN);
				memset(q.Cast<Quaternion, float>(), float.NaN);

				int bonesMaskNeedRecalc = boneMask | oldReadableBones;

				StandardBlendingRules(hdr, pos, q, currentTime, bonesMaskNeedRecalc);

				BoneBitList boneComputed = new();
				BuildTransformations(hdr, pos, q, parentTransform, bonesMaskNeedRecalc, ref boneComputed);

				RemoveFlag((int)EFL.SettingUpBones);
			}

			if ((oldReadableBones & Studio.BONE_USED_BY_ATTACHMENT) == 0 && (boneMask & Studio.BONE_USED_BY_ATTACHMENT) != 0) {
				if (!SetupBones_AttachmentHelper(hdr)) {
					DevWarning(2, "SetupBones: SetupBones_AttachmentHelper failed.\n");
					return false;
				}
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

	private bool SetupBones_AttachmentHelper(StudioHdr hdr) {
		if (hdr == null)
			return false;

		// calculate attachment points
		Matrix3x4 world;
		for (int i = 0; i < hdr.GetNumAttachments(); i++) {
			MStudioAttachment attachment = hdr.Attachment(i);
			int iBone = hdr.GetAttachmentBone(i);
			if ((attachment.Flags & Studio.ATTACHMENT_FLAG_WORLD_ALIGN) == 0)
				MathLib.ConcatTransforms(GetBone(iBone), attachment.Local, out world);
			else {
				Vector3 vecLocalBonePos, vecWorldBonePos;
				MathLib.MatrixGetColumn(attachment.Local, 3, out vecLocalBonePos);
				MathLib.VectorTransform(in vecLocalBonePos, in GetBone(iBone), out vecWorldBonePos);

				MathLib.SetIdentityMatrix(out world);
				MathLib.MatrixSetColumn(in vecWorldBonePos, 3, ref world);
			}

			PutAttachment(i + 1, world);
		}

		return true;
	}
	public override BaseAnimating? GetBaseAnimating() => this;
	public bool PutAttachment(int number, in Matrix3x4 attachmentToWorld) {
		if (number < 1 || number > Attachments.Count)
			return false;

		AttachmentData pAtt = Attachments[number - 1];
		if (gpGlobals.FrameTime > 0 && pAtt.LastFramecount > 0 && pAtt.LastFramecount == gpGlobals.FrameCount - 1) {
			Vector3 vecPreviousOrigin, vecOrigin;
			MathLib.MatrixPosition(pAtt.AttachmentToWorld, out vecPreviousOrigin);
			MathLib.MatrixPosition(attachmentToWorld, out vecOrigin);
			pAtt.OriginVelocity = (vecOrigin - vecPreviousOrigin) / (float)gpGlobals.FrameTime;
		}
		else {
			pAtt.OriginVelocity.Init();
		}
		pAtt.LastFramecount = gpGlobals.FrameCount;
		pAtt.AnglesComputed = false;
		pAtt.AttachmentToWorld = attachmentToWorld;


		return true;
	}

	public TimeUnit_t GetCycle() => Cycle;
	public void SetSequence(int sequence) {
		if (Sequence != sequence) {
			Sequence = sequence;
			InvalidatePhysicsRecursive(InvalidatePhysicsBits.AnimationChanged);
			if (ClientSideAnimation)
				ClientSideAnimationChanged();
		}
	}

	private void ClientSideAnimationChanged() {
		if (!ClientSideAnimation)
			return;

		// todo
	}

	public void SetCycle(TimeUnit_t cycle) {
		if (cycle != Cycle) {
			Cycle = cycle;
			InvalidatePhysicsRecursive(InvalidatePhysicsBits.AnimationChanged);
		}
	}
	private void StandardBlendingRules(StudioHdr hdr, Span<Vector3> pos, Span<Quaternion> q, TimeUnit_t currentTime, int boneMask) {
		Span<float> poseparam = stackalloc float[Studio.MAXSTUDIOPOSEPARAM];
		GetPoseParameters(hdr, poseparam);
		TimeUnit_t cycle = GetCycle();

		BoneSetup setup = new(hdr, boneMask, poseparam);
		setup.InitPose(pos, q);
		setup.AccumulatePose(pos, q, GetSequence(), cycle, 1.0f, currentTime, null);
		MaintainSequenceTransitions(ref setup, cycle, pos, q);
		AccumulateLayers(ref setup, pos, q, currentTime);
		setup.CalcAutoplaySequences(pos, q, currentTime, null);
	}

	private void GetPoseParameters(StudioHdr? hdr, Span<float> poseparam) {
		if (hdr == null)
			return;
		int i;
		for (i = 0; i < Studio.MAXSTUDIOPOSEPARAM; i++)
			poseparam[i] = PoseParameter[i];

		/*if(GetSequenceName(GetSequence()) == "idle" && hdr.Name() == "weapons/c_physcannon.mdl") {
			Msg($"model   : {hdr.Name()}\n");
			Msg($"curtime : {gpGlobals.CurTime} :\n");
			for (i = 0; i < hdr.GetNumPoseParameters(); i++) {
				MStudioPoseParamDesc Pose = hdr.PoseParameter(i);
				Msg($"   poseparam idx#{i} {Pose.Name()} = {poseparam[i] * Pose.End + (1 - poseparam[i]) * Pose.Start}\n");
			}
			Msg("\n");
		}*/
	}

	public virtual void AccumulateLayers(ref BoneSetup setup, Span<Vector3> pos, Span<Quaternion> q, double currentTime) {

	}

	readonly SequenceTransitioner SequenceTransitioner = new();

	private void MaintainSequenceTransitions(ref BoneSetup boneSetup, double cycle, Span<Vector3> pos, Span<Quaternion> q) {
		if (boneSetup.GetStudioHdr() == null)
			return;

		if (prediction.InPrediction()) {
			PrevNewSequenceParity = NewSequenceParity;
			return;
		}

		SequenceTransitioner.CheckForSequenceChange(
			boneSetup.GetStudioHdr(),
			GetSequence(),
			NewSequenceParity != PrevNewSequenceParity,
			!IsNoInterpolationFrame()
			);

		PrevNewSequenceParity = NewSequenceParity;

		// Update the transition sequence list.
		SequenceTransitioner.UpdateCurrent(
			boneSetup.GetStudioHdr(),
			GetSequence(),
			cycle,
			PlaybackRate,
			gpGlobals.CurTime
			);


		for (int i = SequenceTransitioner.AnimationQueue.Count - 2; i >= 0; i--) {
			C_AnimationLayer blend = SequenceTransitioner.AnimationQueue[i];

			double dt = (gpGlobals.CurTime - blend.LayerAnimtime);
			cycle = blend.Cycle + dt * blend.PlaybackRate * GetSequenceCycleRate(boneSetup.GetStudioHdr(), blend.Sequence);
			cycle = ClampCycle(cycle, IsSequenceLooping(boneSetup.GetStudioHdr(), blend.Sequence));

			boneSetup.AccumulatePose(pos, q, blend.Sequence, cycle, blend.Weight, gpGlobals.CurTime, null);
		}
	}

	private double ClampCycle(double cycle, bool isLooping) {
		if (isLooping) {
			cycle -= (int)cycle;
			if (cycle < 0.0) {
				cycle += 1.0;
			}
		}
		else {
			cycle = Math.Clamp(cycle, 0.0, 0.999);
		}
		return cycle;
	}

	public TimeUnit_t SequenceDuration() => SequenceDuration(GetSequence());
	public TimeUnit_t SequenceDuration(int sequence) => SequenceDuration(GetModelPtr(), sequence);
	public TimeUnit_t SequenceDuration(StudioHdr? studioHdr, int sequence) {
		if (studioHdr == null)
			return 0.1f;

		if (!studioHdr.SequencesAvailable())
			return 0.1;

		if (sequence >= studioHdr.GetNumSeq() || sequence < 0) {
			DevWarning(2, $"C_BaseAnimating::SequenceDuration({sequence}) out of range\n");
			return 0.1;
		}

		return BoneSetup.Studio_Duration(studioHdr, sequence, PoseParameter);
	}
	public TimeUnit_t GetSequenceCycleRate(StudioHdr? studioHdr, int sequence) {
		TimeUnit_t t = SequenceDuration(studioHdr, sequence);
		if (t != 0.0)
			return 1.0 / t;
		return t;
	}
	bool PredictionEligible;
	public string GetSequenceName(int sequence) {
		if (sequence == -1)
			return "Not Found!";

		if (GetModelPtr() == null)
			return "No model!";

		return Animation.GetSequenceName(GetModelPtr(), sequence);
	}

	public void SetPredictionEligible(bool canpredict) => PredictionEligible = canpredict;
	public bool IsSequenceLooping(int sequence) => IsSequenceLooping(GetModelPtr(), sequence);
	public bool IsSequenceLooping(StudioHdr? studioHdr, int sequence) {
		return (Animation.GetSequenceFlags(studioHdr, sequence) & StudioAnimSeqFlags.Looping) != 0;
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

		RecvPropInt( FIELD.OF(nameof(Sequence)), 0, RecvProxy_Sequence),
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

	private static void RecvProxy_Sequence(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		RecvProxy_Int32ToInt32(in data, instance, field);
		C_BaseAnimating? animating = (C_BaseAnimating)instance;
		if (animating == null)
			return;
		animating.SetReceivedSequence();
		animating.UpdateVisibility();
	}

	public static readonly new ClientClass ClientClass = new ClientClass("BaseAnimating", null, null, DT_BaseAnimating).WithManualClassID(StaticClassIndices.CBaseAnimating);
	protected override StudioHdr? OnNewModel() {
		InvalidateMdlCache();
		int i;

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

		if (CachedBoneData.Count != hdr.NumBones()) {
			CachedBoneData.SetSize(hdr.NumBones());
			for (i = 0; i < hdr.NumBones(); i++)
				MathLib.SetIdentityMatrix(out CachedBoneData.AsSpan()[i]);
		}
		BoneAccessor.Init(CachedBoneData.Base());

		if (Attachments.Count != hdr.GetNumAttachments())
			Attachments.SetSizeInitialized(hdr.GetNumAttachments());

		for (int j = 0; j < Attachments.Count; j++)
			Attachments[j].Reset();

		Assert(hdr.GetNumPoseParameters() <= PoseParameter.Length);
		iv_flPoseParameter.SetMaxCount(hdr.GetNumPoseParameters());

		for (i = 0; i < hdr.GetNumPoseParameters(); i++) {
			MStudioPoseParamDesc Pose = hdr.PoseParameter(i);
			iv_flPoseParameter.SetLooping(Pose.Loop != 0.0f, i);
		}

		if (ShouldInterpolate())
			AddToInterpolationList();

		int forceSequence = ShouldResetSequenceOnNewModel() ? 0 : Sequence;
		if (GetSequence() >= hdr.GetNumSeq())
			forceSequence = 0;

		Sequence = -1;
		SetSequence(forceSequence);
		if (ResetSequenceInfoOnLoad) {
			ResetSequenceInfoOnLoad = false;
			ResetSequenceInfo();
		}
		UpdateRelevantInterpolatedVars();

		// todo: the rest of this

		return hdr;
	}
	bool ResetSequenceInfoOnLoad = false;

	public void ResetSequenceInfo() {

	}

	public bool ReceivedSequence;
	public void SetReceivedSequence() => ReceivedSequence = true;
	public bool ShouldResetSequenceOnNewModel() => ReceivedSequence == false;
	private void UpdateRelevantInterpolatedVars() {
		if (!IsMarkedForDeletion() && !GetPredictable() && !IsClientCreated() && GetModelPtr() != null && GetModelPtr()!.SequencesAvailable())
			AddBaseAnimatingInterpolatedVars();
		else
			RemoveBaseAnimatingInterpolatedVars();
	}

	public override bool Interpolate(TimeUnit_t currentTime) {
		Vector3 oldOrigin = default;
		QAngle oldAngles = default;
		Vector3 oldVel = default;
		TimeUnit_t flOldCycle = GetCycle();
		InvalidatePhysicsBits nChangeFlags = 0;

		if (!ClientSideAnimation)
			iv_Cycle.SetLooping(IsSequenceLooping(GetSequence()));

		int noMoreChanges = 0;
		InterpolateResult retVal = BaseInterpolatePart1(ref currentTime, ref oldOrigin, ref oldAngles, ref oldVel, ref noMoreChanges);
		if (retVal == InterpolateResult.Stop) {
			if (noMoreChanges != 0)
				RemoveFromInterpolationList();
			return true;
		}

		// Did cycle change?
		if (GetCycle() != flOldCycle)
			nChangeFlags |= InvalidatePhysicsBits.AnimationChanged;

		if (noMoreChanges != 0)
			RemoveFromInterpolationList();

		BaseInterpolatePart2(oldOrigin, oldAngles, oldVel, nChangeFlags);
		return true;
	}
	public C_BaseAnimating() {
		iv_Cycle = new($"{nameof(C_BaseAnimating)}.{nameof(iv_Cycle)}");
		iv_flPoseParameter = new(Studio.MAXSTUDIOPOSEPARAM, $"{nameof(C_BaseAnimating)}.{nameof(iv_flPoseParameter)}");

		pStudioHdr = null;
		hStudioHdr = MDLHANDLE_INVALID;

		AddBaseAnimatingInterpolatedVars();
	}

	bool DynamicModelPending;

	public void OnModelLoadComplete(Model model) {
		if (DynamicModelPending && model == GetModel()) {
			DynamicModelPending = false;
			OnNewModel();
			UpdateVisibility();
		}
	}
	TimeUnit_t OldCycle;
	int OldSequence;
	float OldModelScale;

	public override void PreDataUpdate(DataUpdateType updateType) {
		OldCycle = GetCycle();
		OldSequence = GetSequence();
		OldModelScale = GetModelScale();

		int i;
		for (i = 0; i < Studio.MAXSTUDIOBONECTRLS; i++)
			OldEncodedController[i] = EncodedController[i];

		for (i = 0; i < Studio.MAXSTUDIOPOSEPARAM; i++)
			OldPoseParameters[i] = PoseParameter[i];

		base.PreDataUpdate(updateType);
	}

	public void AddToClientSideAnimationList() { /* todo */}
	public void RemoveFromClientSideAnimationList() { /* todo */}

	public override void PostDataUpdate(DataUpdateType updateType) {
		base.PostDataUpdate(updateType);

		if (ClientSideAnimation) {
			SetCycle(OldCycle);
			AddToClientSideAnimationList();
		}
		else
			RemoveFromClientSideAnimationList();

		bool bBoneControllersChanged = false;

		int i;
		for (i = 0; i < Studio.MAXSTUDIOBONECTRLS && !bBoneControllersChanged; i++)
			if (OldEncodedController[i] != EncodedController[i])
				bBoneControllersChanged = true;

		bool bPoseParametersChanged = false;

		for (i = 0; i < Studio.MAXSTUDIOPOSEPARAM && !bPoseParametersChanged; i++)
			if (OldPoseParameters[i] != PoseParameter[i])
				bPoseParametersChanged = true;

		// Cycle change? Then re-render
		bool bAnimationChanged = OldCycle != GetCycle() || bBoneControllersChanged || bPoseParametersChanged;
		bool bSequenceChanged = OldSequence != GetSequence();
		bool bScaleChanged = (OldModelScale != GetModelScale());
		if (bAnimationChanged || bSequenceChanged || bScaleChanged)
			InvalidatePhysicsRecursive(InvalidatePhysicsBits.AnimationChanged);

		if (bAnimationChanged || bSequenceChanged) {
			if (ClientSideAnimation) {
				ClientSideAnimationChanged();
			}
		}

		// reset prev cycle if new sequence
		if (NewSequenceParity != PrevNewSequenceParity) {
			// It's important not to call Reset() on a static prop, because if we call
			// Reset(), then the entity will stay in the interpolated entities list
			// forever, wasting CPU.
			StudioHdr? hdr = GetModelPtr();
			if (hdr != null && (hdr.Flags() & StudioHdrFlags.StaticProp) == 0) {
				iv_Cycle.Reset();
			}
		}
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

		StudioHeader? studioHdr = mdlcache.LockStudioHdr(hStudioHdr);
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

	public int LookupAttachment(ReadOnlySpan<char> attachmentName) {
		StudioHdr? hdr = GetModelPtr();
		if (hdr == null)
			return -1;
		return BoneSetup.Studio_FindAttachment(hdr, attachmentName);
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

	public bool GetPoseParameterRange(ReadOnlySpan<char> name, out float minValue, out float maxValue) => GetPoseParameterRange(LookupPoseParameter(name), out minValue, out maxValue);
	public bool GetPoseParameterRange(int parameter, out float minValue, out float maxValue) {
		StudioHdr? pStudioHdr = GetModelPtr();

		if (pStudioHdr != null) {
			if (parameter >= 0 && parameter < pStudioHdr.GetNumPoseParameters()) {
				MStudioPoseParamDesc pose = pStudioHdr.PoseParameter(parameter);
				minValue = pose.Start;
				maxValue = pose.End;
				return true;
			}
		}
		minValue = 0.0f;
		maxValue = 1.0f;
		return false;
	}

	public float GetPoseParameter(ReadOnlySpan<char> name) => GetPoseParameter(LookupPoseParameter(name));
	public float GetPoseParameter(int parameter) {
		StudioHdr? pStudioHdr = GetModelPtr();

		if (pStudioHdr == null)
			return 0.0f;

		if (pStudioHdr.GetNumPoseParameters() < parameter)
			return 0.0f;

		if (parameter < 0)
			return 0.0f;

		return PoseParameter[parameter];
	}

	public float SetPoseParameter(ReadOnlySpan<char> name, float value) => SetPoseParameter(GetModelPtr(), name, value);
	public float SetPoseParameter(int parameter, float value) => SetPoseParameter(GetModelPtr(), parameter, value);
	public float SetPoseParameter(StudioHdr? studioHdr, ReadOnlySpan<char> name, float value) => SetPoseParameter(studioHdr, LookupPoseParameter(studioHdr, name), value);

	public int LookupPoseParameter(ReadOnlySpan<char> name) => LookupPoseParameter(GetModelPtr(), name);
	public int LookupPoseParameter(StudioHdr? studioHdr, ReadOnlySpan<char> name) {
		if (studioHdr == null)
			return 0;

		for (int i = 0; i < studioHdr.GetNumPoseParameters(); i++) {
			if (name.Equals(studioHdr.PoseParameter(i).Name(), StringComparison.OrdinalIgnoreCase))
				return i;
		}

		return -1;
	}

	public float SetPoseParameter(StudioHdr? studioHdr, int parameter, float value) {
		if (studioHdr == null) {
			AssertMsg(false, "C_BaseAnimating.SetPoseParameter: model missing");
			return value;
		}

		if (parameter >= 0) {
			value = BoneSetup.Studio_SetPoseParameter(studioHdr, parameter, value, out float newValue);
			PoseParameter[parameter] = newValue;
		}

		return value;
	}

	class AttachmentData
	{
		public Matrix3x4 AttachmentToWorld;
		public QAngle Rotation;
		public Vector3 OriginVelocity;
		public long LastFramecount;
		public bool AnglesComputed;

		public void Reset() {
			AttachmentToWorld = default;
			Rotation = default;
			AttachmentToWorld = default;
			LastFramecount = default;
			AnglesComputed = default;
		}
	}

	readonly List<AttachmentData> Attachments = [];

	public bool CalcAttachments() {
		return SetupBones(null, -1, Studio.BONE_USED_BY_ATTACHMENT, gpGlobals.CurTime);
	}

	public override bool GetAttachment(int number, out Vector3 origin, out QAngle angles) {
		if (number < 1 || number > Attachments.Count || !CalcAttachments()) {
			// Set this to the model origin/angles so that we don't have stack fungus in origin and angles.
			origin = GetAbsOrigin();
			angles = GetAbsAngles();
			return false;
		}

		AttachmentData pData = Attachments[number - 1];
		if (!pData.AnglesComputed) {
			MathLib.MatrixAngles(pData.AttachmentToWorld, out pData.Rotation);
			pData.AnglesComputed = true;
		}
		angles = pData.Rotation;
		MathLib.MatrixPosition(in pData.AttachmentToWorld, out origin);
		return true;
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
	public TimeUnit_t Cycle;
	public readonly InterpolatedVar<float> iv_Cycle;
	public readonly InterpolatedVarArray<float> iv_flPoseParameter;

	public InlineArrayMaxStudioPoseParam<float> PoseParameter;
	public InlineArrayMaxStudioPoseParam<float> OldPoseParameters;
	public float PrevEventCycle;
	public int EventSequence;
	public InlineArrayMaxStudioBoneCtrls<float> EncodedController;
	public InlineArrayMaxStudioBoneCtrls<float> OldEncodedController;
}
