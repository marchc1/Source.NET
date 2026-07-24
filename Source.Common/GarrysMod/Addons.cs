using Steamworks;

namespace Source.Common.GarrysMod;

public static class IAddonSystem
{
	public struct Information
	{
		public string Title;
		public string File;
		public string Tags;
		public string Placeholder1;
		public DateTime TimeUpdated;
		public ulong WorkshopID;
		public CSteamID Creator;
		public ulong HContentFile;
		public ulong Size;
		public ulong HContentPreview;
		public DateTime TimeAdded;
	}

	public struct UGCInfo
	{
		public string Title;
		public string File;
		public string Placeholder1;
		public ulong WorkshopID;
		public CSteamID Creator;
		public DateTime PubDate;
	}
}

public static class Addon
{
	public static class Job
	{
		public interface Base
		{
			void Init(Addon.FileSystem fs);
		}
	}

	public interface FileSystem
	{
		void Clear();
		void Refresh();
		int MountFile(ReadOnlySpan<char> unk1, List<string> unk2);
		bool ShouldMount(ReadOnlySpan<char> unk1);
		bool ShouldMount(ulong unk1);
		void SetShouldMount(ReadOnlySpan<char> unk1, bool unk2);
		void Save();
		List<IAddonSystem.Information> GetList();
		List<IAddonSystem.UGCInfo> GetUGCList();
		void ScanForSubscriptions(ReadOnlySpan<char> unk1);
		void Think();
		void SetDownloadNotify(IAddonDownloadNotification unk1);
		int Notify();
		bool IsSubscribed(ulong workshopID);
		ref readonly IAddonSystem.Information FindFileOwner(ReadOnlySpan<char> unk1);
		void AddFile(ref IAddonSystem.Information info);
		void ClearAllGMAs();
		void GetSteamUGCFile(ulong workshopID, bool unk1);
		void UnmountAddon(ulong workshopID);
		void UnmountServerAddons();
		void MountFloatingAddons();
		void Shutdown();
		void AddFile(in SteamUGCDetails_t unk1);
		void AddSubscription(in SteamUGCDetails_t unk1);
		void AddJob<T>(T job) where T : Job.Base;
		bool HasChanges();
		void MarkChanged();
		void AddonDownloaded(ref IAddonSystem.Information info);
		void NotifyAddonFailedToDownload(ref IAddonSystem.Information info);
		List<SteamUGCDetails_t> GetSubList();
		void IsAddonValidPreInstall(SteamUGCDetails_t unk1);
		void Load();
	}

	public static class Task
	{
		public interface DownloadAddons : Job.Base
		{
			void Start();
			void Cycle();
			void Finished();
		}

		public interface DownloadFile : Job.Base
		{
			void Start();
			void Cycle();
			void Finished();
		}

		public interface AddFloatingAddons : Job.Base
		{
			void Start();
			void Cycle();
			void Finished();
		}

		public interface GetSubscriptions : Job.Base
		{
			void Start();
			void Cycle();
			void Finished();
		}

		public interface GetSubscriptions_Offline : Job.Base
		{
			void Start();
			void Cycle();
			void Finished();
		}

		public interface MountAvailable : Job.Base
		{
			void Start();
			void Cycle();
			void Finished();
		}

		public interface NotifyStart : Job.Base
		{
			void Start();
			void Cycle();
			void Finished();
		}

		public interface NotifyEnd : Job.Base
		{
			void Start();
			void Cycle();
			void Finished();
		}

		public interface OnSubscribed : Job.Base
		{
			void Start();
			void Cycle();
			void Finished();
			void OnReceiveFileInfo(/* todo */);
		}

		public interface UpdateTotals : Job.Base
		{
			void Start();
			void Cycle();
			void Finished();
		}
	}
}
