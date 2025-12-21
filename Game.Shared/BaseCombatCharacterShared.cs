#if CLIENT_DLL || GAME_DLL

using Source.Common;
using Game.Shared;

#if CLIENT_DLL
namespace Game.Client;
#else
namespace Game.Server;
#endif

public partial class
#if CLIENT_DLL
		C_BaseCombatCharacter
#elif GAME_DLL
	BaseCombatCharacter
#endif
{
	public int GetAmmoCount(int ammoIndex) {
		if (ammoIndex == -1)
			return 0;

		// TODO, it is 4 am, I do not want to do this right now
		// FIXME FIX ME FIX ME
#if CLIENT_DLL
		return Ammo[ammoIndex];
#else
		return 0;
#endif
	}
}
#endif
