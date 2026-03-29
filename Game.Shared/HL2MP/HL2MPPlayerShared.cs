#if CLIENT_DLL || GAME_DLL

#if CLIENT_DLL
global using static Game.Client.HL2MP.HL2MPPlayerSharedGlobals;

global using HL2MP_Player = Game.Client.HL2MP.C_HL2MP_Player;

#else
global using static Game.Server.HL2MP.HL2MPPlayerSharedGlobals;

global using HL2MP_Player = Game.Server.HL2MP.HL2MP_Player;

#endif
using Source.Common.Mathematics;

using Game.Shared;

using System.Numerics;

#if CLIENT_DLL
namespace Game.Client.HL2MP;

#else
namespace Game.Server.HL2MP;
#endif

using Source.Common.Commands;
using Source.Common.Physics;
using Source;
using Source.Common;

using System.Runtime.CompilerServices;

public static class HL2MPPlayerSharedGlobals
{
	public static HL2MP_Player? ToHL2MPPlayer(BaseEntity? entity) {
		if (entity == null || !entity.IsPlayer())
			return null;

		return (HL2MP_Player?)entity;
	}
}

public partial class
#if CLIENT_DLL
	C_HL2MP_Player
#elif GAME_DLL
	HL2MP_Player
#endif
{
	public new Vector3 GetAttackSpread(BaseCombatWeapon? weapon, BaseEntity? target = null){
		if (weapon != null)
			return weapon.GetBulletSpread(WeaponProficiency.Perfect);
		return VECTOR_CONE_15DEGREES;
	}
}
#endif
