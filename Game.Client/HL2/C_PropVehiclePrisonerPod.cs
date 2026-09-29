using Game.Shared;

using Source.Common;

using System.Numerics;

namespace Game.Client.HL2;
using FIELD = Source.FIELD<C_PropVehiclePrisonerPod>;

[NetworkName("CPropVehiclePrisonerPod")]
public class C_PropVehiclePrisonerPod : C_PhysicsProp
{
	public static readonly RecvTable DT_PropVehiclePrisonerPod = new(DT_PhysicsProp, [
		RecvPropEHandle(FIELD.OF(nameof(Player))),
		RecvPropBool(FIELD.OF(nameof(EnterAnimOn))),
		RecvPropBool(FIELD.OF(nameof(ExitAnimOn))),
		RecvPropVector(FIELD.OF(nameof(EyeExitEndpoint))),
		RecvPropBool(FIELD.OF(nameof(LimitView))),
		RecvPropBool(FIELD.OF(nameof(Locked))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_PropVehiclePrisonerPod);

	[NetworkName("m_hPlayer")]
	public EHANDLE Player = new();
	[NetworkName("m_bEnterAnimOn")]
	public bool EnterAnimOn = new();
	[NetworkName("m_bExitAnimOn")]
	public bool ExitAnimOn = new();
	[NetworkName("m_vecEyeExitEndpoint")]
	public Vector3 EyeExitEndpoint = new();
	[NetworkName("m_bLimitView")]
	public bool LimitView = new();
	[NetworkName("m_bLocked")]
	public bool Locked = new();
}
