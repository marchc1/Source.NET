global using static Game.Client.EffectsClient;

using Source.Common.Commands;

namespace Game.Client;

public static class EffectsClient
{
	public static readonly ConVar r_decals = new("r_decals", "2048");
}
