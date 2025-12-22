#if CLIENT_DLL || GAME_DLL

using Game.Shared;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Shared
{
}

// Define physics methods for base entity
#if CLIENT_DLL
namespace Game.Client
#else
namespace Game.Server
#endif
{
	public partial class
#if CLIENT_DLL
		C_BaseEntity
#else
		BaseEntity
#endif
	{
		public SharedBaseEntity? GetGroundEntity() => GroundEntity.Get();
	}
}
#endif
