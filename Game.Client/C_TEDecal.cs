using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEDecal>;
[NetworkName("CTEDecal")]
public class C_TEDecal : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEDecal = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Start))),
		RecvPropInt(FIELD.OF(nameof(Entity))),
		RecvPropInt(FIELD.OF(nameof(Hitbox))),
		RecvPropInt(FIELD.OF(nameof(Index))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEDecal);

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

public static partial class TempEnts
{
	public static void TE_Decal(IRecipientFilter filter, float delay, in Vector3 pos, in Vector3 start, int entity, int hitbox, int index) {
		throw new NotImplementedException();
	}
}
