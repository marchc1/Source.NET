#if CLIENT_DLL || GAME_DLL
global using static Game.Shared.HL2.HL2GameMovement;

using Source;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Game.Shared.HL2;

public class HL2GameMovement : GameMovement {
	static readonly HL2GameMovement gameMovement = new();
	
	public static IGameMovement g_pGameMovement {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => gameMovement;
	}
}

#endif
