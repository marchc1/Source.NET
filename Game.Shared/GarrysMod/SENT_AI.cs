#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
using Game.Shared;
using Source;

#if CLIENT_DLL
namespace Game.Client;
using FIELD = Source.FIELD<C_SENT_AI>;
#else
namespace Game.Server;
using FIELD = Source.FIELD<SENT_AI>;
#endif

using Table =
#if CLIENT_DLL
	RecvTable;
#else
	SendTable;
#endif

using Class =
#if CLIENT_DLL
	ClientClass;
#else
	ServerClass;
#endif

public partial class
#if CLIENT_DLL
	C_SENT_AI : C_AI_BaseNPC
#elif GAME_DLL
	SENT_AI 	: AI_BaseNPC
#endif
{
	public static readonly Table DT_SENT_AI = new(DT_AI_BaseNPC, [
#if CLIENT_DLL
		RecvPropDataTable("ScriptedEntity", DT_ScriptedEntity)
#elif GAME_DLL
		SendPropDataTable("ScriptedEntity", DT_ScriptedEntity)
#endif
	]);

	public static readonly new Class
#if CLIENT_DLL
		ClientClass
#else
		ServerClass
#endif
		= new Class("SENT_AI", DT_SENT_AI).WithManualClassID(StaticClassIndices.CSENT_AI);
}
#endif
