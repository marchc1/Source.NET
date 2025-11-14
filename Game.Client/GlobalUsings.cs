global using EHANDLE = Game.Shared.Handle<Game.Client.C_BaseEntity>;
global using static Game.Client.ClientGlobals;
using Source.Common;
using Source.Common.Client;
using Source.Engine;
using Game.Client.HUD;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.DataCache;

namespace Game.Client;

public ref struct C_BaseEntityIterator {
	public C_BaseEntityIterator() {
		Restart();
	}
	public void Restart() {
		CurBaseEntity = cl_entitylist.BaseEntities.First;
	}

	public C_BaseEntity? Next() {
		while (CurBaseEntity != null) {
			C_BaseEntity pRet = CurBaseEntity.Value;
			CurBaseEntity = CurBaseEntity.Next;

			if (!pRet.IsDormant())
				return pRet;
		}

		return null;
	}

	private LinkedListNode<C_BaseEntity>? CurBaseEntity;
}

public static class ClientGlobals
{
	public static ClientGlobalVariables gpGlobals { get; private set; } = null!;
	public static IRenderView render { get; private set; } = null!;
	public static IEngineClient engine { get; private set; } = null!;
	public static IEngineVGui enginevgui { get; private set; } = null!;
	public static IClientMode clientMode { get; set; } = null!;
	public static Hud gHUD { get; private set; } = null!;
	public static Prediction prediction { get; private set; } = null!;
	public static ICvar cvar { get; private set; } = null!;
	public static ClientEntityList cl_entitylist { get; private set; } = null!;
	public static IModelInfoClient modelinfo { get; private set; } = null!;
	public static IDataCache datacache { get; private set; } = null!;
	public static IMDLCache mdlcache { get; private set; } = null!;
	public static IModelRender modelrender { get; private set; } = null!;
	public static IClientLeafSystem clientLeafSystem { get; private set; } = null!;

	public static TimeUnit_t TICK_INTERVAL => gpGlobals.IntervalPerTick;

	public static TimeUnit_t TICKS_TO_TIME(int t) => TICK_INTERVAL * t;
	public static int TIME_TO_TICKS(TimeUnit_t dt) => (int)(0.5f + dt / TICK_INTERVAL);


	/// <summary>
	/// Sets client globals for the client state.
	/// </summary>
	public static void InitClientGlobals() {
		gpGlobals = Singleton<ClientGlobalVariables>();
		engine = Singleton<IEngineClient>();
		enginevgui = Singleton<IEngineVGui>();
		cvar = Singleton<ICvar>();
		cl_entitylist = Singleton<ClientEntityList>();
		render = Singleton<IRenderView>();
		gHUD = Singleton<Hud>();
		prediction = (Prediction)Singleton<IPrediction>();

		modelinfo = Singleton<IModelInfoClient>();
		datacache = Singleton<IDataCache>();
		mdlcache = Singleton<IMDLCache>();
		modelrender = Singleton<IModelRender>();
		clientLeafSystem = Singleton<ClientLeafSystem>();
	}

	public static bool IsEngineThreaded() => false; 
}
