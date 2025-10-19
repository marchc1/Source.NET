using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;


using FIELD = FIELD<SpotlightEnd>;

public class SpotlightEnd : BaseEntity
{
	public static readonly SendTable DT_SpotlightEnd = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(LightScale)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Radius)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("SpotlightEnd", DT_SpotlightEnd).WithManualClassID(StaticClassIndices.CSpotlightEnd);

	public float LightScale;
	public float Radius;
}
