#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
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
using Source;

using FIELD = Source.FIELD<GMODGameRulesProxy>;
using Game.Shared;

public class
#if CLIENT_DLL
	C_GMODGameRulesProxy
#else
	GMODGameRulesProxy
#endif
	: GameRulesProxy
{
	public override GameRules GameRules => gmod_gamerules_data;
	public GMODGameRules gmod_gamerules_data = new();
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
		SendPropInt(FIELD<GMODGameRules>.OF("SkillLevel"), 32, 0)
#endif
	]);

	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_GMODGameRulesProxy = new(DT_GameRulesProxy, [
#if CLIENT_DLL
			RecvPropDataTable(nameof(gmod_gamerules_data), FIELD.OF(nameof(gmod_gamerules_data)), DT_GMODRules, 0, DataTableRecvProxy_PointerDataTable)
#else
			SendPropDataTable(nameof(gmod_gamerules_data), DT_GMODRules)
#endif
		]);
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
	: GameRules
// TODO: AutoGameSystemPerFrame
{
	public override ReadOnlySpan<char> Name() => "GMODGameRules";

	public float TimeScale;
	public int SkillLevel;
}
#endif
