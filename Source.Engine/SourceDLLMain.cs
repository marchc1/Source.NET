global using static Source.Engine.SourceDllMain;

using Source.Common;
using Source.Common.Client;
using Source.Common.Server;
using Source.Engine.Client;
using Source.Engine.Server;

namespace Source.Engine;

// Going to be switching to this style of singleton access shortly. It's starting to get a little unmaintainable, given how many things have to cross wires,
// to constantly be polling for dependencies
public static class SourceDllMain
{
	[Dependency] public static IGameEventManager2 gameEventManager { get; private set; } = null!;
	public static GameEventManager g_GameEventManager => (GameEventManager)gameEventManager;
	[Dependency] public static ClientState cl { get; private set; } = null!;
	[Dependency] public static GameServer sv { get; private set; } = null!;
	[Dependency(Required = false)] public static IBaseClientDLL? g_ClientDLL { get; private set; } = null!;
	[Dependency] public static IServerGameDLL serverGameDLL { get; private set; } = null!;
	[Dependency] public static IClientEntityList entitylist { get; private set; } = null!;
	[Dependency] public static CommonHostState host_state { get; private set; } = null!;
	[Dependency] public static Render R { get; private set; } = null!;
}
