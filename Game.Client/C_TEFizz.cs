using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEFizz>;
public class C_TEFizz : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEFizz = new(DT_BaseTempEntity, [
		RecvPropInt(FIELD.OF(nameof(Entity))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropInt(FIELD.OF(nameof(Density))),
		RecvPropInt(FIELD.OF(nameof(Current))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEFizz", DT_TEFizz).WithManualClassID(StaticClassIndices.CTEFizz);

	public int Entity;
	public int ModelIndex;
	public int Density;
	public int Current;
}
