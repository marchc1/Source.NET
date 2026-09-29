using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_Plasma>;
[NetworkName("CPlasma")]
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
	public static readonly new ClientClass ClientClass = new ClientClass(DT_Plasma);

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
