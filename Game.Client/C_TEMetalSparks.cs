using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEMetalSparks>;
[NetworkName("CTEMetalSparks")]
public class C_TEMetalSparks : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEMetalSparks = new([
		RecvPropVector(FIELD.OF(nameof(Pos))),
		RecvPropVector(FIELD.OF(nameof(Dir))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEMetalSparks);

	[NetworkName("m_vecPos")]
	public Vector3 Pos;
	[NetworkName("m_vecDir")]
	public Vector3 Dir;
}

public static partial class TempEnts
{
	public static void TE_MetalSparks(IRecipientFilter filter, float delay, in Vector3 pos, in Vector3 dir) {
		throw new NotImplementedException();
	}
}
