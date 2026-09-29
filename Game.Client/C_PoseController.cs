using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_PoseController>;
[NetworkName("CPoseController")]
public class C_PoseController : C_BaseEntity
{
	public static readonly RecvTable DT_PoseController = new(DT_BaseEntity, [
		RecvPropArray3(FIELD.OF_ARRAY(nameof(HProps)), RecvPropEHandle(FIELD.OF_ARRAYINDEX(nameof(HProps), 0))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(ChPoseIndex)), RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(ChPoseIndex), 0))),
		RecvPropBool(FIELD.OF(nameof(PoseValueParity))),
		RecvPropFloat(FIELD.OF(nameof(PoseValue))),
		RecvPropFloat(FIELD.OF(nameof(InterpolationTime))),
		RecvPropBool(FIELD.OF(nameof(InterpolationWrap))),
		RecvPropFloat(FIELD.OF(nameof(CycleFrequency))),
		RecvPropInt(FIELD.OF(nameof(FModType))),
		RecvPropFloat(FIELD.OF(nameof(FModTimeOffset))),
		RecvPropFloat(FIELD.OF(nameof(FModRate))),
		RecvPropFloat(FIELD.OF(nameof(FModAmplitude))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_PoseController);

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
