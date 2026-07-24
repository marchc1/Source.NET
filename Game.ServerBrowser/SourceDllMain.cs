global using static Game.ServerBrowser.SourceDllMain;

using Microsoft.Extensions.DependencyInjection;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.GUI;
using Source.Common.ServerBrowser;

namespace Game.ServerBrowser;

[EngineComponent]
public static class SourceDllMain
{
	[Dependency] public static ISchemeManager SchemeManager { get; private set; } = null!;
	[Dependency] public static IVGui VGui { get; private set; } = null!;
	[Dependency] public static IVGuiInput Input { get; private set; } = null!;
	[Dependency] public static ILocalize Localize { get; private set; } = null!;
	[Dependency] public static IFileSystem fileSystem { get; private set; } = null!;

	public static void Link(IServiceCollection services) {
		services.AddSingleton<IServerBrowser, ServerBrowser>();
	}
}