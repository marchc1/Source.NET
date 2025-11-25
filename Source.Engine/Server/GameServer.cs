using Source.Common.Client;
using Source.Common.Networking;
using Source.Common.Server;

namespace Source.Engine.Server;

/// <summary>
/// Base server, in SERVER. Often referred to by 'sv'
/// </summary>
public class GameServer(Net Net, Host Host) : BaseServer(Net, Host)
{
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

	bool bIsLevelMainMenuBackground;

	internal bool IsLevelMainMenuBackground() {
		return bIsLevelMainMenuBackground;
	}
}
