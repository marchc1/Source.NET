using Source.Common;
using Source.Common.Client;
using Source.Engine;
using Game.Client.HUD;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.DataCache;
using Source;
using Source.Common.MaterialSystem;
using Source.Common.GUI;

namespace Game.Client;

public static class SourceDllMain
{
	[Dependency] public static ClientGlobalVariables gpGlobals { get; private set; } = null!;
	[Dependency] public static IViewRender view { get; private set; } = null!;
	[Dependency] public static IRenderView render { get; private set; } = null!;
	[Dependency] public static IEngineClient engine { get; private set; } = null!;
	[Dependency] public static ISurface surface { get; private set; } = null!;
	[Dependency] public static IEngineVGui enginevgui { get; private set; } = null!;
	[Dependency] public static Hud gHUD { get; private set; } = null!;
	[Dependency<IPrediction>] public static Prediction prediction { get; private set; } = null!;
	[Dependency] public static ICvar cvar { get; private set; } = null!;
	[Dependency] public static ClientEntityList cl_entitylist { get; private set; } = null!;
	[Dependency] public static IModelInfoClient modelinfo { get; private set; } = null!;
	[Dependency] public static IGameEventManager2 gameeventmanager { get; private set; } = null!;
	[Dependency] public static IDataCache datacache { get; private set; } = null!;
	[Dependency] public static IMDLCache mdlcache { get; private set; } = null!;
	[Dependency] public static ICenterPrint centerprint { get; private set; } = null!;
	[Dependency] public static IMaterialSystem materials { get; private set; } = null!;
	[Dependency] public static IModelRender modelrender { get; private set; } = null!;
	[Dependency] public static IClientLeafSystem clientLeafSystem { get; private set; } = null!;
	[Dependency] public static IUniformRandomStream random { get; private set; } = null!;
	[Dependency] public static ILocalize localize { get; private set; } = null!;

	public static TimeUnit_t TICK_INTERVAL => gpGlobals.IntervalPerTick;

	public static TimeUnit_t TICKS_TO_TIME(int t) => TICK_INTERVAL * t;
	public static int TIME_TO_TICKS(TimeUnit_t dt) => (int)(0.5f + dt / TICK_INTERVAL);

	public static bool IsEngineThreaded() => false;

	// TODO stubs
	public static bool IsInCommentaryMode() => false;
}
