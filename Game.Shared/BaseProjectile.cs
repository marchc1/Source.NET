#if CLIENT_DLL || GAME_DLL

#if CLIENT_DLL
global using C_BaseProjectile = Game.Shared.BaseProjectile;
#endif

#if GAME_DLL
using Game.Server;
#endif

using Source.Common;

namespace Game.Shared;

using FIELD = Source.FIELD<BaseProjectile>;
public partial class BaseProjectile : BaseAnimating
{

}
#endif
