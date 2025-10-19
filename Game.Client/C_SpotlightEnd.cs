using Game.Shared;

using Source;
using Source.Common;

namespace Game.Client;
using FIELD = FIELD<C_SpotlightEnd>;

public class C_SpotlightEnd : C_BaseEntity
{
	public static readonly RecvTable DT_SpotlightEnd = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(LightScale))),
		RecvPropFloat(FIELD.OF(nameof(Radius))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("SpotlightEnd", DT_SpotlightEnd).WithManualClassID(StaticClassIndices.CSpotlightEnd);

	public float LightScale;
	public float Radius;
}

