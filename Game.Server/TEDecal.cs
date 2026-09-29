using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEDecal>;
[NetworkName("CTEDecal")]
public class TEDecal(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEDecal = new(DT_BaseTempEntity, [
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Start)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(Entity)), 13, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Hitbox)), 12, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Index)), 9, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEDecal);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("m_vecStart")]
	public Vector3 Start;
	[NetworkName("m_nEntity")]
	public int Entity;
	[NetworkName("m_nHitbox")]
	public int Hitbox;
	[NetworkName("m_nIndex")]
	public int Index;
}
