global using static Game.Server.GameGlobals;

using Source.Common.Commands;

namespace Game.Server;

public static class GameGlobals
{
	public static readonly ConVar displaysoundlist = new("displaysoundlist", "0");
	public static readonly ConVar mapcyclefile = new("mapcyclefile", "mapcycle.txt", FCvar.None, "Name of the .txt file used to cycle the maps on multiplayer servers "); // todo MapCycleFileChangedCallback
	public static readonly ConVar servercfgfile = new("servercfgfile", "server.cfg");
	public static readonly ConVar lservercfgfile = new("lservercfgfile", "listenserver.cfg");

	// multiplayer server rules
	public static readonly ConVar teamplay = new("mp_teamplay", "0", FCvar.Notify);
	public static readonly ConVar falldamage = new("mp_falldamage", "0", FCvar.Notify);
	public static readonly ConVar weaponstay = new("mp_weaponstay", "0", FCvar.Notify);
	public static readonly ConVar forcerespawn = new("mp_forcerespawn", "1", FCvar.Notify);
	public static readonly ConVar footsteps = new("mp_footsteps", "1", FCvar.Notify);
#if CSTRIKE
	public static readonly ConVar flashlight= new( "mp_flashlight","1", FCvar.Notify );
#else
	public static readonly ConVar flashlight = new("mp_flashlight", "0", FCvar.Notify);
#endif
	public static readonly ConVar aimcrosshair = new("mp_autocrosshair", "1", FCvar.Notify);
	public static readonly ConVar decalfrequency = new("decalfrequency", "10", FCvar.Notify);
	public static readonly ConVar teamlist = new("mp_teamlist", "hgrunt;scientist", FCvar.Notify);
	public static readonly ConVar teamoverride = new("mp_teamoverride", "1");
	public static readonly ConVar defaultteam = new("mp_defaultteam", "0");
	public static readonly ConVar allowNPCs = new("mp_allowNPCs", "1", FCvar.Notify);
}
