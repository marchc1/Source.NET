global using static Game.Client.HL2.HL_Prediction;
using Game.Shared;
using Game.Shared.HL2;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Game.Client.HL2;

public static class HL_Prediction
{
	static readonly HLMoveData g_HLMoveData = new();
	public static MoveData g_pMoveData { get; set; } = g_HLMoveData;
}
