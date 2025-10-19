#if CLIENT_DLL || GAME_DLL
#if CLIENT_DLL
global using HL2MPGameRules = Game.Client.GarrysMod.C_HL2MPGameRules;
global using HL2MPGameRulesProxy = Game.Client.GarrysMod.C_HL2MPGameRulesProxy;
namespace Game.Client.GarrysMod;
#else
global using HL2MPGameRules = Game.Server.GarrysMod.HL2MPGameRules;
global using HL2MPGameRulesProxy = Game.Server.GarrysMod.HL2MPGameRulesProxy;
namespace Game.Server.GarrysMod;
#endif

using Source.Common;
using Source;

using FIELD = Source.FIELD<HL2MPGameRulesProxy>;
using Game.Shared;

public class
#if CLIENT_DLL
	C_HL2MPGameRulesProxy
#else
	HL2MPGameRulesProxy
#endif
	: GameRulesProxy
{
	public override GameRules GameRules => hl2mp_gamerules_data;
	public HL2MPGameRules hl2mp_gamerules_data = new();
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
	DT_HL2MPGameRules = new(nameof(DT_HL2MPGameRules), [
#if CLIENT_DLL
		RecvPropBool(FIELD<HL2MPGameRules>.OF("TeamPlayEnabled")),
#else
		SendPropBool(FIELD<HL2MPGameRules>.OF("TeamPlayEnabled"))
#endif
	]);

	public static readonly
	#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_HL2MPGameRulesProxy = new(DT_GameRulesProxy, [
#if CLIENT_DLL
			RecvPropDataTable(nameof(hl2mp_gamerules_data), FIELD.OF(nameof(hl2mp_gamerules_data)), DT_HL2MPGameRules, 0, DataTableRecvProxy_PointerDataTable)
#else
			SendPropDataTable(nameof(hl2mp_gamerules_data), DT_HL2MPGameRules)
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("HL2MPGameRulesProxy", null, null, DT_HL2MPGameRulesProxy).WithManualClassID(StaticClassIndices.CHL2MPGameRulesProxy);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("HL2MPGameRulesProxy", DT_HL2MPGameRulesProxy).WithManualClassID(StaticClassIndices.CHL2MPGameRulesProxy);
#endif
}

public class
#if CLIENT_DLL
	C_HL2MPGameRules
#else
	HL2MPGameRules
#endif
	: GameRules
// TODO: AutoGameSystemPerFrame
{
	public override ReadOnlySpan<char> Name() => "HL2MPGameRules";
	public bool TeamPlayEnabled;
}
#endif
