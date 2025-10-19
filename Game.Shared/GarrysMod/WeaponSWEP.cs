#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
using Game.Shared;
using Source;

#if CLIENT_DLL
namespace Game.Client;
using FIELD = Source.FIELD<C_WeaponSWEP>;
#else
namespace Game.Server;
using FIELD = Source.FIELD<WeaponSWEP>;
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
    C_WeaponSWEP : C_BaseHL2MPCombatWeapon
#else
	WeaponSWEP : BaseHL2MPCombatWeapon
#endif
{
	public static readonly Table DT_WeaponSWEP = new(DT_BaseHL2MPCombatWeapon, [
#if CLIENT_DLL
		RecvPropDataTable("ScriptedEntity", DT_ScriptedEntity),
		RecvPropString(FIELD.OF(nameof(HoldType)))
#elif GAME_DLL
		SendPropDataTable("ScriptedEntity", DT_ScriptedEntity),
		SendPropString(FIELD.OF(nameof(HoldType)))
#endif
	]);

	public static readonly new Class
#if CLIENT_DLL
		ClientClass
#else
		ServerClass
#endif
		= new Class("WeaponSWEP", DT_WeaponSWEP).WithManualClassID(StaticClassIndices.CWeaponSWEP);

	public InlineArray64<char> HoldType;

}
#endif
