using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;
namespace Game.Client;

using FIELD_PJ = Source.FIELD<C_PropJeep>;
using FIELD_PJE = Source.FIELD<C_PropJeepEpisodic>;
public class C_PropJeep : C_PropVehicleDriveable
{
	public static readonly RecvTable DT_PropJeep = new(DT_PropVehicleDriveable, [
		RecvPropBool(FIELD_PJ.OF(nameof(HeadlightIsOn)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("PropJeep", DT_PropJeep).WithManualClassID(StaticClassIndices.CPropJeep);

	public bool HeadlightIsOn;
}

public class C_PropJeepEpisodic : C_PropJeep
{
	public static readonly RecvTable DT_PropJeepEpisodic = new(DT_PropJeep, [
		RecvPropInt(FIELD_PJE.OF(nameof(NumRadarContacts))),

		RecvPropVector(FIELD_PJE.OF_ARRAYINDEX(nameof(RadarContactPos), 0)),
		RecvPropArray2(null!, 24, "RadarContactPos"),

		RecvPropInt(FIELD_PJE.OF_ARRAYINDEX(nameof(RadarContactType), 0)),
		RecvPropArray2(null!, 24, "RadarContactType"),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("PropJeepEpisodic", DT_PropJeepEpisodic).WithManualClassID(StaticClassIndices.CPropJeepEpisodic);

	public int NumRadarContacts;
	public InlineArray24<Vector3> RadarContactPos;
	public InlineArray24<int> RadarContactType;
}
