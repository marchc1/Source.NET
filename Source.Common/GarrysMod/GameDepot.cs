namespace Source.Common.GarrysMod;

public static class IGameDepotSystem {
	public struct Information
	{
		public uint Placeholder1;
		public uint Depot;
		public string Title;
		public string Folder;
		public bool Mounted;
		public bool Placeholder6;
		public bool Owned;
		public bool Installed;
	}
}

public static class GameDepot {
	public interface System
	{
		void Refresh();
		void Clear();
		void Save();
		void SetMount(uint unk1, bool unk2);
		void MarkGameAsMounted(ReadOnlySpan<char> unk1);
		List<IGameDepotSystem.Information> GetList();
		void MountAsMapFix(uint unk1);
		void MountCurrentGame(ReadOnlySpan<char> unk1);
	}
}
