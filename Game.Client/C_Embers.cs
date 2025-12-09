using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_Embers>;
public class C_Embers : C_BaseEntity
{
	public static readonly RecvTable DT_Embers = new(DT_BaseEntity, [
		RecvPropInt(FIELD.OF(nameof(Density))),
		RecvPropInt(FIELD.OF(nameof(Lifetime))),
		RecvPropInt(FIELD.OF(nameof(Speed))),
		RecvPropInt(FIELD.OF(nameof(Emit))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("Embers", DT_Embers).WithManualClassID(StaticClassIndices.CEmbers);

	public int Density;
	public int Lifetime;
	public new int Speed;
	public int Emit;
}
