using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEMetalSparks>;
[NetworkName("CTEMetalSparks")]
public class TEMetalSparks(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEMetalSparks = new([
		SendPropVector(FIELD.OF(nameof(Pos)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Dir)), 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEMetalSparks);

	[NetworkName("m_vecPos")]
	public Vector3 Pos;
	[NetworkName("m_vecDir")]
	public Vector3 Dir;
}
