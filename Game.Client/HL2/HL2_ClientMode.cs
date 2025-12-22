global using static Game.Client.HL2.HL2_ClientMode_Globals;

using Source;
using Source.Common.Commands;

namespace Game.Client.HL2;

[EngineComponent]
public static class HL2_ClientMode_Globals
{
	public static readonly ConVar default_fov = new("75", 0);
	public static IClientMode clientMode { get; set; } = null!;
}

public class HLModeManager : IVModeManager {
	public static readonly HLModeManager g_HLModeManager = new();
	static HLModeManager(){
		modemanager = g_HLModeManager;
	}

	public void Init() {
		clientMode = GetClientModeNormal();
	}

	public void LevelInit(ReadOnlySpan<char> newmap) {
		clientMode.LevelInit(newmap);
	}

	public void LevelShutdown() {
		throw new NotImplementedException();
	}
}
