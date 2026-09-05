global using static Game.Server.WorldGlobals;

using Game.Shared;

using Source.Common;
using Source.Common.Commands;
using Source.Engine;

using System.Numerics;

namespace Game.Server;

using FIELD = Source.FIELD<World>;


public static class WorldGlobals
{
	public static bool g_fGameOver = false;
	public static World? GetWorldEntity() => World.g_WorldEntity;
}

[LinkEntityToClass("worldspawn")]
public class World : BaseEntity
{
	public static World? g_WorldEntity { get; private set; }

	public static SendTable DT_World = new([
		SendPropDataTable("baseclass", DT_BaseEntity),

		SendPropVector(FIELD.OF(nameof(WorldMins)), -1, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(WorldMaxs)), -1, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(StartDark)), 1, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(MaxOccludeeArea)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MinOccluderArea)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MaxPropScreenSpaceWidth)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MinPropScreenSpaceWidth)), 0, PropFlags.NoScale),
		SendPropStringT(FIELD.OF(nameof(DetailSpriteMaterial))),
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

		ConVarRef stepsize = new("sv_stepsize");
		stepsize.SetValue(18);

		// ConVarRef roomtype = new("room_type");
		// roomtype.SetValue(0);

		Assert(g_pGameRules == null);

		InstallGameRules();
		Assert(g_pGameRules != null);
		g_pGameRules.Init();

		BaseEntity.SetAllowPrecache(true);
		IGameSystem.LevelInitPreEntityAllSystems(GetModelName());

		g_pGameRules.CreateStandardEntities();

		ActivityList.Free();
		ActivityList.RegisterSharedActivities();

		EventList.Free();
		EventList.RegisterSharedEvents();

		// InitBodyQue();

		// SENTENCEG_Init();
		// PrecacheStandardParticleSystems();

		BaseCombatWeapon.W_Precache();
		GameServerClientMethods.ClientPrecache();
		g_pGameRules.Precache();
		BaseTempEntity.PrecacheTempEnts();

		for (int i = 0; i < g_DefaultLightstyles.Length; i++) 
			engine.LightStyle(i, GetDefaultLightstyleString(i));
		// styles 32-62 are assigned by the light program for switchable lights

		// 63 testing
		engine.LightStyle(63, "a");

		// AI_NetworkManager.InitializeAINetworks();
		// g_AI_SchedulesManager.LoadAllSchedules();
		// g_pGameRules.InitDefaultAIRelationships();

		// BaseCombatCharacter.InitInteractionSystem();

		PrecacheRegister.Precache();
	}

	public static ReadOnlySpan<char> GetDefaultLightstyleString(int styleIndex) => styleIndex < g_DefaultLightstyles.Length ? g_DefaultLightstyles[styleIndex] : "m";
	static readonly string[] g_DefaultLightstyles =	[
		// 0 normal
		"m",
		// 1 FLICKER (first variety)
		"mmnmmommommnonmmonqnmmo",
		// 2 SLOW STRONG PULSE
		"abcdefghijklmnopqrstuvwxyzyxwvutsrqponmlkjihgfedcba",
		// 3 CANDLE (first variety)
		"mmmmmaaaaammmmmaaaaaabcdefgabcdefg",
		// 4 FAST STROBE
		"mamamamamama",
		// 5 GENTLE PULSE 1
		"jklmnopqrstuvwxyzyxwvutsrqponmlkj",
		// 6 FLICKER (second variety)
		"nmonqnmomnmomomno",
		// 7 CANDLE (second variety)
		"mmmaaaabcdefgmmmmaaaammmaamm",
		// 8 CANDLE (third variety)
		"mmmaaammmaaammmabcdefaaaammmmabcdefmmmaaaa",
		// 9 SLOW STROBE (fourth variety)
		"aaaaaaaazzzzzzzz",
		// 10 FLUORESCENT FLICKER
		"mmamammmmammamamaaamammma",
		// 11 SLOW PULSE NOT FADE TO BLACK
		"abcdefghijklmnopqrrqponmlkjihgfedcba",
		// 12 UNDERWATER LIGHT MUTATION
		// this light only distorts the lightmap - no contribution
		// is made to the brightness of affected surfaces
		"mmnnmmnnnmmnn",
	];

	public override void Spawn() {
		SetLocalOrigin(vec3_origin);
		SetLocalAngles(vec3_angle);
		SetModelIndex(1);
		SetModelName(modelinfo.GetModelName(modelinfo.GetModel(GetModelIndex())));
		AddFlag(Source.EntityFlags.WorldBrush);

		// EventQueue.Init();
		Precache();

		GlobalEntity.Add("is_console", gpGlobals.MapName, (IsConsole()) ? GlobalEState.On : GlobalEState.Off);
		GlobalEntity.Add("is_pc", gpGlobals.MapName, (!IsConsole()) ? GlobalEState.On : GlobalEState.Off);
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
