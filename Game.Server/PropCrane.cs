using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<PropCrane>;
public class PropCrane : BaseAnimating
{
	public static readonly SendTable DT_PropCrane = new(DT_BaseAnimating, [
		SendPropEHandle(FIELD.OF(nameof(Player))),
		SendPropBool(FIELD.OF(nameof(MagnetOn))),
		SendPropBool(FIELD.OF(nameof(EnterAnimOn))),
		SendPropBool(FIELD.OF(nameof(ExitAnimOn))),
		SendPropVector(FIELD.OF(nameof(EyeExitEndpoint)), 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("PropCrane", DT_PropCrane).WithManualClassID(StaticClassIndices.CPropCrane);

	public EHANDLE Player = new();
	public bool MagnetOn;
	public bool EnterAnimOn;
	public bool ExitAnimOn;
	public Vector3 EyeExitEndpoint;
}
