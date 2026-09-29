using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEEnergySplash>;
[NetworkName("CTEEnergySplash")]
public class C_TEEnergySplash : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEEnergySplash = new([
		RecvPropVector(FIELD.OF(nameof(Pos))),
		RecvPropVector(FIELD.OF(nameof(Dir))),
		RecvPropInt(FIELD.OF(nameof(Explosive))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEEnergySplash);

	[NetworkName("m_vecPos")]
	public Vector3 Pos;
	[NetworkName("m_vecDir")]
	public Vector3 Dir;
	[NetworkName("m_bExplosive")]
	public int Explosive;
}

public static partial class TempEnts
{
	public static void TE_EnergySplash(IRecipientFilter filter, float delay, in Vector3 pos, in Vector3 dir, bool explosive) {
		throw new NotImplementedException();
	}
}
