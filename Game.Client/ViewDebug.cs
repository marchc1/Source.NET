using Source.Common;
using Source.Common.Commands;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Client;

public static class DebugViewRender
{
	public static readonly ConVar cl_drawshadowtexture = new("cl_drawshadowtexture", "0", FCvar.Cheat);
	public static readonly ConVar cl_shadowtextureoverlaysize = new("cl_shadowtextureoverlaysize", "256", FCvar.Cheat);

	public static void Draw3DDebuggingInfo(in ViewSetup view) {
		render.Draw3DDebugOverlays();
	}
	public static void Draw2DDebuggingInfo(in ViewSetup view) {

	}
	public static void GenerateOverdrawForTesting() {

	}
}
