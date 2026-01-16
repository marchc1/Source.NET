namespace Source.Common.ServerBrowser;

public interface IServerBrowser
{
	/// <summary>
	/// activates the server browser window, brings it to the foreground
	/// </summary>
	bool Activate();

	/// <summary>
	/// joins a game directly
	/// </summary>
	bool JoinGame(uint gameIP, ushort gamePort, ReadOnlySpan<char> connectCode);

	/// <summary>
	/// joins a specified game - game info dialog will only be opened if the server is fully or passworded
	/// </summary>
	bool JoinGame(ulong steamIDFriend, ReadOnlySpan<char> connectCode);

	/// <summary>
	/// opens a game info dialog to watch the specified server; associated with the friend 'userName'
	/// </summary>
	bool OpenGameInfoDialog(ulong steamIDFriend, ReadOnlySpan<char> connectCode);

	/// <summary>
	/// forces the game info dialog closed
	/// </summary>
	void CloseGameInfoDialog(ulong steamIDFriend);

	/// <summary>
	/// closes all the game info dialogs
	/// </summary>
	void CloseAllGameInfoDialogs();

	/// <summary>
	/// Given a map name, strips off some stuff and returns the "friendly" name of the map.
	/// Returns the cleaned out map name into the caller's buffer, and returns the friendly
	/// game type name.
	/// </summary>
	ReadOnlySpan<char> GetMapFriendlyNameAndGameType(ReadOnlySpan<char> mapName, out ReadOnlySpan<char> friendlyMapName);

	/// <summary>
	/// Enable filtering of workshop maps, requires the game/tool loading us to feed subscription data.
	/// </summary>
	void SetWorkshopEnabled(bool managed);
	void AddWorkshopSubscribedMap(ReadOnlySpan<char> mapName);
	void RemoveWorkshopSubscribedMap(ReadOnlySpan<char> mapName);
}