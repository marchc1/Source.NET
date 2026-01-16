using Steamworks;

namespace Source.Common.ServerBrowser;

public struct ServerDisplay
{
	public int ListID;
	public int ServerID;
	public bool DoNotRefresh;

	public ServerDisplay() {
		ListID = -1;
		ServerID = -1;
		DoNotRefresh = true;
	}

	public override int GetHashCode() => ServerID;
	public override bool Equals(object? obj) => obj is ServerDisplay other && other.ServerID == ServerID;
}

public interface IGameList
{
	public enum InterfaceItem
	{
		Filters,
		GetNewList,
		AddServer,
		AddCurrentServer
	}

	bool SupportsItem(InterfaceItem item);

	void StartRefresh();

	void GetNewServerList();

	void StopRefresh();

	bool IsRefreshing();

	gameserveritem_t GetServer(uint serverID);

	void OnBeginConnect();

	int GetInvalidServerListID();

	ReadOnlySpan<char> GetConnectCode();
}