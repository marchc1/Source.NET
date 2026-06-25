using Microsoft.Extensions.DependencyInjection;

using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Input;
using Source.Common.Launcher;
using Source.Common.MaterialSystem;

using System.Reflection;

using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Source.Engine;

public class DedicatedServerAPI(IGame game, IServiceProvider services, Common COM, Sys Sys, EngineParms host_parms, SV SV) : IDedicatedServerAPI
{
	public List<MemberInfo> __INTERNAL_FilledDependencies { get; set; } = [];
	public object? GetService(Type serviceType) => services.GetService(serviceType);
	public object? GetKeyedService(Type serviceType, object? key) => ((IKeyedServiceProvider)services).GetKeyedService(serviceType, key);
	public object GetRequiredKeyedService(Type serviceType, object? key) => ((IKeyedServiceProvider)services).GetRequiredKeyedService(serviceType, key);

	static string GetModDirFromPath(string path) {
		int slash = path.LastIndexOf('\\');
		if (slash == -1) slash = path.LastIndexOf('/');
		return slash != -1 ? path.Substring(slash + 1) : path;
	}

	Mod? dedicatedServer = null;
	public bool ModInit(in StartupInfo info) {
		ConVar.Register();
		IEngineAPI engineAPI = services.GetRequiredService<IEngineAPI>();
		IEngine eng = services.GetRequiredService<IEngine>();
		eng.SetQuitting(IEngine.Quit.NotQuitting);

		host_parms.BaseDir = info.BaseDirectory;
		host_parms.Mod = GetModDirFromPath(info.InitialMod);
		host_parms.Game = info.InitialGame;

		Sys.TextMode = info.TextMode;

		materials.ModInit();

		dedicatedServer = new Mod(true, engineAPI);
		dedicatedServer.Main();
		return true;
	}

	public void ModShutdown() {
		if (dedicatedServer != null) {
			dedicatedServer = null;
		}
		IEngine eng = services.GetRequiredService<IEngine>();

		eng.Unload();
		materials.ModShutdown();
	}

	public bool RunFrame() {
		throw new NotImplementedException();
	}

	public void AddConsoleText(ReadOnlySpan<char> text) {
		throw new NotImplementedException();
	}

	public void UpdateStatus(out float fps, out int active, out int maxPlayers, Span<char> map) {
		throw new NotImplementedException();
	}

	public void UpdateHostname(Span<char> hostname) {
		throw new NotImplementedException();
	}
}
