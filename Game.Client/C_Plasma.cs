using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_Plasma>;
public class C_Plasma : C_BaseEntity
{
	public static readonly RecvTable DT_Plasma = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(Scale))),
		RecvPropFloat(FIELD.OF(nameof(ScaleTime))),
		RecvPropInt(FIELD.OF(nameof(Flags))),
		RecvPropInt(FIELD.OF(nameof(PlasmaModelIndex))),
		RecvPropInt(FIELD.OF(nameof(PlasmaModelIndex2))),
		RecvPropInt(FIELD.OF(nameof(GlowModelIndex))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("Plasma", DT_Plasma).WithManualClassID(StaticClassIndices.CPlasma);

	public float Scale;
	public float ScaleTime;
	public int Flags;
	public int PlasmaModelIndex;
	public int PlasmaModelIndex2;
	public int GlowModelIndex;
}
