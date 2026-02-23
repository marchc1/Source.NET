using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;

namespace Game.Server;

using FIELD = Source.FIELD<Game.Server.BaseAnimating>;
using FIELD_ILR = Source.FIELD<Game.Server.InfoLightingRelative>;

public partial class InfoLightingRelative : BaseEntity
{
	public static readonly SendTable DT_InfoLightingRelative = new(DT_BaseEntity, [
		SendPropEHandle(FIELD_ILR.OF(nameof(LightingLandmark)))
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("InfoLightingRelative", DT_InfoLightingRelative).WithManualClassID(StaticClassIndices.CInfoLightingRelative);

	public EHANDLE LightingLandmark = new();
}

public class BaseAnimating : BaseEntity
{
	public const int ANIMATION_SEQUENCE_BITS = 12;
	public const int ANIMATION_SKIN_BITS = 10;
	public const int ANIMATION_BODY_BITS = 32;
	public const int ANIMATION_HITBOXSET_BITS = 2;
	public const int ANIMATION_POSEPARAMETER_BITS = 11;
	public const int ANIMATION_PLAYBACKRATE_BITS = 8;

	public static readonly SendTable DT_ServerAnimationData = new([
		SendPropFloat(FIELD.OF(nameof(Cycle)), ANIMATION_CYCLE_BITS, PropFlags.ChangesOften|PropFlags.RoundDown, -1.0f, 1.0f)
	]);
	public static readonly ServerClass CC_ServerAnimationData = new ServerClass("ServerAnimationData", DT_ServerAnimationData);
	public static readonly SendTable DT_BaseAnimating = new(DT_BaseEntity, [
		SendPropInt( FIELD.OF(nameof(ForceBone)), 8, 0 ),
		SendPropVector( FIELD.OF(nameof(Force)), 0, PropFlags.NoScale ),

		SendPropInt( FIELD.OF(nameof(Skin)), ANIMATION_SKIN_BITS),
		SendPropInt( FIELD.OF(nameof(Body)), ANIMATION_BODY_BITS),

		SendPropInt( FIELD.OF(nameof(HitboxSet)),ANIMATION_HITBOXSET_BITS, PropFlags.Unsigned ),

		SendPropFloat( FIELD.OF(nameof(ModelScale)) ),

		SendPropArray3( FIELD.OF_ARRAY(nameof(PoseParameter)), SendPropFloat(null!, ANIMATION_POSEPARAMETER_BITS, 0, 0.0f, 1.0f ) ),

		SendPropInt( FIELD.OF(nameof(Sequence)), ANIMATION_SEQUENCE_BITS, PropFlags.Unsigned ),
		SendPropFloat( FIELD.OF(nameof(PlaybackRate)), ANIMATION_PLAYBACKRATE_BITS, PropFlags.RoundUp, -4.0f, 12.0f ),

		SendPropArray3(FIELD.OF_ARRAY(nameof(EncodedController)), SendPropFloat(null!, 11, PropFlags.RoundDown, 0.0f, 1.0f ) ),

		SendPropInt( FIELD.OF(nameof( ClientSideAnimation )), 1, PropFlags.Unsigned ),
		SendPropInt( FIELD.OF(nameof( ClientSideFrameReset )), 1, PropFlags.Unsigned ),

		SendPropInt( FIELD.OF(nameof( NewSequenceParity) ), (int)EntityEffects.ParityBits, PropFlags.Unsigned ),
		SendPropInt( FIELD.OF(nameof( ResetEventsParity )), (int)EntityEffects.ParityBits, PropFlags.Unsigned ),
		SendPropInt( FIELD.OF(nameof( MuzzleFlashParity )), (int)EntityEffects.MuzzleflashBits, PropFlags.Unsigned ),

		SendPropEHandle( FIELD.OF(nameof( LightingOrigin )) ),
		SendPropEHandle( FIELD.OF(nameof( LightingOriginRelative )) ),

		SendPropDataTable( "serveranimdata", DT_ServerAnimationData, SendProxy_ClientSideAnimation ),

		SendPropFloat( FIELD.OF(nameof(FadeMinDist) ), 0, PropFlags.NoScale ),
		SendPropFloat( FIELD.OF(nameof(FadeMaxDist )), 0, PropFlags.NoScale ),
		SendPropFloat( FIELD.OF(nameof(FadeScale )), 0, PropFlags.NoScale ),

		// Gmod specific
		SendPropEHandle(FIELD.OF(nameof(BoneManipulator))),
		SendPropEHandle(FIELD.OF(nameof(FlexManipulator))),
		SendPropVector(FIELD.OF(nameof(OverrideViewTarget)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("BaseAnimating", DT_BaseAnimating).WithManualClassID(StaticClassIndices.CBaseAnimating);

	public int ForceBone;
	public Vector3 Force;
	public int Skin;
	public int Body;
	public int HitboxSet;

	public float ModelScale;
	public InlineArrayMaxStudioPoseParam<float> PoseParameter;
	public InlineArrayMaxStudioPoseParam<float> OldPoseParameters;
	public float PrevEventCycle;
	public int EventSequence;
	public InlineArrayMaxStudioBoneCtrls<float> EncodedController;
	public InlineArrayMaxStudioBoneCtrls<float> OldEncodedController;
	public int Sequence;
	public TimeUnit_t PlaybackRate;
	public bool ClientSideAnimation;
	public bool ClientSideFrameReset;
	public int NewSequenceParity;
	public int ResetEventsParity;
	public int MuzzleFlashParity;
	public EHANDLE LightingOrigin = new();
	public EHANDLE LightingOriginRelative = new();
	public EHANDLE BoneManipulator = new();
	public EHANDLE FlexManipulator = new();
	public float FadeMinDist;
	public float FadeMaxDist;
	public float FadeScale;
	public TimeUnit_t Cycle;
	public Vector3 OverrideViewTarget;

	public bool IsModelScaleFractional() => ModelScale < 1.0f;
	public bool IsModelScaled() => ModelScale > 1.0f + float.Epsilon || ModelScale < 1.0f - float.Epsilon;
	public float GetModelScale() => ModelScale;

	// todo...
	public StudioHdr? GetModelPtr() => null!;

	public ReadOnlySpan<float> GetPoseParameterArray() => PoseParameter;

	public int GetSequence() => Sequence;

	public TimeUnit_t SequenceDuration(StudioHdr? studioHdr, int sequence) {
		if (studioHdr == null) {
			DevWarning(2, $"BaseAnimating.SequenceDuration( {sequence} ) NULL pstudiohdr on {GetClassname()}!\n");
			return 0.1;
		}
		if (studioHdr.SequencesAvailable()) {
			return 0.1;
		}
		if (sequence >= studioHdr.GetNumSeq() || sequence < 0) {
			DevWarning(2, $"BaseAnimating.SequenceDuration( {sequence} ) out of range\n");
			return 0.1;
		}

		return BoneSetup.Studio_Duration(studioHdr, sequence, GetPoseParameterArray());
	}
	public TimeUnit_t SequenceDuration(int sequence) => SequenceDuration(GetModelPtr(), sequence);
	public TimeUnit_t SequenceDuration() => SequenceDuration(GetSequence());
	public virtual void DoMuzzleFlash() => MuzzleFlashParity = unchecked((byte)((MuzzleFlashParity + 1) & ((1 << (int)EntityEffects.MuzzleflashBits) - 1)));
	public virtual void SetSequence(int sequence) {
		Sequence = sequence;
	}
	public int SelectWeightedSequence(Activity activity) {
		return Animation.SelectWeightedSequence(GetModelPtr(), activity, GetSequence());
	}
	public float GroundSpeed;
	public bool SequenceLoops;
	public bool ResetSequenceInfoOnLoad;
	public bool DynamicModelLoading;
	public bool SequenceFinished;
	public TimeUnit_t LastEventCheck;
	public TimeUnit_t GetCycle() => Cycle;
	public void SetCycle(TimeUnit_t cycle) => Cycle = cycle;
	public float GetSequenceMoveDist(StudioHdr? studioHdr, int sequence) {
		Animation.GetSequenceLinearMotion(studioHdr, sequence, GetPoseParameterArray(), out Vector3 ret);

		return ret.Length();
	}
	public bool IsDynamicModelLoading() => DynamicModelLoading;
	public float GetSequenceGroundSpeed(StudioHdr? studioHdr, int sequence) {
		TimeUnit_t t = SequenceDuration(studioHdr, sequence);

		if (t > 0) 
			return (GetSequenceMoveDist(studioHdr, sequence) / (float)t);
		else 
			return 0;
	}
	public void ResetSequenceInfo() {
		if (GetSequence() == -1)
			// This shouldn't happen.  Setting m_nSequence blindly is a horrible coding practice.
			SetSequence(0);

		if (IsDynamicModelLoading()) {
			ResetSequenceInfoOnLoad = true;
			return;
		}

		StudioHdr? studioHdr = GetModelPtr();
		GroundSpeed = GetSequenceGroundSpeed(studioHdr, GetSequence()) * GetModelScale();
		SequenceLoops = ((Animation.GetSequenceFlags(studioHdr, GetSequence()) & StudioAnimSeqFlags.Looping) != 0);
		// m_flAnimTime = gpGlobals->time;
		PlaybackRate = 1.0;
		SequenceFinished = false;
		LastEventCheck = 0;

		NewSequenceParity = (NewSequenceParity + 1) & (int)EntityEffects.ParityMask;
		ResetEventsParity = (ResetEventsParity + 1) & (int)EntityEffects.ParityMask;

		// FIXME: why is this called here?  Nothing should have changed to make this necessary
		if (studioHdr != null)
			Animation.SetEventIndexForSequence(studioHdr.Seqdesc(GetSequence()));
	}

	public int FindTransitionSequence(int currentSequence, int goalSequence) {
		StudioHdr? hdr = GetModelPtr();
		if (hdr == null) {
			return -1;
		}

		int dir = 1;
		int sequence = Animation.FindTransitionSequence(hdr, currentSequence, goalSequence, ref dir);
		if (dir != 1)
			return -1;
		else
			return sequence;
	}
}
