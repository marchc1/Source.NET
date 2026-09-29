using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEProjectedDecal>;
[NetworkName("CTEProjectedDecal")]
public class TEProjectedDecal(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEProjectedDecal = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropQAngles(FIELD.OF(nameof(Rotation)), 10, PropFlags.RoundDown),
		SendPropFloat(FIELD.OF(nameof(LDistance)), 10, PropFlags.RoundUp),
		SendPropInt(FIELD.OF(nameof(Index)), 9, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEProjectedDecal);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("m_angRotation")]
	public Vector3 Rotation;
	[NetworkName("m_flDistance")]
	public float LDistance;
	[NetworkName("m_nIndex")]
	public int Index;
}
