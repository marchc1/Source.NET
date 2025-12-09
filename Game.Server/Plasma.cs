using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Plasma>;
public class Plasma : BaseEntity
{
	public static readonly SendTable DT_Plasma = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(Scale)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(ScaleTime)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(Flags)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(PlasmaModelIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(PlasmaModelIndex2)), 14, 0),
		SendPropInt(FIELD.OF(nameof(GlowModelIndex)), 14, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("Plasma", DT_Plasma).WithManualClassID(StaticClassIndices.CPlasma);

	public float Scale;
	public float ScaleTime;
	public int Flags;
	public int PlasmaModelIndex;
	public int PlasmaModelIndex2;
	public int GlowModelIndex;
}
