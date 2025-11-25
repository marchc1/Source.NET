using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.DataCache;
using Source.Common.Filesystem;
using Source.Common.Server;

namespace Game.Server;

public static class SourceDllMain
{
	[Dependency] public static IEngineServer engine { get; private set; } = null!;
	[Dependency] public static IFileSystem filesystem { get; private set; } = null!;
	[Dependency] public static ServerGlobalVariables gpGlobals { get; private set; } = null!;
	[Dependency] public static ICvar cvar { get; private set; } = null!;
	[Dependency] public static IUniformRandomStream random { get; private set; } = null!;
	[Dependency] public static IGameEventManager2 gameeventmanager { get; private set; } = null!;
	[Dependency] public static IDataCache datacache { get; private set; } = null!;

	public static TimeUnit_t TICK_INTERVAL => gpGlobals.IntervalPerTick;

	public static TimeUnit_t TICKS_TO_TIME(int t) => TICK_INTERVAL * t;
	public static int TIME_TO_TICKS(TimeUnit_t dt) => (int)(0.5f + dt / TICK_INTERVAL);

	public static bool IsEngineThreaded() => false;
}
