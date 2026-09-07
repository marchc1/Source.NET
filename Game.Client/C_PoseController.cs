using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_PoseController>;
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
	public static readonly new ClientClass ClientClass = new ClientClass("PoseController", DT_PoseController).WithManualClassID(StaticClassIndices.CPoseController);

	public const int MAX_POSE_CONTROLLED_PROPS = 4;
	public InlineArray4<EHANDLE> HProps;
	public InlineArray4<byte> ChPoseIndex;
	public bool PoseValueParity;
	public float PoseValue;
	public float InterpolationTime;
	public bool InterpolationWrap;
	public float CycleFrequency;
	public int FModType;
	public float FModTimeOffset;
	public float FModRate;
	public float FModAmplitude;
}
