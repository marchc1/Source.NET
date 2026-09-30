using Game.Server;
using Game.Shared;

using Source.Common;

using System.Numerics;

namespace Game.Server.HL2;
using FIELD = Source.FIELD<PropVehiclePrisonerPod>;
[NetworkName("CPropVehiclePrisonerPod")]
public partial class PropVehiclePrisonerPod : PhysicsProp
{
	public static readonly SendTable DT_PropVehiclePrisonerPod = new(DT_PhysicsProp, [
		SendPropEHandle(PropVehiclePrisonerPod.NetworkVarFields.Player),
		SendPropBool(PropVehiclePrisonerPod.NetworkVarFields.EnterAnimOn),
		SendPropBool(PropVehiclePrisonerPod.NetworkVarFields.ExitAnimOn),
		SendPropVector(PropVehiclePrisonerPod.NetworkVarFields.EyeExitEndpoint, 0, PropFlags.Coord),
		SendPropBool(FIELD.OF(nameof(LimitView))),
		SendPropBool(NetworkVarFields.Locked),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PropVehiclePrisonerPod);

	[NetworkName("m_hPlayer")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> Player { get; }
	[NetworkName("m_bEnterAnimOn")]
	[NetworkVar] public partial bool EnterAnimOn { get; set; }
	[NetworkName("m_bExitAnimOn")]
	[NetworkVar] public partial bool ExitAnimOn { get; set; }
	[NetworkName("m_vecEyeExitEndpoint")]
	[NetworkVar] public partial Vector3 EyeExitEndpoint { get; set; }
	[NetworkName("m_bLimitView")]
	public bool LimitView = new();
	[NetworkName("m_bLocked")]
	[NetworkVar] public partial bool Locked { get; set; }
}
