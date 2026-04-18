using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_PropCrane>;
public class C_PropCrane : C_BaseAnimating
{
	public static readonly RecvTable DT_PropCrane = new(DT_BaseAnimating, [
		RecvPropEHandle(FIELD.OF(nameof(Player))),
		RecvPropBool(FIELD.OF(nameof(MagnetOn))),
		RecvPropBool(FIELD.OF(nameof(EnterAnimOn))),
		RecvPropBool(FIELD.OF(nameof(ExitAnimOn))),
		RecvPropVector(FIELD.OF(nameof(EyeExitEndpoint))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("PropCrane", DT_PropCrane).WithManualClassID(StaticClassIndices.CPropCrane);

	public EHANDLE Player = new();
	public bool MagnetOn;
	public bool EnterAnimOn;
	public bool ExitAnimOn;
	public Vector3 EyeExitEndpoint;
}
