namespace Game.Server;
public partial class BaseCombatWeapon : BaseAnimating
{
	public override bool IsWeapon() => true;
	public override GarrysMod.LuaClass Lua_GetLuaClass() => GarrysMod.LuaEntity.LC_Weapon;

	public static void W_Precache(){

	}
}
