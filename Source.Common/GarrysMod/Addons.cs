using Steamworks;

namespace Source.Common.GarrysMod;

public static class IAddonSystem
{
	public struct Information
	{
		public string Title;
		public string File;
		public string Tags;
		public string Failure;
		public uint TimeUpdated;
		public uint Models;
		public ulong WorkshopID;
		public ulong Creator;
		public ulong HContentFile;
		public uint Size;
		public ulong HContentPreview;
		public uint TimeAdded;
		public bool CanUpdate;
		public bool Downloaded;
		public bool Failed;
		public bool Legacy;
		public bool Unloaded;
	}

	public struct UGCInfo
	{
		public string Title;
		public string Tags;
		public uint TimeAdded;
		public ulong WorkshopID;
		public ulong Creator;
		public Addon.AddonType Type;
		public bool Unloaded;
	}
}

public static class Addon
{
	public enum AddonType : byte
	{
		Unknown,
		Addon,
		Dupe,
		Save,
		Demo,
		ServerContent
	}

	public static class Job
	{
		public class Base
		{
			protected FileSystem AddonSystem = null!;

			public virtual void Start() { }
			public virtual void Cycle() { }
			public virtual bool Finished() => true;
			public virtual void Init(FileSystem fs) => AddonSystem = fs;
		}
	}

	public interface FileSystem
	{
		void Clear();
		void Refresh();
		bool MountFile(ReadOnlySpan<char> file, List<string>? files, ulong workshopID, ulong unk1, int unk2);
		bool ShouldMount(ulong workshopID);
		void SetShouldMount(ulong workshopID, bool shouldMount);
		void Save();
		LinkedList<IAddonSystem.Information> GetList();
		LinkedList<IAddonSystem.UGCInfo> GetUGCList();
		void ScanForSubscriptions(ReadOnlySpan<char> unk1, bool unk2);
		void Think();
		void SetDownloadNotify(IAddonDownloadNotification? unk1);
		IAddonDownloadNotification? Notify();
		bool IsSubscribed(ulong workshopID);
		bool FindFileOwner(ReadOnlySpan<char> unk1, out IAddonSystem.Information info);
		void AddAddon(in IAddonSystem.Information info);
		void ClearUnusedGMAs();
		string GetAddonFilepath(ulong workshopID, bool unk1);
		void UnmountAddon(ulong workshopID, ReadOnlySpan<char> reason);
		void UnmountServerAddons();
		string IsAddonValidPreInstall(in SteamUGCDetails_t details);
		bool AllJobsFinished();
		void Shutdown();
		void AddJob(Job.Base job);
		LinkedList<SteamUGCDetails_t> GetSubList();
		void MountFloatingAddons();
		void AddAddonFromSteamDetails(in SteamUGCDetails_t details);
		void OnAddonSubscribed(in SteamUGCDetails_t details);
		void AddUnloadedSubscription(ulong workshopID);
		void EnableLoadingUnloadedAddons();
		bool HasChanges();
		void MarkChanged();
		void OnAddonDownloaded(in IAddonSystem.Information info);
		void OnAddonDownloadFailed(in IAddonSystem.Information info);
		void Load();
	}
}
