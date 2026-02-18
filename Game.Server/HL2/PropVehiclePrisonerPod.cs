using Game.Server;
using Game.Shared;

using Source.Common;

using System.Numerics;

namespace Game.Server.HL2;
using FIELD = Source.FIELD<PropVehiclePrisonerPod>;
public class PropVehiclePrisonerPod : PhysicsProp
{
	public static readonly SendTable DT_PropVehiclePrisonerPod = new(DT_PhysicsProp, [
		SendPropEHandle(FIELD.OF(nameof(Player))),
		SendPropBool(FIELD.OF(nameof(EnterAnimOn))),
		SendPropBool(FIELD.OF(nameof(ExitAnimOn))),
		SendPropVector(FIELD.OF(nameof(EyeExitEndpoint)), 0, PropFlags.Coord),
		SendPropBool(FIELD.OF(nameof(LimitView)))
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("PropVehiclePrisonerPod", DT_PropVehiclePrisonerPod).WithManualClassID(StaticClassIndices.CPropVehiclePrisonerPod);

	public EHANDLE Player = new();
	public bool EnterAnimOn = new();
	public bool ExitAnimOn = new();
	public Vector3 EyeExitEndpoint = new();
	public bool LimitView = new();
}
