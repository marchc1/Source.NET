using Source.Common.Bitbuffers;
using Source.Common.Client;
using Source.Common.Engine;
using Source.Common.Networking;
using Source.Common.Server;
using Source.Common.Utilities;

namespace Source.Engine.Server;

/// <summary>
/// Base server, in SERVER. Often referred to by 'sv'
/// </summary>
public class GameServer : BaseServer
{
	public override void SetMaxClients(int number) {
		MaxClients = Math.Clamp(number, 1, MaxClientsLimit);
		Host.deathmatch.SetValue(MaxClients > 1);
	}
	public override void BroadcastMessage(INetMessage msg, bool onlyActive = false, bool reliable = false) {
		throw new NotImplementedException();
	}

	public override void DisconnectClient(IClient client, ReadOnlySpan<char> reason) {
		throw new NotImplementedException();
	}

	public override ReadOnlySpan<char> GetName() {
		throw new NotImplementedException();
	}

	public override void GetNetStats(out double avgIn, out double avgOut) {
		throw new NotImplementedException();
	}

	public override int GetNumClients() {
		throw new NotImplementedException();
	}

	public override int GetNumFakeClients() {
		throw new NotImplementedException();
	}

	public override int GetNumPlayers() {
		throw new NotImplementedException();
	}

	public override int GetNumProxies() {
		throw new NotImplementedException();
	}

	public override ReadOnlySpan<char> GetPassword() {
		throw new NotImplementedException();
	}

	public override bool GetPlayerInfo(int clientIndex, out PlayerInfo pinfo) {
		throw new NotImplementedException();
	}

	public override double GetTime() {
		throw new NotImplementedException();
	}

	public override bool ProcessConnectionlessPacket(NetPacket packet) {
		throw new NotImplementedException();
	}

	public override void SetPassword(ReadOnlySpan<char> password) {
		throw new NotImplementedException();
	}

	public override void SetPaused(bool paused) {
		throw new NotImplementedException();
	}

	public override void Init(bool dedicated) {

	}

	public override void Shutdown() {


	}

	internal bool IsLevelMainMenuBackground() {
		return LevelMainMenuBackground;
	}

	public bool LoadGame;           // handle connections specially

	public InlineArray64<char> Startspot;

	public int NumEdicts;
	public int MaxEdicts;
	public int FreeEdicts;
	Edict[]? edicts;       

	public int MaxClientsLimit;    // Max allowed on server.

	public bool AllowSignOnWrites;
	public bool DLLInitialized;    // Have we loaded the game dll.

	public bool LevelMainMenuBackground;  // true if the level running only as the background to the main menu

	public readonly List<EventInfo> TempEntities = [];     // temp entities

	public readonly bf_write FullSendTables = new();
	public readonly UtlMemory<byte> FullSendTablesBuffer = new();

	public bool LoadedPlugins;
}
