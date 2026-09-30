using Source.Common;
using Source;

using Game.Shared;

namespace Game.Server;


using FIELD = FIELD<SpotlightEnd>;

[NetworkName("CSpotlightEnd")]
public partial class SpotlightEnd : BaseEntity
{
	public static readonly SendTable DT_SpotlightEnd = new(DT_BaseEntity, [
		SendPropFloat(NetworkVarFields.LightScale, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Radius, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SpotlightEnd);

	[NetworkName("m_flLightScale")]
	[NetworkVar] public partial float LightScale { get; set; }
	[NetworkName("m_Radius")]
	[NetworkVar] public partial float Radius { get; set; }
}
