global using static Game.Server.GlobalState;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Server;

public class GlobalState
{
	static readonly GlobalState globalState = new();

	public static void ResetGlobalState() {

	}
}
