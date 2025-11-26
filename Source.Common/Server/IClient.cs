
using Source.Common.Networking;

namespace Source.Common.Server;

/// <summary>
/// A server-side client.
/// </summary>
public interface IClient
{
	void Connect(ReadOnlySpan<char> name, int userID, INetChannel netChannel, bool fakePlayer, int clientChallenge );
	// set the client in a pending state waiting for a new game
	void Inactivate();
	// Reconnect without dropiing the netchannel
	void Reconnect();		
	// disconnects a client with a given reason
	void Disconnect(ReadOnlySpan<char> reason);
	int GetPlayerSlot(); // returns client slot (usually entity number-1)
	int GetUserID(); // unique ID on this server 
	USERID GetNetworkID(); // network wide ID
	ReadOnlySpan<char> GetClientName();  // returns client name
	INetChannel? GetNetChannel(); // returns client netchannel
	IServer? GetServer(); // returns the object server the client belongs to
	ReadOnlySpan<char> GetUserSetting(ReadOnlySpan<char> cvar); // returns a clients FCVAR_USERINFO setting
	ReadOnlySpan<char> GetNetworkIDString(); // returns a human readable representation of the network id
	// set/get client data rate in bytes/second
	void SetRate(int nRate, bool bForce);
	int GetRate();
	// set/get updates/second rate
	void SetUpdateRate(int nUpdateRate, bool bForce);
	int GetUpdateRate();
	// clear complete object & free all memory 
	void Clear();
	// returns the highest world tick number acknowledge by client
	int GetMaxAckTickCount();

	// execute a client command
	bool ExecuteStringCommand( ReadOnlySpan<char> s );
	// send client a network message
	bool SendNetMsg(INetMessage msg, bool bForceReliable = false);
	// send client a text message
	void ClientPrintf(ReadOnlySpan<char> fmt);
	// client has established network channels, nothing else
	bool IsConnected();
	// client is downloading signon data
	bool IsSpawned();
	// client active is ingame, receiving snapshots
	bool IsActive();
	// returns true, if client is not a real player
	bool IsFakeClient();
	// returns true, if client is a HLTV proxy
	bool IsHLTV();
	// returns true, if client hears this player
	bool IsHearingClient(int index);
	// returns true, if client hears this player by proximity
	bool IsProximityHearingClient(int index);
	void SetMaxRoutablePayloadSize(int nMaxRoutablePayloadSize);
}
