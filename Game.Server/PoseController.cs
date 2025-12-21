using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<PoseController>;
public class PoseController : BaseEntity
{
	public static readonly SendTable DT_PoseController = new(DT_BaseEntity, [
		SendPropInt(FIELD.OF(nameof(HProps)), 23, PropFlags.ProxyAlwaysYes | PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(ChPoseIndex)), 5, PropFlags.ProxyAlwaysYes | PropFlags.Unsigned),
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

	public int HProps;
	public int ChPoseIndex;
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
