using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<PoseController>;
[NetworkName("CPoseController")]
public partial class PoseController : BaseEntity
{
	public static readonly SendTable DT_PoseController = new(DT_BaseEntity, [
		SendPropArray3(PoseController.NetworkVarFields.HProps, SendPropEHandle(PoseController.NetworkVarFields.HProps.AtIndex(0)!)),
		SendPropArray3(PoseController.NetworkVarFields.ChPoseIndex, SendPropInt(PoseController.NetworkVarFields.ChPoseIndex.AtIndex(0)!, 5, PropFlags.Unsigned)),
		SendPropBool(NetworkVarFields.PoseValueParity),
		SendPropFloat(NetworkVarFields.PoseValue, 11, 0, 0.0f, 1.0f),
		SendPropFloat(NetworkVarFields.InterpolationTime, 11, 0, 0.0f, 10.0f),
		SendPropBool(NetworkVarFields.InterpolationWrap),
		SendPropFloat(NetworkVarFields.CycleFrequency, 11, 0, -10.0f, 10.0f),
		SendPropInt(NetworkVarFields.FModType, 3, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.FModTimeOffset, 11, 0, -1.0f, 1.0f),
		SendPropFloat(NetworkVarFields.FModRate, 11, 0, -10.0f, 10.0f),
		SendPropFloat(NetworkVarFields.FModAmplitude, 11, 0, 0.0f, 10.0f),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PoseController);

	public const int MAX_POSE_CONTROLLED_PROPS = 4;
	[NetworkName("m_hProps")]
	[NetworkVar] public partial NetworkArray<InlineArray4<EHANDLE>, EHANDLE> HProps { get; }
	[NetworkName("m_chPoseIndex")]
	[NetworkVar] public partial NetworkArray<InlineArray4<byte>, byte> ChPoseIndex { get; }
	[NetworkName("m_bPoseValueParity")]
	[NetworkVar] public partial bool PoseValueParity { get; set; }
	[NetworkName("m_fPoseValue")]
	[NetworkVar] public partial float PoseValue { get; set; }
	[NetworkName("m_fInterpolationTime")]
	[NetworkVar] public partial float InterpolationTime { get; set; }
	[NetworkName("m_bInterpolationWrap")]
	[NetworkVar] public partial bool InterpolationWrap { get; set; }
	[NetworkName("m_fCycleFrequency")]
	[NetworkVar] public partial float CycleFrequency { get; set; }
	[NetworkName("m_nFModType")]
	[NetworkVar] public partial int FModType { get; set; }
	[NetworkName("m_fFModTimeOffset")]
	[NetworkVar] public partial float FModTimeOffset { get; set; }
	[NetworkName("m_fFModRate")]
	[NetworkVar] public partial float FModRate { get; set; }
	[NetworkName("m_fFModAmplitude")]
	[NetworkVar] public partial float FModAmplitude { get; set; }
}
