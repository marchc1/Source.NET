#if CLIENT_DLL || GAME_DLL
#if CLIENT_DLL
global using static Game.Client.GarrysMod.HL2MP_GameRules_Globals;
#else
global using static Game.Server.GarrysMod.HL2MP_GameRules_Globals;
#endif
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

public static class HL2MP_GameRules_Globals
{
	static readonly AmmoDef def = new();
	static bool initted = false;

	public static float BULLET_MASS_GRAINS_TO_LB(int grains) => 0.002285f * (grains) / 16.0f;
	public static float BULLET_MASS_GRAINS_TO_KG(int grains) => lbs2kg(BULLET_MASS_GRAINS_TO_LB(grains));
	public const float BULLET_IMPULSE_EXAGGERATION = 3.5f;
	public static float BULLET_IMPULSE(int grains, float ftpersec)
		=> ((ftpersec) * 12 * BULLET_MASS_GRAINS_TO_KG(grains) * BULLET_IMPULSE_EXAGGERATION);

	public static AmmoDef GetAmmoDef() {
		if (!initted) {
			initted = true;

			def.AddAmmoType("AR2", DamageType.Bullet, AmmoTracer.LineAndWhiz, 0, 0, 60, BULLET_IMPULSE(200, 1225), 0);
			def.AddAmmoType("AR2AltFire", DamageType.Dissolve, AmmoTracer.None, 0, 0, 3, 0, 0);
			def.AddAmmoType("Pistol", DamageType.Bullet, AmmoTracer.LineAndWhiz, 0, 0, 150, BULLET_IMPULSE(200, 1225), 0);
			def.AddAmmoType("SMG1", DamageType.Bullet, AmmoTracer.LineAndWhiz, 0, 0, 225, BULLET_IMPULSE(200, 1225), 0);
			def.AddAmmoType("357", DamageType.Bullet, AmmoTracer.LineAndWhiz, 0, 0, 12, BULLET_IMPULSE(800, 5000), 0);
			def.AddAmmoType("XBowBolt", DamageType.Bullet, AmmoTracer.Line, 0, 0, 10, BULLET_IMPULSE(800, 8000), 0);
			def.AddAmmoType("Buckshot", DamageType.Bullet | DamageType.Buckshot, AmmoTracer.Line, 0, 0, 30, BULLET_IMPULSE(400, 1200), 0);
			def.AddAmmoType("RPG_Round", DamageType.Burn, AmmoTracer.None, 0, 0, 3, 0, 0);
			def.AddAmmoType("SMG1_Grenade", DamageType.Burn, AmmoTracer.None, 0, 0, 3, 0, 0);
			def.AddAmmoType("Grenade", DamageType.Burn, AmmoTracer.None, 0, 0, 5, 0, 0);
			def.AddAmmoType("slam", DamageType.Burn, AmmoTracer.None, 0, 0, 5, 0, 0);
		}

		return def;
	}
}

#endif
