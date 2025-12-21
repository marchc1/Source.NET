using Game.Shared;

using Source.Common;

using System.Numerics;

namespace Game.Server;
using FIELD = Source.FIELD<World>;


[LinkEntityToClass("worldspawn")]
public class World : BaseEntity
{
	public static SendTable DT_World = new([
		SendPropDataTable("baseclass", DT_BaseEntity),

		SendPropFloat(FIELD.OF(nameof(WaveHeight)), 8, PropFlags.RoundUp, 0, 8),
		SendPropVector(FIELD.OF(nameof(WorldMins)), -1, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(WorldMaxs)), -1, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(StartDark)), 1, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(MaxOccludeeArea)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MinOccluderArea)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MaxPropScreenSpaceWidth)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MinPropScreenSpaceWidth)), 0, PropFlags.NoScale),
		SendPropStringT(FIELD.OF(nameof(DetailSpriteMaterial))),
		SendPropInt(FIELD.OF(nameof(ColdWorld)), 1, PropFlags.Unsigned),
	]);

	public static readonly new ServerClass ServerClass = new ServerClass("World", DT_World)
																		.WithManualClassID(StaticClassIndices.CWorld);
	float WaveHeight;
	Vector3 WorldMins;
	Vector3 WorldMaxs;
	bool StartDark;
	float MaxOccludeeArea;
	float MinOccluderArea;
	float MaxPropScreenSpaceWidth;
	float MinPropScreenSpaceWidth;
	string? DetailSpriteMaterial;
	bool ColdWorld;
}
