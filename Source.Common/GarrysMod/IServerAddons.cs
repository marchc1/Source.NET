namespace Source.Common.GarrysMod;

public interface IServerAddons
{
	void Update();
	int GetCount();
	void Queue(ReadOnlySpan<char> unk1);
	void Clear();
	void MountDownloadedAddons();
}
