using Game.Shared;

using Source.Common;

using System.Numerics;

namespace Game.Client.HL2;
using FIELD = Source.FIELD<C_PropVehiclePrisonerPod>;

public class C_PropVehiclePrisonerPod : C_PhysicsProp
{
	public static readonly RecvTable DT_PropVehiclePrisonerPod = new(DT_PhysicsProp, [
		RecvPropEHandle(FIELD.OF(nameof(Player))),
		RecvPropBool(FIELD.OF(nameof(EnterAnimOn))),
		RecvPropBool(FIELD.OF(nameof(ExitAnimOn))),
		RecvPropVector(FIELD.OF(nameof(EyeExitEndpoint))),
		RecvPropBool(FIELD.OF(nameof(LimitView)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("PropVehiclePrisonerPod", DT_PropVehiclePrisonerPod).WithManualClassID(StaticClassIndices.CPropVehiclePrisonerPod);

	public readonly EHANDLE Player = new();
	public bool EnterAnimOn = new();
	public bool ExitAnimOn = new();
	public Vector3 EyeExitEndpoint = new();
	public bool LimitView = new();
}
