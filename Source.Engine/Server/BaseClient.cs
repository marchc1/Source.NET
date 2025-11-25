using Source.Common.Networking;
using Source.Common.Server;

using Steamworks;

namespace Source.Engine.Server;


public abstract class BaseClient : IClient, IClientMessageHandler, IDisposable {
	public int GetPlayerSlot() => ClientSlot;
	public int GetUserID() => UserID;
	// NetworkID?
	public string GetClientName() => Name;
	public INetChannel GetNetChannel() => NetChannel;

	public void Dispose() {

	}

	internal void Clear() {
		throw new NotImplementedException();
	}

	public bool IsConnected() {
		throw new NotImplementedException();
	}

	public void Disconnect(ReadOnlySpan<char> v) {
		throw new NotImplementedException();
	}

	public bool IsActive() {
		throw new NotImplementedException();
	}

	public bool IsSpawned() {
		throw new NotImplementedException();
	}

	public bool IsFakeClient() {
		throw new NotImplementedException();
	}

	public bool IsHLTV() {
		throw new NotImplementedException();
	}

	internal bool SendNetMsg(INetMessage msg, bool reliable) {
		throw new NotImplementedException();
	}

	public int ClientSlot;
	public int EntityIndex;
	public int UserID;

	public string Name;
	public string GUID;

	public CSteamID SteamID;
	public uint FriendsID;
	public string FriendsName;

	// convars...
	public bool SendServerInfo;
	public BaseServer Server;
	public bool HLTV;
	public bool Replay;
	public int ClientChallenge;
	
	public uint SendTableCRC;

	public CustomFile[] CustomFiles;
	public int FilesDownloaded;

	public INetChannel NetChannel;
	public SignOnState SignOnState;
	public int DeltaTick;
	public int StringTableAckTick;
	public int SignOnTick;
	// CSmartPtr<CFrameSnapshot, CRefCountAccessorLongName>
	// CFrameSnapshot baseline
	int BaselineUpdateTick;
	MaxEdictsBitVec BaselinesSent;
	public int BaselineUsed;
}
