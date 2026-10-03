global using static Game.Lua.SourceDllMain;

using Microsoft.Extensions.DependencyInjection;

using Source;
using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.GarrysMod.Lua;

namespace Game.Lua;

[EngineComponent]
public static class SourceDllMain
{
	[Dependency] public static ICommandLine commandLine { get; private set; } = null!;
	[Dependency] public static ICvar cvar { get; private set; } = null!;
	[Dependency] public static IFileSystem filesystem { get; private set; } = null!;
	[Dependency] public static ILuaShared luashared { get; private set; } = null!;
	[Dependency] public static ILuaConVars luaconvars { get; private set; } = null!;

	public static void Link(IServiceCollection services) {
		services.AddSingleton<ILuaConVars, LuaConVarsImpl>();
	}
}
