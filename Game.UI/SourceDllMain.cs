global using static Game.UI.SourceDllMain;
using Microsoft.Extensions.DependencyInjection;

using Source;
using Source.Common;
using Source.Common.GameUI;

using System.Runtime.CompilerServices;

namespace Game.UI;

[EngineComponent]
public static class SourceDllMain
{
	[Source.Dependency] public static IGameUIFuncs gameuifuncs = null!;
	public static void Link(IServiceCollection services) {
		services.AddSingleton<IGameConsole, GameConsole>();
	}
}
