global using static Game.Client.TempEntsSystemGlobals;

using Game.Shared;

namespace Game.Client;

public class TempEntsSystem : IPredictionSystem
{
}

public static class TempEntsSystemGlobals
{
	public static readonly TempEntsSystem te = new();
}
