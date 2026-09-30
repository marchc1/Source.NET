using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;
namespace Game.Server;

using FIELD_PJ = Source.FIELD<PropJeep>;
using FIELD_PJE = Source.FIELD<PropJeepEpisodic>;
[NetworkName("CPropJeep")]
public partial class PropJeep : PropVehicleDriveable
{
	public static readonly SendTable DT_PropJeep = new(DT_PropVehicleDriveable, [
		SendPropBool(NetworkVarFields.HeadlightIsOn)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PropJeep);

	[NetworkName("m_bHeadlightIsOn")]
	[NetworkVar] public partial bool HeadlightIsOn { get; set; }
}

[NetworkName("CPropJeepEpisodic")]
public partial class PropJeepEpisodic : PropJeep
{
	public static readonly SendTable DT_CPropJeepEpisodic = new(DT_PropJeep, [
		SendPropInt(NetworkVarFields.NumRadarContacts, 8),

		SendPropVector(FIELD_PJE.OF_SENDINFO_ARRAY(PropJeepEpisodic.NetworkVarFields.RadarContactPos), 0, PropFlags.Coord),
		SendPropArray2(null!, 24, "m_vecRadarContactPos"),

		SendPropInt(FIELD_PJE.OF_SENDINFO_ARRAY(PropJeepEpisodic.NetworkVarFields.RadarContactType), 4),
		SendPropArray2(null!, 24, "m_iRadarContactType"),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_CPropJeepEpisodic);

	[NetworkName("m_iNumRadarContacts")]
	[NetworkVar] public partial int NumRadarContacts { get; set; }
	[NetworkName("m_vecRadarContactPos")]
	[NetworkVar] public partial NetworkArray<InlineArray24<Vector3>, Vector3> RadarContactPos { get; }
	[NetworkName("m_iRadarContactType")]
	[NetworkVar] public partial NetworkArray<InlineArray24<int>, int> RadarContactType { get; }
}
