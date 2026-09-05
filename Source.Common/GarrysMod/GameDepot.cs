using Steamworks;

namespace Source.Common.GarrysMod;

public static class IGameDepotSystem
{
	public struct Information
	{
		public AppId_t AppID;
		public uint Depot;
		public string Title;
		public string Folder;
		public bool Mounted;               
		public bool Enabled;               
		public bool Owned;                 
		public bool Installed;             
		public bool Retail;                
		public bool Bundled;               
		public List<string> ExtraFolders;  
	}
}

public static class GameDepot
{
	public interface System
	{
		void Refresh();
		void Clear();
		void Save();
		void SetMount(AppId_t appID, bool shouldMount);
		void MarkGameAsMounted(ReadOnlySpan<char> folder);
		List<IGameDepotSystem.Information> GetList();
		void MountAsMapFix(AppId_t appID);
		void MountCurrentGame(ReadOnlySpan<char> game);
	}
}
