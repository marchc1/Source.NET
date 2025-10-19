#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
#if CLIENT_DLL
global using GameRules = Game.Client.C_GameRules;
global using GameRulesProxy = Game.Client.C_GameRulesProxy;
namespace Game.Client;
#else
global using GameRules = Game.Server.GameRules;
global using GameRulesProxy = Game.Server.GameRulesProxy;
namespace Game.Server;
#endif

using Source.Common;
using Game.Shared;

public class
#if CLIENT_DLL
	C_GameRulesProxy
#else
	GameRulesProxy
#endif
	: SharedBaseEntity
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

public class
#if CLIENT_DLL
	C_GameRules
#else
	GameRules
#endif
// TODO: AutoGameSystemPerFrame
{
	public virtual ReadOnlySpan<char> Name() => "GameRules";
}
#endif
