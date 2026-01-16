using Microsoft.Extensions.DependencyInjection;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.ServerBrowser;

namespace Game.ServerBrowser;

[EngineComponent]
public static class SourceDllMain
{
	public static void Link(IServiceCollection services) {
		services.AddSingleton<IServerBrowser, ServerBrowser>();
	}

	[Dependency] public static IFileSystem filesystem { get; private set; } = null!;
	[Dependency] public static ICvar cvar { get; private set; } = null!;
}