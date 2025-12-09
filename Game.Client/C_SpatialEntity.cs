using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_SpatialEntity>;

public class C_SpatialEntity : C_BaseEntity
{
	public static readonly RecvTable DT_SpatialEntity = new([
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropFloat(FIELD.OF(nameof(MinFalloff))),
		RecvPropFloat(FIELD.OF(nameof(MaxFalloff))),
		RecvPropFloat(FIELD.OF(nameof(CurWeight))),
		RecvPropBool(FIELD.OF(nameof(Enabled))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("SpatialEntity", DT_SpatialEntity).WithManualClassID(StaticClassIndices.CSpatialEntity);

	public float MinFalloff;
	public float MaxFalloff;
	public float CurWeight;
	public bool Enabled;
}
