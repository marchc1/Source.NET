using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEFizz>;
[NetworkName("CTEFizz")]
public class C_TEFizz : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEFizz = new(DT_BaseTempEntity, [
		RecvPropInt(FIELD.OF(nameof(Entity))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropInt(FIELD.OF(nameof(Density))),
		RecvPropInt(FIELD.OF(nameof(Current))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEFizz);

	[NetworkName("m_nEntity")]
	public int Entity;
	[NetworkName("m_nModelIndex")]
	public int ModelIndex;
	[NetworkName("m_nDensity")]
	public int Density;
	[NetworkName("m_nCurrent")]
	public int Current;
}

public static partial class TempEnts
{
	public static void TE_Fizz(IRecipientFilter filter, float delay, C_BaseEntity? ed, int modelIndex, int density, int current) {
		throw new NotImplementedException();
	}
}
