using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;
namespace Game.Server;

using FIELD_PJ = Source.FIELD<PropJeep>;
using FIELD_PJE = Source.FIELD<PropJeepEpisodic>;
public class PropJeep : PropVehicleDriveable
{
	public static readonly SendTable DT_PropJeep = new(DT_PropVehicleDriveable, [
		SendPropBool(FIELD_PJ.OF(nameof(HeadlightIsOn)))
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("PropJeep", DT_PropJeep).WithManualClassID(StaticClassIndices.CPropJeep);

	public bool HeadlightIsOn;
}

public class PropJeepEpisodic : PropJeep
{
	public static readonly SendTable DT_PropJeepEpisodic = new(DT_PropJeep, [
		SendPropInt(FIELD_PJE.OF(nameof(NumRadarContacts)), 8),

		SendPropVector(FIELD_PJE.OF_ARRAYINDEX(nameof(RadarContactPos), 0), 0, PropFlags.Coord),
		SendPropArray2(null!, 24, "RadarContactPos"),

		SendPropInt(FIELD_PJE.OF_ARRAYINDEX(nameof(RadarContactType), 0), 3),
		SendPropArray2(null!, 24, "RadarContactType"),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("PropJeepEpisodic", DT_PropJeepEpisodic).WithManualClassID(StaticClassIndices.CPropJeepEpisodic);

	public int NumRadarContacts;
	public InlineArray24<Vector3> RadarContactPos;
	public InlineArray24<int> RadarContactType;
}
