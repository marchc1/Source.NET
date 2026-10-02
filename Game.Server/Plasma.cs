using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Plasma>;
[LinkEntityToClass("_plasma")]
[NetworkName("CPlasma")]
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
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Plasma);

	[NetworkName("m_flScale")]
	public float Scale;
	[NetworkName("m_flScaleTime")]
	public float ScaleTime;
	[NetworkName("m_nFlags")]
	public int Flags;
	[NetworkName("m_nPlasmaModelIndex")]
	public int PlasmaModelIndex;
	[NetworkName("m_nPlasmaModelIndex2")]
	public int PlasmaModelIndex2;
	[NetworkName("m_nGlowModelIndex")]
	public int GlowModelIndex;
}
