using Microsoft.Extensions.DependencyInjection;

using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Engine.Client;

namespace Source.Engine;

public class BaseMod(IServiceProvider services, EngineParms host_parms, SV SV, ICommandLine commandLine, IMaterialSystem materials, IVideoMode videomode) : IMod
{
	private bool IsServerOnly(IEngineAPI api) => ((EngineAPI)api).Dedicated;

	public bool Init(string initialMod, string initialGame) {
		host_parms.Mod = initialMod;
		host_parms.Game = initialGame;

		if (cl != null) {
			cl.RestrictServerCommands = false;
			cl.RestrictClientCommands = false;
		}

		MaterialSystem_Config config = materials.GetCurrentConfigForVideoCard();
		int width = config.VideoMode.Width;
		int height = config.VideoMode.Height;
		bool windowed = config.Windowed();
		bool borderless = config.NoWindowBorder();

		videomode.Init();

		return videomode.CreateGameWindow(width, height, windowed, borderless);
	}

	public IMod.Result Run() {
		IMod.Result res = IMod.Result.RunOK;
		IEngine eng = services.GetRequiredService<IEngine>();
		IEngineAPI engineAPI = services.GetRequiredService<IEngineAPI>();

		if (IsServerOnly(engineAPI)) {
			if (eng.Load(true, host_parms.BaseDir)) {
				// Dedicated stuff one day?
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

	}
}
