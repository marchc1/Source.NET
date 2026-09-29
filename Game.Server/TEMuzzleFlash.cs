using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEMuzzleFlash>;
[NetworkName("CTEMuzzleFlash")]
public class TEMuzzleFlash(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEMuzzleFlash = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Angles)), 0, PropFlags.Coord),
		SendPropFloat(FIELD.OF(nameof(Scale)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(Type)), 4, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEMuzzleFlash);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("m_vecAngles")]
	public Vector3 Angles;
	[NetworkName("m_flScale")]
	public float Scale;
	[NetworkName("m_nType")]
	public int Type;
}
