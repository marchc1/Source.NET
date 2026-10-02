#if CLIENT_DLL || GAME_DLL

#if CLIENT_DLL
global using InfoLadderDismount = Game.Client.C_InfoLadderDismount;
using Source.Common;

using Game.Shared;
namespace Game.Client;
#else
using Source.Common;

using Game.Shared;
namespace Game.Server;
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

using FIELD = Source.FIELD<InfoLadderDismount>;

[LinkEntityToClass("info_ladder_dismount")]
[NetworkName("CInfoLadderDismount")]
public partial class
#if CLIENT_DLL
	C_InfoLadderDismount: C_BaseEntity
#else
	InfoLadderDismount : BaseEntity
#endif
{
	public static Table DT_InfoLadderDismount = new(DT_BaseEntity, []);
	public static readonly Class Class = new Class(DT_InfoLadderDismount);
}

#endif
