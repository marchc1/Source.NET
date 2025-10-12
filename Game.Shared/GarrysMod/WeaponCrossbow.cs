#if CLIENT_DLL || GAME_DLL
using Game.Server;

using Source.Common;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<WeaponCrossbow>;

#if !CLIENT_DLL
public class CrossbowBolt : BaseCombatCharacter {
	public static readonly SendTable DT_CrossbowBolt = new(DT_BaseCombatCharacter, []);
	public static readonly new ServerClass ServerClass = new ServerClass("CrossbowBolt", DT_CrossbowBolt).WithManualClassID(StaticClassIndices.CCrossbowBolt);
}
#endif

public class WeaponCrossbow : BaseHL2MPCombatWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponCrossbow = new(DT_BaseHL2MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropBool(FIELD.OF(nameof(InZoom))),
			RecvPropBool(FIELD.OF(nameof(MustReload)))
#else
			SendPropBool(FIELD.OF(nameof(InZoom))),
			SendPropBool(FIELD.OF(nameof(MustReload)))
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponCrossbow", null, null, DT_WeaponCrossbow).WithManualClassID(StaticClassIndices.CWeaponCrossbow);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponCrossbow", DT_WeaponCrossbow).WithManualClassID(StaticClassIndices.CWeaponCrossbow);
#endif
	public bool InZoom;
	public bool MustReload;
}
#endif
