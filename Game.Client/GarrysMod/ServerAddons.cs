using Bootil.Compression;

using Source;
using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.GarrysMod;

using Steamworks;

namespace Game.Client.GarrysMod;

public abstract class IAddonDownloader : IDisposable
{
	public ulong WorkshopID;
	public ulong TimeUpdated;

	public abstract void Dispose();
	public abstract bool Update();
	public abstract ReadOnlySpan<char> GetFile();
}

public class WorkshopDownloader : IAddonDownloader
{
	Callback<DownloadItemResult_t>? DownloadItemResult;
	SteamUGCDetails_t Details;
	string CachePath = "";
	string File = "";
	LZMA.ExtractionThread? Extractor;
	int State;
	int Failures;
	bool Downloading;
	ulong LastBytes;
	double LastTime;

	public WorkshopDownloader(in SteamUGCDetails_t details) {
		WorkshopID = details.m_nPublishedFileId.m_PublishedFileId;
		Details = details;
		TimeUpdated = details.m_rtimeUpdated;
		Start();
	}

	void Start() {
		ServerAddons.TotalBytes += (ulong)(uint)Details.m_nFileSize;
		CachePath = $"{get.GameDir()}/cache/workshop/{WorkshopID}.gma";
		State = 1;
	}

	public override void Dispose() {
		DownloadItemResult?.Unregister();

		if (Downloading)
			SteamUGC.MarkDownloadedItemAsUnused(new PublishedFileId_t(WorkshopID));

		if (Extractor != null) {
			while (!Extractor.IsDone()) {
				ServerAddons.UpdateProgress("Cancelling Workshop download", true);
				Thread.Sleep(10);
			}
			Extractor = null;
		}
	}

	public override bool Update() {
		switch (State) {
			case 1:
				if (Details.m_nPublishedFileId.m_PublishedFileId == 0) {
					Warning($"WorkshopDL: Failed to get details for file {WorkshopID}\n");
					return false;
				}

				State = 2;
				uint itemState = SteamUGC.GetItemState(new PublishedFileId_t(WorkshopID));
				if ((itemState & (uint)(EItemState.k_EItemStateInstalled | EItemState.k_EItemStateNeedsUpdate)) == (uint)EItemState.k_EItemStateInstalled) {
					if (SteamUGC.GetItemInstallInfo(new PublishedFileId_t(WorkshopID), out _, out _, 260, out uint timeStamp) && timeStamp != Details.m_rtimeUpdated)
						itemState |= (uint)EItemState.k_EItemStateNeedsUpdate;
				}

				if ((itemState & (uint)EItemState.k_EItemStateInstalled) != 0 && (itemState & (uint)EItemState.k_EItemStateNeedsUpdate) == 0) {
					if ((itemState & (uint)EItemState.k_EItemStateLegacyItem) != 0)
						return CheckLegacyCache();

					Downloaded();
					return false;
				}

				return true;
			case 2:
				return StartDownload();
			case 3:
				return UpdateDownload();
			case 4:
				return Extract();
		}

		return false;
	}

	public override ReadOnlySpan<char> GetFile() => File.Length == 0 ? null : File;

	bool CheckLegacyCache() {
		string path = $"{get.GameDir()}/cache/workshop/{Details.m_hFile.m_UGCHandle}.gma";
		if (!CheckFile(path, true)) {
			path = path.Replace(".gma", ".cache");
			if (!CheckFile(path, true) && !CheckFile(CachePath, false))
				return true;
		}

		return false;
	}

	bool CheckFile(string path, bool legacy) {
		IFileHandle? handle = filesystem.Open(path, FileOpenOptions.Read | FileOpenOptions.Binary, "MOD");
		if (handle == null)
			return false;

		Span<byte> timeStamp = stackalloc byte[4];
		timeStamp.Clear();
		handle.Stream.Seek(13, SeekOrigin.Begin);
		handle.Stream.Read(timeStamp);
		handle.Dispose();

		if (BitConverter.ToUInt32(timeStamp) != Details.m_rtimeUpdated) {
			if (legacy)
				filesystem.RemoveFile(path, null);
			return false;
		}

		ServerAddons.UpdateProgress($"Checking for updates\n'{Details.m_rgchTitle}'", false);

		if (!legacy)
			File = path;
		else {
			if (filesystem.RenameFile(path, CachePath, null))
				path = CachePath;
			File = path;
		}

		if (!SteamUGC.GetItemInstallInfo(new PublishedFileId_t(WorkshopID), out _, out _, 260, out _))
			Warning($"WorkshopDL: Failed to GetItemInstallInfo for '{Details.m_rgchTitle}' ({WorkshopID}) to mark as used\n");

		return true;
	}

	bool StartDownload() {
		filesystem.Addons().UnmountAddon(WorkshopID, "for update, server");

		ServerAddons.Progress = 0;
		ServerAddons.UpdateProgress($"Downloading Workshop content\n'{Details.m_rgchTitle}'", false);

		if (!SteamUGC.DownloadItem(new PublishedFileId_t(WorkshopID), true)) {
			Warning($"WorkshopDL: Failed to initialize download for addon '{Details.m_rgchTitle}' ({WorkshopID})\n");
			return false;
		}

		DownloadItemResult?.Unregister();
		DownloadItemResult = Callback<DownloadItemResult_t>.Create(OnDownloadItemResult);

		State = 3;
		LastTime = Platform.Time;
		Downloading = true;
		return true;
	}

	bool UpdateDownload() {
		if (Failures >= 6) {
			Warning($"WorkshopDL: GetItemDownloadInfo failed for {WorkshopID} too many times or download is stuck, aborting...\n");
			return false;
		}

		if (!SteamUGC.GetItemDownloadInfo(new PublishedFileId_t(WorkshopID), out ulong bytesDownloaded, out ulong bytesTotal)) {
			Failures++;
			Warning($"WorkshopDL: GetItemDownloadInfo failed for {WorkshopID}, trying...\n");
			return true;
		}

		if (bytesTotal == 0) {
			bytesDownloaded = 0;
			bytesTotal = 1;
		}

		if (Platform.Time - LastTime > 10.0) {
			ulong transferred = bytesDownloaded - LastBytes;
			if (transferred < 0x2000) {
				Msg($"WorkshopDL: Transferred {Bootil.String.Format.Memory(transferred)} ({transferred}) in 10 seconds!\n");
				Failures++;
			}
			else
				Failures = 0;

			LastBytes = bytesDownloaded;
			LastTime = Platform.Time;
		}

		ServerAddons.Progress = (float)((double)bytesDownloaded / (double)bytesTotal);

		if (bytesTotal < 2)
			ServerAddons.UpdateProgress($"Downloading Workshop content\n'{Details.m_rgchTitle}'", true);
		else
			ServerAddons.UpdateProgress($"Downloading Workshop content\n'{Details.m_rgchTitle}' ({Bootil.String.Format.Memory(bytesTotal)})", true);

		return true;
	}

	void OnDownloadItemResult(DownloadItemResult_t result) {
		if (result.m_unAppID.m_AppId != 4000)
			return;

		if (result.m_nPublishedFileId.m_PublishedFileId != WorkshopID) {
			Warning($"WorkshopDL: Received wrong downloaded item? Got {result.m_nPublishedFileId.m_PublishedFileId}, expecting {WorkshopID}\n");
			return;
		}

		if (result.m_eResult == EResult.k_EResultOK) {
			Failures = 0;
			LastTime = Platform.Time - LastTime;
			Downloading = false;

			if ((SteamUGC.GetItemState(new PublishedFileId_t(WorkshopID)) & (uint)EItemState.k_EItemStateLegacyItem) != 0) {
				State = 4;
				return;
			}

			Downloaded();
			State = 5;
			return;
		}

		Warning($"WorkshopDL: '{Details.m_rgchTitle}' ({WorkshopID}) failed to download, {SteamResult.ToString(result.m_eResult)}\n");

		string parms = Details.m_rgchTitle + ";" + SteamResult.ToString(result.m_eResult);
		get.MenuSystem()?.SendProblemToMenu("addon_download_failed", 2, parms);
		State = 5;
	}

	bool Extract() {
		if (Extractor != null) {
			if (Extractor.IsDone()) {
				if (!Extractor.Success()) {
					Warning($"WorkshopDL: Failed to extract to '{CachePath}'\n");
					State = 5;
					return false;
				}

				FileStream? stream = null;
				try {
					stream = new FileStream(CachePath, FileMode.Open, FileAccess.ReadWrite);
				}
				catch { }

				if (stream == null)
					Warning($"WorkshopDL: Failed to open file '{CachePath}'\n");
				else {
					stream.Seek(13, SeekOrigin.Begin);
					stream.Write(BitConverter.GetBytes(Details.m_rtimeUpdated));
					stream.Flush();

					ServerAddons.Progress = 1.0f;
					ServerAddons.UpdateProgress($"Extracted!\n'{Details.m_rgchTitle}'", false);
					File = CachePath;
				}

				State = 5;
				stream?.Dispose();
				return false;
			}

			ServerAddons.Progress = Extractor.GetProgress();
			ServerAddons.UpdateProgress($"Extracting Workshop content\n'{Details.m_rgchTitle}'", true);
			return true;
		}

		if (!SteamUGC.GetItemInstallInfo(new PublishedFileId_t(WorkshopID), out _, out string folder, 260, out _)) {
			Warning($"WorkshopDL: Failed to GetItemInstallInfo for '{Details.m_rgchTitle}' ({WorkshopID})\n");
			return false;
		}

		byte[] buffer;
		try {
			buffer = System.IO.File.ReadAllBytes(folder);
		}
		catch {
			Warning($"WorkshopDL: Failed to read addon file for '{Details.m_rgchTitle}' ({WorkshopID})\n");
			return false;
		}

		int size = buffer.Length;
		if (BitConverter.ToUInt32(buffer, size - 4) == 0xBEEFCACE)
			size -= 8;

		ServerAddons.Progress = 0;
		ServerAddons.UpdateProgress($"Extracting Workshop content\n'{Details.m_rgchTitle}'", false);

		Extractor = new LZMA.ExtractionThread(buffer.AsSpan(0, size), CachePath);
		return true;
	}

	void Downloaded() {
		string file = filesystem.Addons().GetAddonFilepath(WorkshopID, true);
		if (file.Length == 0) {
			Warning($"WorkshopDL: Failed to GetItemInstallInfo for '{Details.m_rgchTitle}' ({WorkshopID})\n");
			return;
		}

		ServerAddons.Progress = 1.0f;
		ServerAddons.UpdateProgress($"Downloaded!\n'{Details.m_rgchTitle}'", false);
		File = file;
	}
}

public class ServerAddons : IServerAddons
{
	record struct DownloadedAddon(string File, ulong WorkshopID, ulong TimeUpdated);

	internal static int Total;
	internal static int Current;
	internal static ulong TotalBytes;
	internal static float Progress;
	static string LastProgress = "";

	readonly Queue<ulong> Queued = [];
	readonly Queue<IAddonDownloader> Downloaders = [];
	int QueuedTotal;
	int QueuedRemaining;
	IAddonDownloader? CurrentDownloader;
	readonly List<DownloadedAddon> Downloaded = [];
	bool HasDownloaded;
	UGCQueryHandle_t Query = UGCQueryHandle_t.Invalid;
	readonly CallResult<SteamUGCQueryCompleted_t> QueryCompleted;

	public ServerAddons() {
		QueryCompleted = CallResult<SteamUGCQueryCompleted_t>.Create(OnQueryCompleted);
	}

	public bool Update() {
		Total = QueuedTotal;
		Current = QueuedTotal - QueuedRemaining + 1;

		if (Queued.Count == 0) {
			if (Query != UGCQueryHandle_t.Invalid)
				return true;

			if (CurrentDownloader == null && Downloaders.Count != 0)
				CurrentDownloader = Downloaders.Dequeue();

			if (CurrentDownloader != null) {
				if (!CurrentDownloader.Update()) {
					ReadOnlySpan<char> file = CurrentDownloader.GetFile();
					if (file.IsEmpty)
						Warning($"WorkshopDL: There is no file to mount for '{CurrentDownloader.WorkshopID}'!\n");
					else {
						Downloaded.Add(new(new string(file), CurrentDownloader.WorkshopID, CurrentDownloader.TimeUpdated));
						HasDownloaded = true;
					}

					QueuedRemaining--;
					CurrentDownloader.Dispose();
					CurrentDownloader = null;
				}

				return true;
			}

			if (HasDownloaded) {
				enginevgui.UpdateCustomProgressBar(0.5f, "Mounting Workshop Addons");
				MountDownloadedAddons();
				get.FileSystem()!.DoFilesystemRefresh();
				enginevgui.UpdateCustomProgressBar(0.71f, "Workshop Complete!");
			}

			HasDownloaded = false;
			return false;
		}

		if (Query == UGCQueryHandle_t.Invalid) {
			PublishedFileId_t[] ids = new PublishedFileId_t[500];
			uint count = 0;
			while (count < 500) {
				ids[count++] = new PublishedFileId_t(Queued.Dequeue());
				if (Queued.Count == 0)
					break;
			}

			UpdateProgress("Fetching info about workshop addons...", false);
			Msg($"WorkshopDL: Querying {count} addons for info..\n");

			Query = SteamUGC.CreateQueryUGCDetailsRequest(ids, count);
			QueryCompleted.Set(SteamUGC.SendQueryUGCRequest(Query));
		}

		return true;
	}

	public int GetCount() => Math.Max(Downloaders.Count, Queued.Count);

	public bool Queue(ReadOnlySpan<char> file) {
		string str = new(file);
		if (!str.EndsWith(".gma", StringComparison.Ordinal))
			return false;

		str = str.Replace(".gma", "");
		ulong workshopID = Bootil.String.To.UInt64(str);
		if (workshopID == 0)
			return false;

		Queued.Enqueue(workshopID);
		QueuedTotal++;
		QueuedRemaining++;
		return true;
	}

	public void Clear() {
		Downloaded.Clear();

		while (Downloaders.Count != 0)
			Downloaders.Dequeue()?.Dispose();

		Queued.Clear();

		if (Query != UGCQueryHandle_t.Invalid) {
			SteamUGC.ReleaseQueryUGCRequest(Query);
			Query = UGCQueryHandle_t.Invalid;
		}

		if (QueryCompleted.IsActive())
			QueryCompleted.Cancel();

		QueuedTotal = 0;
		QueuedRemaining = 0;
		TotalBytes = 0;

		CurrentDownloader?.Dispose();
		CurrentDownloader = null;

		HasDownloaded = false;
	}

	public void MountDownloadedAddons() {
		foreach (DownloadedAddon addon in Downloaded) {
			if (!get.FileSystem()!.Addons().MountFile(addon.File, null, addon.WorkshopID, addon.TimeUpdated, 1))
				Warning($"WorkshopDL: Failed to mount {addon.File}\n");
		}

		filesystem.Gamemodes().OnServerDownloadsMounted();
	}

	void OnQueryCompleted(SteamUGCQueryCompleted_t result, bool ioFailure) {
		if (!ioFailure) {
			int skipped = 0;
			for (uint i = 0; i < result.m_unNumResultsReturned; i++) {
				if (!SteamUGC.GetQueryUGCResult(result.m_handle, i, out SteamUGCDetails_t details)) {
					Warning($"WorkshopDL: ReceivedBatchInfo failed for item {i}\n");
					continue;
				}

				ConVarRef cl_downloadfilter = new("cl_downloadfilter");
				if (stricmp(cl_downloadfilter.GetString(), "mapsonly") == 0 && strstr(details.m_rgchTags, ",map").IsEmpty) {
					skipped++;
					continue;
				}

				string reason = filesystem.Addons().IsAddonValidPreInstall(in details);
				if (reason.Length == 0)
					Downloaders.Enqueue(new WorkshopDownloader(in details));
				else
					Warning($"WorkshopDL: Not downloading/mounting addon '{details.m_rgchTitle}' ({details.m_nPublishedFileId.m_PublishedFileId}): {reason}!\n");
			}

			if (skipped != 0)
				Msg($"WorkshopDL: Skipped {skipped} workshop addons that do not match cl_downloadfilter!\n");

			SteamUGC.ReleaseQueryUGCRequest(result.m_handle);
		}
		else {
			Warning($"WorkshopDL: ReceivedBatchInfo failed - {SteamResult.ToString(result.m_eResult)}\n");
			SteamUGC.ReleaseQueryUGCRequest(result.m_handle);
		}

		Query = UGCQueryHandle_t.Invalid;
	}

	internal static void UpdateProgress(string status, bool force) {
		if (status == LastProgress && !force)
			return;

		LastProgress = status;

		string text = $"{Current}/{Total} ({Bootil.String.Format.Memory(TotalBytes)} total) - {status}";
		enginevgui.UpdateCustomProgressBar(Progress, text);
	}
}
