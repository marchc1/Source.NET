global using static Game.UI.SourceDllMain;

using Microsoft.Extensions.DependencyInjection;

using Source;
using Source.Common;
using Source.Common.Client;
using Source.Common.Filesystem;
using Source.Common.GameUI;
using Source.Common.Input;

namespace Game.UI;

[EngineComponent]
public static class SourceDllMain
{
	[Dependency] public static IGameUIFuncs gameuifuncs = null!;
	[Dependency] public static IFileSystem g_pFileSystem { get; private set; } = null!;
	[Dependency] public static IEngineClient engine { get; private set; } = null!;
	[Dependency] public static IInputSystem inputSystem { get; private set; } = null!;
	public static void Link(IServiceCollection services) {
		services.AddSingleton<IGameConsole, GameConsole>();
	}
}
