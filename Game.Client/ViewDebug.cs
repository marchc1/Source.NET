using Source.Common;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Client;

public static class DebugViewRender
{
	public static void Draw3DDebuggingInfo(in ViewSetup view) {
		render.Draw3DDebugOverlays();
	}
	public static void Draw2DDebuggingInfo(in ViewSetup view) {

	}
	public static void GenerateOverdrawForTesting() {

	}
}
