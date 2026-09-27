#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
#if CLIENT_DLL
global using static Game.Client.GarrysMod.GMOD_GameRules_Globals;
#else
global using static Game.Server.GarrysMod.GMOD_GameRules_Globals;
#endif
#if CLIENT_DLL
global using GMODGameRules = Game.Client.GarrysMod.C_GMODGameRules;
global using GMODGameRulesProxy = Game.Client.GarrysMod.C_GMODGameRulesProxy;
namespace Game.Client.GarrysMod;
#else
global using GMODGameRules = Game.Server.GarrysMod.GMODGameRules;
global using GMODGameRulesProxy = Game.Server.GarrysMod.GMODGameRulesProxy;
namespace Game.Server.GarrysMod;
#endif

using Source.Common;
using Source.Common.Engine;
using Source;

using FIELD = Source.FIELD<GMODGameRulesProxy>;

using Game.Shared;
using Source.GUI.Controls;

public static class GMOD_GameRules_Globals
{
	static readonly GameRulesRegister s_GMODRulesRegister = new("CGMODRules", () => new GMODGameRules());

	public static GMODGameRules GMODRules() => (GMODGameRules)g_pGameRules;
}

#if GAME_DLL
[LinkEntityToClass("gmod_gamerules")]
#endif
public class
#if CLIENT_DLL
	C_GMODGameRulesProxy
#else
	GMODGameRulesProxy
#endif
	: GameRulesProxy
{
	public override GameRules GameRules => GMODRules();
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
	DT_GMODRules = new(nameof(DT_GMODRules), [
#if CLIENT_DLL
		RecvPropFloat(FIELD<GMODGameRules>.OF("TimeScale")),
		RecvPropInt(FIELD<GMODGameRules>.OF("SkillLevel"))
#else
		SendPropFloat(FIELD<GMODGameRules>.OF("TimeScale"), 0, PropFlags.NoScale, 0, 0),
		SendPropInt(FIELD<GMODGameRules>.OF("SkillLevel"), 4, PropFlags.Unsigned)
#endif
	]);

#if CLIENT_DLL
	public static void RecvProxy_GMODRules(RecvProp prop, out object? outInstance, object? instance, IFieldAccessor fieldInfo, int objectID) {
		GMODGameRules rules = GMODRules();
		Assert(rules != null);
		outInstance = rules;
	}

	public static readonly RecvTable DT_GMODGameRulesProxy = new(DT_GameRulesProxy, [
		RecvPropDataTable("gmod_gamerules_data", DT_GMODRules, 0, RecvProxy_GMODRules)
	]);
#else
	public static object SendProxy_GMODRules(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		GMODGameRules rules = GMODRules();
		Assert(rules != null);
		return rules;
	}

	public static readonly SendTable DT_GMODGameRulesProxy = new(DT_GameRulesProxy, [
		SendPropDataTable("gmod_gamerules_data", DT_GMODRules, SendProxy_GMODRules)
	]);
#endif
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("GMODGameRulesProxy", null, null, DT_GMODGameRulesProxy).WithManualClassID(StaticClassIndices.CGMODGameRulesProxy);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("GMODGameRulesProxy", DT_GMODGameRulesProxy).WithManualClassID(StaticClassIndices.CGMODGameRulesProxy);
#endif
}

public class
#if CLIENT_DLL
	C_GMODGameRules
#else
	GMODGameRules
#endif
	: HL2MPGameRules
// TODO: AutoGameSystemPerFrame
{
	public override ReadOnlySpan<char> Name() => "GMODGameRules";

	public float TimeScale;
	public int SkillLevel;

#if GAME_DLL
	public override bool FlPlayerFallDeathDoesScreenFade(BasePlayer player) {
		return base.FlPlayerFallDeathDoesScreenFade(player);
	}
	public override float FlPlayerFallDamage(BasePlayer player) {
		return 10; // todo: lua hook
	}
#endif
}
#endif
