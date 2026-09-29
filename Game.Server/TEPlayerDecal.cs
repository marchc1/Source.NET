using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEPlayerDecal>;
[NetworkName("CTEPlayerDecal")]
public class TEPlayerDecal(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEPlayerDecal = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(Entity)), 13, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Player)), 7, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEPlayerDecal);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("m_nEntity")]
	public int Entity;
	[NetworkName("m_nPlayer")]
	public int Player;
}
