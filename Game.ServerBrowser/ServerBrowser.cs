using Source.Common.ServerBrowser;
using Source.Common.GUI;

using Steamworks;
using Source.GUI.Controls;
using Source.Common;

namespace Game.ServerBrowser;

public class ServerBrowser : IServerBrowser
{
	ServerBrowserDialog? InternetDialog;
	bool WorkshopEnabled;
	readonly List<string> WorkshopSubscribedMaps = [];
	readonly List<GameType> GameTypes = [];

	public static ServerBrowser? Instance;
	public ServerBrowser() => Instance = this;

	public void CreateDialog() {
		if (InternetDialog == null) {
			InternetDialog = new ServerBrowserDialog(null);
			InternetDialog.Initialize();
		}
	}

	readonly ILocalize Localize = Singleton<ILocalize>();
	public bool Initialize() {
		SteamAPI.Init();
		Localize.AddFile("servers/serverbrowser_%language%.txt");
		return true;
	}

	public bool PostInitialize() {
		Panel.InitializeControls();

		CreateDialog();
		InternetDialog!.SetVisible(false);
		return true;
	}

	public bool IsVACBannedFromGame(int appID) => false;
	public void SetWorkshopEnabled(bool enabled) => WorkshopEnabled = enabled;

	public void AddWorkshopSubscribedMap(ReadOnlySpan<char> mapName) {
		string map = new(mapName);
		if (!WorkshopSubscribedMaps.Contains(map))
			WorkshopSubscribedMaps.Add(map);
	}

	public void RemoveWorkshopSubscribedMap(ReadOnlySpan<char> mapName) => WorkshopSubscribedMaps.Remove(new(mapName));
	public bool IsWorkshopEnabled() => WorkshopEnabled;
	public bool IsWorkshopSubscribedMap(ReadOnlySpan<char> mapName) => WorkshopSubscribedMaps.Contains(new(mapName));

	public bool IsValid() {
		throw new NotImplementedException();
	}

	static bool FirstTimeOpening = true;
	public bool Activate() {
		if (FirstTimeOpening) {
			InternetDialog!.LoadUserData();
			FirstTimeOpening = false;
		}

		Open();
		return true;
	}

	public void Deactivate() {
		throw new NotImplementedException();
	}

	public void Reactivate() {
		throw new NotImplementedException();
	}

	private void Open() => InternetDialog!.Open();

	public IPanel? GetPanel() => InternetDialog;
	public void SetParent(IPanel parent) => InternetDialog?.SetParent(parent);

	public void Shutdown() {
		if (InternetDialog != null) {
			InternetDialog.Close();
			InternetDialog.MarkForDeletion();
		}
	}

	public bool OpenGameInfoDialog(ulong steamIDFriend, ReadOnlySpan<char> connectCode) {
		throw new NotImplementedException();
	}

	public bool JoinGame(uint gameIP, ushort gamePort, ReadOnlySpan<char> connectCode) {
		throw new NotImplementedException();
	}

	public bool JoinGame(ulong steamIDFriend, ReadOnlySpan<char> connectCode) {
		throw new NotImplementedException();
	}

	public void CloseGameInfoDialog(ulong steamIDFriend) {
		throw new NotImplementedException();
	}

	public void CloseAllGameInfoDialogs() {
		throw new NotImplementedException();
	}

	private void LoadGameTypes() {
		throw new NotImplementedException();
	}

	private void GetGameTypeName(ReadOnlySpan<char> mapName) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetMapFriendlyNameAndGameType(ReadOnlySpan<char> MapName, out ReadOnlySpan<char> friendlyMapName) {
		throw new NotImplementedException();
	}

	private class GameType(string prefix, string gameTypeName)
	{
		public string Prefix = prefix;
		public string GameTypeName = gameTypeName;
	}
}
