using Source.GUI.Controls;

using Steamworks;

namespace Game.ServerBrowser;

class ModList
{
	struct Mod
	{
		public string Description;
		public string GameDir;
		public CGameID GameID;
		public int InternalAppId;
		public override readonly bool Equals(object? obj) => obj is Mod other && other.GameID == GameID;
	}

	readonly List<Mod> Modlist = [];
	readonly List<Panel> VGUIListeners = [];

	public static ModList? Instance;

	public ModList() {
		Instance = this;
		ParseSteamMods();
	}

	int ModCount() => Modlist.Count;

	ReadOnlySpan<char> GetModName(int index) => Modlist[index].Description;
	ReadOnlySpan<char> GetModDir(int index) => Modlist[index].GameDir;
	CGameID GetAppID(int index) => Modlist[index].GameID;
	int GetIndex(CGameID appId) => Modlist.FindIndex(mod => mod.GameID == appId);

	public ReadOnlySpan<char> GetModNameForModDir(CGameID gameID) {
		int app = GetIndex(gameID);
		if (app != -1)
			return Modlist[app].Description;

		ReadOnlySpan<char> activeModName = ServerBrowserDialog.Instance!.GetActiveModName();
		if (!activeModName.IsEmpty)
			return activeModName;

		return "";
	}

	int ModNameCompare(Mod left, Mod right) => stricmp(left.Description, right.Description);
	void ParseSteamMods() { }
	int LoadAppConfiguration(UInt32 appID) => -1;
	void AddVGUIListener(Panel panel) => VGUIListeners.Add(panel);
}