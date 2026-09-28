using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<PoseController>;
public class PoseController : BaseEntity
{
	public static readonly SendTable DT_PoseController = new(DT_BaseEntity, [
		SendPropArray3(FIELD.OF_ARRAY(nameof(HProps)), SendPropEHandle(FIELD.OF_ARRAYINDEX(nameof(HProps), 0))),
		SendPropArray3(FIELD.OF_ARRAY(nameof(ChPoseIndex)), SendPropInt(FIELD.OF_ARRAYINDEX(nameof(ChPoseIndex), 0), 5, PropFlags.Unsigned)),
		SendPropBool(FIELD.OF(nameof(PoseValueParity))),
		SendPropFloat(FIELD.OF(nameof(PoseValue)), 11, 0),
		SendPropFloat(FIELD.OF(nameof(InterpolationTime)), 11, 0),
		SendPropBool(FIELD.OF(nameof(InterpolationWrap))),
		SendPropFloat(FIELD.OF(nameof(CycleFrequency)), 11, 0),
		SendPropInt(FIELD.OF(nameof(FModType)), 3, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(FModTimeOffset)), 11, 0),
		SendPropFloat(FIELD.OF(nameof(FModRate)), 11, 0),
		SendPropFloat(FIELD.OF(nameof(FModAmplitude)), 11, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("PoseController", DT_PoseController).WithManualClassID(StaticClassIndices.CPoseController);

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
