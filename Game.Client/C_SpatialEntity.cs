using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_SpatialEntity>;

[NetworkName("CSpatialEntity")]
public class C_SpatialEntity : C_BaseEntity
{
	public static readonly RecvTable DT_SpatialEntity = new([
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropFloat(FIELD.OF(nameof(MinFalloff))),
		RecvPropFloat(FIELD.OF(nameof(MaxFalloff))),
		RecvPropFloat(FIELD.OF(nameof(CurWeight))),
		RecvPropBool(FIELD.OF(nameof(Enabled))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_SpatialEntity);

	[NetworkName("m_MinFalloff")]
	public float MinFalloff;
	[NetworkName("m_MaxFalloff")]
	public float MaxFalloff;
	[NetworkName("m_flCurWeight")]
	public float CurWeight;
	[NetworkName("m_bEnabled")]
	public bool Enabled;
}
