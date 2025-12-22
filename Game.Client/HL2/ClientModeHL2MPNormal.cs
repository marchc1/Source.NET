global using static Game.Client.HL2.ClientModeHL2MPNormal;

using Game.Client.HUD;

using Source.Common;
using Source.Common.GUI;
using Source.Engine;

namespace Game.Client.HL2;

public class HudViewport : BaseViewport {

}

public class ClientModeHL2MPNormal : ClientModeShared
{
	static ClientModeHL2MPNormal g_ClientModeNormal = null!;
	public static IClientMode GetClientModeNormal() => g_ClientModeNormal ??= new();

	public ClientModeHL2MPNormal() {
		Viewport = new HudViewport();
		Viewport.Start();
	}
}
