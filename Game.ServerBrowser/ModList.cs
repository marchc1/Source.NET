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
		public int IntervalAppId;
		public override bool Equals(object? obj) => obj is Mod other && other.GameID == GameID;
	}

	List<Mod> Modlist = [];
	List<Panel> VGUIListeners;

	public static ModList? Instance;

	public ModList() {
		Instance = this;
	}

	int ModCount() {
		throw new NotImplementedException();
	}

	ReadOnlySpan<char> GetModName(int index) {
		throw new NotImplementedException();
	}

	ReadOnlySpan<char> GetModDir(int index) {
		throw new NotImplementedException();
	}

	CGameID GetAppID(int index) {
		throw new NotImplementedException();
	}

	int GetIndex(CGameID appId) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetModNameForModDir(CGameID gameID) {
		throw new NotImplementedException();
	}

	int ModNameCompare(Mod left, Mod right) {
		throw new NotImplementedException();
	}

	void ParseSteamMods() { }

	int LoadAppConfiguration(UInt32 appID) {
		throw new NotImplementedException();
	}

	void AddVGUIListener(Panel panel) { }
}