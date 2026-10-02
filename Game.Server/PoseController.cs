using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<PoseController>;
[LinkEntityToClass("point_posecontroller")]
[NetworkName("CPoseController")]
public class PoseController : BaseEntity
{
	public static readonly SendTable DT_PoseController = new(DT_BaseEntity, [
		SendPropArray3(FIELD.OF_ARRAY(nameof(HProps)), SendPropEHandle(FIELD.OF_ARRAYINDEX(nameof(HProps), 0))),
		SendPropArray3(FIELD.OF_ARRAY(nameof(ChPoseIndex)), SendPropInt(FIELD.OF_ARRAYINDEX(nameof(ChPoseIndex), 0), 5, PropFlags.Unsigned)),
		SendPropBool(FIELD.OF(nameof(PoseValueParity))),
		SendPropFloat(FIELD.OF(nameof(PoseValue)), 11, 0, 0.0f, 1.0f),
		SendPropFloat(FIELD.OF(nameof(InterpolationTime)), 11, 0, 0.0f, 10.0f),
		SendPropBool(FIELD.OF(nameof(InterpolationWrap))),
		SendPropFloat(FIELD.OF(nameof(CycleFrequency)), 11, 0, -10.0f, 10.0f),
		SendPropInt(FIELD.OF(nameof(FModType)), 3, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(FModTimeOffset)), 11, 0, -1.0f, 1.0f),
		SendPropFloat(FIELD.OF(nameof(FModRate)), 11, 0, -10.0f, 10.0f),
		SendPropFloat(FIELD.OF(nameof(FModAmplitude)), 11, 0, 0.0f, 10.0f),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PoseController);

	public const int MAX_POSE_CONTROLLED_PROPS = 4;
	[NetworkName("m_hProps")]
	public InlineArray4<EHANDLE> HProps;
	[NetworkName("m_chPoseIndex")]
	public InlineArray4<byte> ChPoseIndex;
	[NetworkName("m_bPoseValueParity")]
	public bool PoseValueParity;
	[NetworkName("m_fPoseValue")]
	public float PoseValue;
	[NetworkName("m_fInterpolationTime")]
	public float InterpolationTime;
	[NetworkName("m_bInterpolationWrap")]
	public bool InterpolationWrap;
	[NetworkName("m_fCycleFrequency")]
	public float CycleFrequency;
	[NetworkName("m_nFModType")]
	public int FModType;
	[NetworkName("m_fFModTimeOffset")]
	public float FModTimeOffset;
	[NetworkName("m_fFModRate")]
	public float FModRate;
	[NetworkName("m_fFModAmplitude")]
	public float FModAmplitude;
}
