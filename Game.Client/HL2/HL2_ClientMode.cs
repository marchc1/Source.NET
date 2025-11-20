global using static Game.Client.HL2.HL2_ClientMode_Globals;

using Source;
using Source.Common.Commands;

namespace Game.Client.HL2;

[EngineComponent]
public static class HL2_ClientMode_Globals
{
	public static readonly ConVar default_fov = new("75", 0);
	public static IClientMode clientMode { get; set; } = null!;
}
