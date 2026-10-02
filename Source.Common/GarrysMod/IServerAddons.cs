namespace Source.Common.GarrysMod;

public interface IServerAddons
{
	bool Update();
	int GetCount();
	bool Queue(ReadOnlySpan<char> file);
	void Clear();
	void MountDownloadedAddons();
}
