global using static Source.Engine.Server.SvClientConvars;

using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;

namespace Source.Engine.Server;

public static class SvClientConvars {
	public static readonly ConVar sv_timeout = new( "sv_timeout", "65", 0, "After this many seconds without a message from a client, the client is dropped" );
	public static readonly ConVar sv_maxrate = new( "sv_maxrate", "0", FCvar.Replicated, "Max bandwidth rate allowed on server, 0 == unlimited" );
	public static readonly ConVar sv_minrate = new( "sv_minrate", "3500", FCvar.Replicated, "Min bandwidth rate allowed on server, 0 == unlimited" );
	public static readonly ConVar sv_maxupdaterate = new( "sv_maxupdaterate", "66", FCvar.Replicated, "Maximum updates per second that the server will allow" );
	public static readonly ConVar sv_minupdaterate = new( "sv_minupdaterate", "10", FCvar.Replicated, "Minimum updates per second that the server will allow" );
	public static readonly ConVar sv_stressbots = new("sv_stressbots", "0", 0, "If set to 1, the server calculates data and fills packets to bots. Used for perf testing.");
	public static readonly ConVar sv_allowdownload = new("sv_allowdownload", "1", 0, "Allow clients to download files");
	public static readonly ConVar sv_allowupload = new("sv_allowupload", "1", 0, "Allow clients to upload customizations files");
	public static readonly ConVar sv_sendtables = new( "sv_sendtables", "0", FCvar.DevelopmentOnly, "Force full sendtable sending path." );
}

/// <summary>
/// Represents a player client in a game server
/// </summary>
public class GameClient : BaseClient {
	public GameClient(int slot, BaseServer server) {
		Clear();

		ClientSlot = slot;
		EntityIndex = slot + 1;
		Server = server;
		CurrentFrame = null;
		IsInReplayMode = false;
	}
	public bool VoiceLoopback;
	public AbsolutePlayerLimitBitVec VoiceStreams;   
	public AbsolutePlayerLimitBitVec VoiceProximity; 
	public int LastMovementTick; 
	public int SoundSequence;   
	public Edict Edict = null!;        
	public readonly List<SoundInfo> Sounds = [];          
	public Edict? ViewEntity;   
	public ClientFrame? CurrentFrame;  
	// public CheckTransmitInfo PackInfo;
	public bool IsInReplayMode;
	// public CheckTransmitInfo PrevPackInfo;     
	public MaxEdictsBitVec PrevTransmitEdict;
}
