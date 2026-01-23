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

	public readonly EHANDLE LightingLandmark = new();
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
	public float PlaybackRate;
	public bool ClientSideAnimation;
	public bool ClientSideFrameReset;
	public int NewSequenceParity;
	public int ResetEventsParity;
	public int MuzzleFlashParity;
	public readonly EHANDLE LightingOrigin = new();
	public readonly EHANDLE LightingOriginRelative = new();
	public readonly EHANDLE BoneManipulator = new();
	public readonly EHANDLE FlexManipulator = new();
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

	public TimeUnit_t SequenceDuration(StudioHdr? studioHdr, int sequence){
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
}
