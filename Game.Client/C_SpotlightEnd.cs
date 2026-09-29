using Game.Shared;

using Source;
using Source.Common;

namespace Game.Client;
using FIELD = FIELD<C_SpotlightEnd>;

[NetworkName("CSpotlightEnd")]
public class C_SpotlightEnd : C_BaseEntity
{
	public static readonly RecvTable DT_SpotlightEnd = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(LightScale))),
		RecvPropFloat(FIELD.OF(nameof(Radius))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_SpotlightEnd);

	[NetworkName("m_flLightScale")]
	public float LightScale;
	[NetworkName("m_Radius")]
	public float Radius;
}

