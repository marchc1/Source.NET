using Source.Common;
using Source.Common.Engine;

namespace Source.Engine.Server;

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
