global using static Game.Lua.SourceDllMain;

using Microsoft.Extensions.DependencyInjection;

using Source;
using Source.Common.GarrysMod.Lua;

namespace Game.Lua;

[EngineComponent]
public static class SourceDllMain
{
	public static void Link(IServiceCollection services) {
		services.AddSingleton<ILuaConVars, LuaConVarsImpl>();
	}
}
