using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_Embers>;
[NetworkName("CEmbers")]
public class C_Embers : C_BaseEntity
{
	public static readonly RecvTable DT_Embers = new(DT_BaseEntity, [
		RecvPropInt(FIELD.OF(nameof(Density))),
		RecvPropInt(FIELD.OF(nameof(Lifetime))),
		RecvPropInt(FIELD.OF(nameof(Speed))),
		RecvPropInt(FIELD.OF(nameof(Emit))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_Embers);

	[NetworkName("m_nDensity")]
	public int Density;
	[NetworkName("m_nLifetime")]
	public int Lifetime;
	[NetworkName("m_nSpeed")]
	public new int Speed;
	[NetworkName("m_bEmit")]
	public int Emit;
}
