using Microsoft.Extensions.DependencyInjection;

using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Engine.Client;

namespace Source.Engine;

/*
public class BaseMod(IServiceProvider services, EngineParms host_parms, SV SV, IMaterialSystem materials) : IMod
{
	IVideoMode videomode = Singleton<IVideoMode>()!;

	private bool IsServerOnly(IEngineAPI api) => ((EngineAPI)api).Dedicated;

	public bool InitRegistry(ReadOnlySpan<char> modName) {
		Span<char> regSubPath = stackalloc char[260];
		// NOTE: I decided to prefix the path here with sdn_ so we don't touch config for the actual hl2 etc -Callum
		int n = sprintf(regSubPath, "%s\\sdn_%s").S("Source").S(modName);
		return registry.Init(regSubPath[..n]);
	}

	public void ShutdownRegistry() => registry.Shutdown();

	public bool Init(string initialMod, string initialGame) {
		host_parms.Mod = initialMod;
		host_parms.Game = initialGame;

		if (cl != null) {
			cl.RestrictServerCommands = false;
			cl.RestrictClientCommands = false;
		}

		InitRegistry(initialMod);
		
		MaterialSystem_Config config = materials.GetCurrentConfigForVideoCard();
		int width = config.VideoMode.Width;
		int height = config.VideoMode.Height;
		bool windowed = config.Windowed();
		bool borderless = config.NoWindowBorder();

		if (videomode == null)
			return false;

		videomode.Init();
		return videomode.CreateGameWindow(new(width, height, windowed, borderless));
	}

	public IMod.Result Run() {
		IMod.Result res = IMod.Result.RunOK;
		IEngine eng = services.GetRequiredService<IEngine>();
		IEngineAPI engineAPI = services.GetRequiredService<IEngineAPI>();

		if (IsServerOnly(engineAPI)) {
			if (eng.Load(true, host_parms.BaseDir)) {
				// Dedicated stuff one day?
				Msg("Congrats, dedicated can boot...");
			}
		}
		else {
			eng.SetQuitting(IEngine.Quit.NotQuitting);

			if (eng.Load(false, host_parms.BaseDir)) {
#if !SWDS
				if (engineAPI.MainLoop())
					res = IMod.Result.RunRestart;

				eng.Unload();
#endif

				SV.ShutdownGameDLL();
			}
		}

		return res;
	}

	public void Shutdown() {
		host_parms.Mod = null!;
		host_parms.Game = null!;
		Singleton<IGame>().InputDetachFromGameWindow();
		ShutdownRegistry();
	}
}
*/
