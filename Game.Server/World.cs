global using static Game.Server.WorldGlobals;

using Game.Shared;

using Source.Common;

using System.Numerics;

namespace Game.Server;

using FIELD = Source.FIELD<World>;


public static class WorldGlobals {
	public static bool g_fGameOver = false;
	public static World? GetWorldEntity() => World.g_WorldEntity;
}

[LinkEntityToClass("worldspawn")]
public class World : BaseEntity
{
	public static World? g_WorldEntity { get; private set; }

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

	public World() {
		AddEFlags(EFL.NoAutoEdictAttach | EFL.KeepOnRecreateEntities);
		ActivityList.Init();
		SetSolid(Source.SolidType.BSP);
		SetMoveType(Source.MoveType.None);
		ColdWorld = false;
	}

	public override void Precache() {
		g_WorldEntity = this;
		g_fGameOver = false;

		Assert(g_pGameRules == null);
		InstallGameRules();
		Assert(g_pGameRules != null);
		g_pGameRules.Init();

		IGameSystem.LevelInitPreEntityAllSystems(GetModelName());
	}

	public override void Spawn() {
		SetLocalOrigin(vec3_origin);
		SetLocalAngles(vec3_angle);
		SetModelIndex(1);
		// SetModelName todo
		AddFlag(Source.EntityFlags.WorldBrush);

		// EventQueue.Init();
		Precache();
	}

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
