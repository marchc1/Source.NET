#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
#if CLIENT_DLL
global using GameRules = Game.Client.C_GameRules;
global using static Game.Client.C_GameRules;
global using GameRulesProxy = Game.Client.C_GameRulesProxy;
namespace Game.Client;
#else
global using static Game.Server.GameRules;

global using GameRules = Game.Server.GameRules;
global using GameRulesProxy = Game.Server.GameRulesProxy;
namespace Game.Server;
#endif

using Game.Shared;

using System.Numerics;

using Source.Common;
using Source.Common.Engine;
using System.Diagnostics.CodeAnalysis;

public enum GameRulesPlayerRelationship
{
	NotTeammate,
	Teammate,
	Enemy,
	Ally,
	Neutral
}

public class
#if CLIENT_DLL
	C_GameRulesProxy
#else
	GameRulesProxy
#endif
	: BaseEntity
{
	public virtual GameRules GameRules => gameRules!;
	GameRules? gameRules = null;

	public static GameRulesProxy? s_GameRulesProxy;

	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_GameRulesProxy = new([]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("GameRulesProxy", null, null, DT_GameRulesProxy).WithManualClassID(StaticClassIndices.CGameRulesProxy);
#else
#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required
	public static readonly new ServerClass ServerClass = new ServerClass("GameRulesProxy", DT_GameRulesProxy).WithManualClassID(StaticClassIndices.CGameRulesProxy);
#pragma warning restore CS0109 // Member does not hide an inherited member; new keyword is not required
#endif

}

public abstract class
#if CLIENT_DLL
	C_GameRules
#else
	GameRules
#endif
: AutoGameSystemPerFrame
{
	[NotNull] public static GameRules g_pGameRules = null!;
	public
#if CLIENT_DLL
	C_GameRules
#else
	GameRules
#endif
	() : base("GameRules") {
		g_pGameRules = this;
	}

	public static readonly ViewVectors g_DefaultViewVectors = new(
		new Vector3(0, 0, 64),          //VEC_VIEW (View)

		new Vector3(-16, -16, 0),       //VEC_HULL_MIN (HullMin)
		new Vector3(16, 16, 72),    //VEC_HULL_MAX (HullMax)

		new Vector3(-16, -16, 0),       //VEC_DUCK_HULL_MIN (DuckHullMin)
		new Vector3(16, 16, 36),    //VEC_DUCK_HULL_MAX	(DuckHullMax)
		new Vector3(0, 0, 28),          //VEC_DUCK_VIEW		(DuckView)

		new Vector3(-10, -10, -10),     //VEC_OBS_HULL_MIN	(ObsHullMin)
		new Vector3(10, 10, 10),    //VEC_OBS_HULL_MAX	(ObsHullMax)

		new Vector3(0, 0, 14)           //VEC_DEAD_VIEWHEIGHT (DeadViewHeight)
	);
	public virtual ViewVectors GetViewVectors() => g_DefaultViewVectors;

	public virtual bool SwitchToNextBestWeapon(BaseCombatCharacter? player, BaseCombatWeapon? currentWeapon) {
		return false;
	}
	public virtual BaseCombatWeapon? GetNextBestWeapon(BaseCombatCharacter? player, BaseCombatWeapon? currentWeapon) {
		return null;
	}

	public virtual ReadOnlySpan<char> GetGameDescription() => "";

	public virtual void CreateCustomNetworkStringTables() { }

	internal ReadOnlySpan<char> TranslateEffectForVisionFilter(ReadOnlySpan<char> type, ReadOnlySpan<char> soundName) {
		return soundName;
	}

#if GAME_DLL
	public ReadOnlySpan<char> GetTeamID(BaseEntity? entity) {
		if (entity == null || entity.Edict() == null)
			return "\0";
		return entity.TeamID();
	}
	public virtual GameRulesPlayerRelationship PlayerCanHearChat(BasePlayer? listener, BasePlayer? speaker) => PlayerRelationship(listener, speaker);

	public virtual GameRulesPlayerRelationship PlayerRelationship(BasePlayer? player, BasePlayer? target) {
		if (player == null || target == null || !target.IsPlayer())
			return GameRulesPlayerRelationship.NotTeammate;

		if ((GetTeamID(player)[0] != '\0') && (GetTeamID(target)[0] != '\0') && 0 == stricmp(GetTeamID(player), GetTeamID(target)))
			return GameRulesPlayerRelationship.Teammate;

		return GameRulesPlayerRelationship.NotTeammate;
	}
#endif

#if CLIENT_DLL

#else
	public static BaseEntity? GetPlayerSpawnSpot(BasePlayer player) {
		BaseEntity? spawnSpot = player.EntSelectSpawnPoint();
		Assert(spawnSpot != null);

		player.SetLocalOrigin(spawnSpot!.GetAbsOrigin() + new Vector3(0, 0, 1));
		player.SetAbsVelocity(vec3_origin);
		player.SetLocalAngles(spawnSpot.GetAbsAngles());
		player.Local.PunchAngle = vec3_angle;
		player.Local.PunchAngleVel = vec3_angle;
		// player.SnapEyeAngles(spawnSpot.GetLocalAngles());

		return spawnSpot;
	}

	public void CreateStandardEntities() {
		PlayerResource g_pPlayerResource = (PlayerResource)BaseEntity.Create("player_manager", vec3_origin, vec3_angle); // todo, should be global
		g_pPlayerResource.AddEFlags(EFL.KeepOnRecreateEntities);
	}

	internal void ClientSpawned(Edict player) { }

	internal ReadOnlySpan<char> GetChatFormat(bool teamOnly, BasePlayer? player) {
		return "";
	}

	internal ReadOnlySpan<char> GetChatPrefix(bool teamOnly, BasePlayer? player) {
		return "";
	}

	internal ReadOnlySpan<char> GetChatLocation(bool teamOnly, BasePlayer? player) {
		return "";
	}
#endif
}
#endif
