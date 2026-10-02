using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;


using FIELD = FIELD<SpotlightEnd>;

[LinkEntityToClass("spotlight_end")]
[NetworkName("CSpotlightEnd")]
public class SpotlightEnd : BaseEntity
{
	public static readonly SendTable DT_SpotlightEnd = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(LightScale)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Radius)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SpotlightEnd);

	[NetworkName("m_flLightScale")]
	public float LightScale;
	[NetworkName("m_Radius")]
	public float Radius;
}
