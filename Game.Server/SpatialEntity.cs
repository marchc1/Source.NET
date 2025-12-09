using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SpatialEntity>;
public class SpatialEntity : BaseEntity
{
	public static readonly SendTable DT_SpatialEntity = new([
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MinFalloff)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MaxFalloff)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(CurWeight)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Enabled))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("SpatialEntity", DT_SpatialEntity).WithManualClassID(StaticClassIndices.CSpatialEntity);

	public float MinFalloff;
	public float MaxFalloff;
	public float CurWeight;
	public bool Enabled;
}
