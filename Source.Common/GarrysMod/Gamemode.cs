namespace Source.Common.GarrysMod;

public static class IGamemodeSystem
{
	public struct Information
	{
		public bool Exists;
		public bool MenuSystem;
		public string Title;
		public string Name;
		public string Maps;
		public string BaseName;
		public string Category;
		public ulong WorkshopID;
	}
}

public static class Gamemode
{
	public interface System
	{
		void OnJoinServer(ReadOnlySpan<char> unk1);
		void OnLeaveServer();
		void Refresh();
		void Clear();
		ref IGamemodeSystem.Information Active();
		ref IGamemodeSystem.Information FindByName(ReadOnlySpan<char> str); // Guessing that it returns IGamemodeSystem::Information
		void SetActive(ReadOnlySpan<char> unk1);
		List<IGamemodeSystem.Information> GetList();
		bool IsServerBlacklisted(ReadOnlySpan<char> address, ReadOnlySpan<char> hostname, ReadOnlySpan<char> description, ReadOnlySpan<char> gm, ReadOnlySpan<char> map);
	}
}
