global using static Game.Client.GarrysMod.GarrysModSingletons;

using Microsoft.Extensions.DependencyInjection;

using Source.Common.MaterialSystem;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Client.GarrysMod;

public static class GarrysModSingletons{
	public static readonly GarrysMod garrysmod = new();
}

public class GarrysMod
{
	public void DLLInit(IServiceCollection services) {

	}

	public void InitializeMod(IServiceProvider services){

	}
}
