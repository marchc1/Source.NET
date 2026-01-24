using Source.Common.ServerBrowser;
using Source.Common.GUI;

using Steamworks;
using Source.GUI.Controls;
using Source.Common;
using Source.Common.Formats.Keyvalues;
using Source.Common.Filesystem;

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
	readonly IFileSystem fileSystem = Singleton<IFileSystem>();

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
		if (GameTypes.Count > 0)
			return;

		const string GAMETYPES_FILE = "servers/ServerBrowserGameTypes.txt";

		KeyValues kvs = new();
		if (!kvs.LoadFromFile(fileSystem, GAMETYPES_FILE, "MOD"))
			return;

		GameTypes.Clear();

		for (KeyValues? sub = kvs.GetFirstSubKey(); sub != null; sub = sub.GetNextKey()) {
			GameType gt = new(
				prefix: sub.GetString("prefix", "").ToString(),
				gameTypeName: sub.GetString("name", "").ToString()
			);

			GameTypes.Add(gt);
		}
	}

	private ReadOnlySpan<char> GetGameTypeName(ReadOnlySpan<char> mapName) {
		LoadGameTypes();

		foreach (GameType gt in GameTypes)
			if (mapName.StartsWith(gt.Prefix, StringComparison.OrdinalIgnoreCase))
				return gt.GameTypeName;

		return "";
	}

	public ReadOnlySpan<char> GetMapFriendlyNameAndGameType(ReadOnlySpan<char> MapName, out ReadOnlySpan<char> friendlyMapName) {
		LoadGameTypes();

		ReadOnlySpan<char> friendlyGameTypeName = "";
		foreach (GameType gt in GameTypes) {
			int prefixLength = gt.Prefix.Length;
			if (MapName.StartsWith(gt.Prefix, StringComparison.OrdinalIgnoreCase)) {
				MapName = MapName[prefixLength..];
				friendlyGameTypeName = gt.GameTypeName;
				break;
			}
		}

		int l = MapName.Length;
		ReadOnlySpan<char> finalStr = "_final"; ;
		int finalIndex = MapName.IndexOf(finalStr, StringComparison.OrdinalIgnoreCase);
		if (finalIndex != -1) {
			int nextCharIndex = finalIndex + finalStr.Length;
			if (nextCharIndex >= MapName.Length || (MapName[nextCharIndex] == '1' && nextCharIndex + 1 == MapName.Length)) {
				l = finalIndex;
			}
		}

		if (l >= 256) {
			Assert("Map name too long for buffer!");
			l = 255;
		}

		friendlyMapName = MapName[..l];
		return friendlyGameTypeName;
	}

	private class GameType(string prefix, string gameTypeName)
	{
		public string Prefix = prefix;
		public string GameTypeName = gameTypeName;
	}
}
