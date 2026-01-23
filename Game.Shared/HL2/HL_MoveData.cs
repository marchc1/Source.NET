#if CLIENT_DLL || GAME_DLL
using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Shared.HL2;

public class HLMoveData : MoveData
{
	public bool IsSprinting;
}
#endif
