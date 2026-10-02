using Bootil.Compression;

using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.GarrysMod;
using Source.FileSystem;

using Steamworks;

namespace Source.Filesystem.GarrysMod;

public static class DedicatedServerAddons
{
	const int MinCollectionLength = 5;
	const int LoginWaitStep = 250;
	const int LoginWaitMax = 10000;
	const int CallWaitStep = 100;
	const int CallWaitMax = 600000;
	const int RetryWait = 500;
	const int MaxRetries = 4;
	const int DownloadWait = 1000;

	static readonly Color ColorInfo = new(90, 255, 255, 0);
	static readonly Color ColorHighlight = new(255, 255, 90, 0);
	static readonly Color ColorError = new(255, 90, 90, 0);
	static readonly Color ColorGood = new(90, 255, 90, 0);

	static bool AutoUpdate;
	static bool LoadedFromCache;
	static readonly List<ulong> ProcessedCollections = [];
	static readonly List<ulong> PendingCollections = [];
	static readonly List<SteamUGCDetails_t> Addons = [];

	class CollectionInfo
	{
		public readonly List<ulong> Children = [];
		public string Name = "";
		public bool Failed;
	}

	class ItemInfo
	{
		public readonly List<SteamUGCDetails_t> Items = [];
		public bool Failed;
	}

	public static void RunAddonProcess(ReadOnlySpan<char> unk1, bool unk2) {
		if (!RunChecks(unk1))
			return;

		if (!LoadedFromCache) {
			bool failed = false;
			PendingCollections.Add(Bootil.String.To.UInt64(unk1));
			AutoUpdate = unk2;

			ConColorMsg(ColorInfo, "WS: Fetching collection info...\n");
			while (PendingCollections.Count != 0) {
				if (DownloadCollection())
					failed = true;
			}

			ConColorMsg(ColorInfo, "WS: Finished!\n\n");
			ProcessedCollections.Clear();

			if (failed) {
				ConColorMsg(ColorError, "WS: Detected failed collections!\n");
				LoadCachedAddonList();
			}
		}

		g_FullFileSystem.CreateDirHierarchy("cache/srcds", "DEFAULT_WRITE_PATH");

		List<SteamUGCDetails_t> addons = [.. Addons];

		ConColorMsg(ColorInfo, $"WS: Processing {Addons.Count} addons...{(AutoUpdate ? "" : " (Updates disabled)")}\n");
		while (Addons.Count != 0)
			DownloadAddon();

		ConColorMsg(ColorInfo, "WS: Finished!\n");

		CacheAddonList(addons);
		ClearUnusedGMAs();
	}

	static bool RunChecks(ReadOnlySpan<char> collection) {
		if (CommandLine.FindParm("-authkey") != 0)
			Warning("-authkey is no longer required by Garry's Mod. You can safely remove it from your server's launch options.\n");

		if (collection.IsEmpty || collection.Length < MinCollectionLength) {
			ConColorMsg(ColorError, "WS: No +host_workshop_collection or it is invalid!\n");
			return false;
		}

		int waited = 0;
		while (!SteamGameServer.BLoggedOn()) {
			if (waited == 0)
				ConColorMsg(ColorInfo, "WS: Waiting for Steam to log us in");

			Thread.Sleep(LoginWaitStep);
			waited += LoginWaitStep;
			ConColorMsg(ColorInfo, ".");
			BaseFileSystem.get.RunSteamCallbacks();

			if (waited > LoginWaitMax) {
				ConColorMsg(ColorError, "\nWS: Steam failed to log on within 10 seconds! Loading cached list...\n");
				return LoadCachedAddonList();
			}
		}

		Msg("\n");
		BaseFileSystem.get.RunSteamCallbacks();

		string steamCache = $"{Directory.GetCurrentDirectory()}/steam_cache";
		Bootil.String.File.FixSlashes(ref steamCache, "/", "\\");

		if (!SteamGameServerUGC.BInitWorkshopForGameServer(new DepotId_t((uint)GetSteamInfIDVersionInfo().AppID), steamCache))
			Warning("WS: BInitWorkshopForGameServer failed!\n");

		return true;
	}

	static bool WaitForCall(SteamAPICall_t call) {
		if (call == SteamAPICall_t.Invalid)
			return true;

		int waited = 0;
		bool failed;
		while (!SteamGameServerUtils.IsAPICallCompleted(call, out failed)) {
			Thread.Sleep(CallWaitStep);
			waited += CallWaitStep;
			if (waited > CallWaitMax) {
				Warning("Steam call is taking longer than 10 minutes.. assuming timed out!\n");
				return true;
			}
		}

		BaseFileSystem.get.RunSteamCallbacks();
		return failed;
	}

	static bool DownloadCollection() {
		bool failed = true;
		ulong collection = PendingCollections[0];

		ConColorMsg(ColorHighlight, $"Processing collection {collection}...\n");

		for (int attempt = 0; attempt <= MaxRetries; attempt++) {
			CollectionInfo info = new();
			CallResult<SteamUGCQueryCompleted_t> infoResult = CallResult<SteamUGCQueryCompleted_t>.Create((result, ioFailure) => OnCollectionInfo(info, result, ioFailure));

			SteamAPICall_t call = SteamAPICall_t.Invalid;
			UGCQueryHandle_t query = SteamGameServerUGC.CreateQueryUGCDetailsRequest([new PublishedFileId_t(collection)], 1);
			if (query == UGCQueryHandle_t.Invalid)
				info.Failed = true;
			else {
				SteamGameServerUGC.SetReturnChildren(query, true);
				call = SteamGameServerUGC.SendQueryUGCRequest(query);
				if (call == SteamAPICall_t.Invalid)
					info.Failed = true;
				else
					infoResult.Set(call);
			}

			if (!WaitForCall(call) && !info.Failed) {
				failed = false;
				ConColorMsg(ColorInfo, $"   Collection '{info.Name}' ({info.Children.Count} items)\n");

				if (info.Children.Count < 1) {
					ConColorMsg(ColorGood, "   Reported 0 items, skipping\n");
					infoResult.Cancel();
					break;
				}

				ConColorMsg(ColorInfo, "   Retrieving item details...\n");

				ItemInfo items = new();
				CallResult<SteamUGCQueryCompleted_t> itemsResult = CallResult<SteamUGCQueryCompleted_t>.Create((result, ioFailure) => OnItemInfo(items, result, ioFailure));

				PublishedFileId_t[] children = new PublishedFileId_t[info.Children.Count];
				for (int i = 0; i < children.Length; i++)
					children[i] = new PublishedFileId_t(info.Children[i]);

				query = SteamGameServerUGC.CreateQueryUGCDetailsRequest(children, (uint)children.Length);
				call = SteamGameServerUGC.SendQueryUGCRequest(query);
				if (call == SteamAPICall_t.Invalid)
					items.Failed = true;
				else
					itemsResult.Set(call);

				if (!WaitForCall(call) && !items.Failed) {
					if (items.Items.Count > 0) {
						int newAddons = 0;
						int newCollections = 0;

						foreach (SteamUGCDetails_t item in items.Items) {
							ulong workshopID = item.m_nPublishedFileId.m_PublishedFileId;
							if (item.m_eFileType == EWorkshopFileType.k_EWorkshopFileTypeCollection) {
								if (ProcessedCollections.Contains(workshopID) || PendingCollections.Contains(workshopID))
									continue;

								if (workshopID != collection) {
									PendingCollections.Add(workshopID);
									newCollections++;
								}
							}
							else if (item.m_eFileType == EWorkshopFileType.k_EWorkshopFileTypeCommunity) {
								if (Addons.Exists(x => x.m_nPublishedFileId.m_PublishedFileId == workshopID))
									continue;

								Addons.Add(item);
								newAddons++;
							}
							else
								ConColorMsg(ColorError, $"   Unhandled type {(int)item.m_eFileType} for item {workshopID}\n");
						}

						Msg("  ");
						if (newAddons > 0)
							ConColorMsg(ColorGood, $" {newAddons} new addon(s)");
						if (newCollections > 0)
							ConColorMsg(ColorGood, $" + {newCollections} new collection(s)");
						if (newAddons == 0 && newCollections == 0)
							ConColorMsg(ColorGood, " No new addons or collections");
						Msg("\n");

						itemsResult.Cancel();
						infoResult.Cancel();
						break;
					}

					failed = true;
					ConColorMsg(ColorError, "   Failed to get info of collection items, retrying...\n");
					Thread.Sleep(RetryWait);
				}
				else {
					failed = true;
					ConColorMsg(ColorError, "   Failed to get collection items, retrying...\n");
					Thread.Sleep(RetryWait);
				}

				itemsResult.Cancel();
			}
			else {
				ConColorMsg(ColorError, "   Failed to get collection info, retrying...\n");
				Thread.Sleep(RetryWait);
			}

			infoResult.Cancel();
		}

		ProcessedCollections.Add(collection);
		PendingCollections.Remove(collection);
		return failed;
	}

	static void OnCollectionInfo(CollectionInfo info, SteamUGCQueryCompleted_t result, bool ioFailure) {
		if (result.m_eResult != EResult.k_EResultOK || ioFailure) {
			Warning($"   WS GetCollectionInfo failed with code {SteamResult.ToString(result.m_eResult)}\n");
			SteamGameServerUGC.ReleaseQueryUGCRequest(result.m_handle);
			info.Failed = true;
			return;
		}

		for (uint i = 0; i < result.m_unNumResultsReturned; i++) {
			if (!SteamGameServerUGC.GetQueryUGCResult(result.m_handle, i, out SteamUGCDetails_t details)) {
				info.Failed = true;
				Warning("   Failed to retrieve collection info!\n");
			}
			else if (details.m_eResult == EResult.k_EResultOK) {
				info.Name = details.m_rgchTitle;
				if (details.m_eFileType == EWorkshopFileType.k_EWorkshopFileTypeCollection) {
					if (details.m_eVisibility != ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic)
						Warning("   Warning! Item is not public!\n");

					PublishedFileId_t[] children = new PublishedFileId_t[details.m_unNumChildren];
					SteamGameServerUGC.GetQueryUGCChildren(result.m_handle, i, children, details.m_unNumChildren);
					foreach (PublishedFileId_t child in children)
						info.Children.Add(child.m_PublishedFileId);
				}
				else
					Warning("   Item is not a collection!\n");
			}
			else {
				info.Failed = true;
				Warning($"   Failed to get collection info! {SteamResult.ToString(details.m_eResult)}!\n");
			}
		}

		SteamGameServerUGC.ReleaseQueryUGCRequest(result.m_handle);
	}

	static void OnItemInfo(ItemInfo info, SteamUGCQueryCompleted_t result, bool ioFailure) {
		if (result.m_eResult == EResult.k_EResultOK && !ioFailure) {
			for (uint i = 0; i < result.m_unNumResultsReturned; i++) {
				if (!SteamGameServerUGC.GetQueryUGCResult(result.m_handle, i, out SteamUGCDetails_t details)) {
					Warning($"   WS GetItemInfo failed to get item {i}\n");
					info.Failed = true;
				}
				else
					info.Items.Add(details);
			}

			SteamGameServerUGC.ReleaseQueryUGCRequest(result.m_handle);
			return;
		}

		Warning($"   WS GetItemInfo failed with code {SteamResult.ToString(result.m_eResult)}\n");
		SteamGameServerUGC.ReleaseQueryUGCRequest(result.m_handle);
		info.Failed = true;
	}

	static void DownloadAddon() {
		SteamUGCDetails_t details = Addons[0];
		ulong workshopID = details.m_nPublishedFileId.m_PublishedFileId;

		if (details.m_rgchTitle.Length == 0)
			ConColorMsg(ColorHighlight, $"Processing addon {Addons.Count}: {workshopID}...\n");
		else
			ConColorMsg(ColorHighlight, $"Processing addon {Addons.Count}: {details.m_rgchTitle} ({workshopID})...\n");

		if (details.m_bBanned) {
			ConColorMsg(ColorError, "   Addon banned!\n");
			RemoveAddon(workshopID);
			return;
		}

		if (details.m_eResult != EResult.k_EResultOK) {
			ConColorMsg(ColorError, $"   Failed to get addon info! {SteamResult.ToString(details.m_eResult)}!\n");
			RemoveAddon(workshopID);
			return;
		}

		string cachePath = $"cache/srcds/{workshopID}.gma";

		Color color;
		string message;
		bool installed = SteamGameServerUGC.GetItemInstallInfo(details.m_nPublishedFileId, out _, out string folder, MAX_PATH, out uint timeStamp);
		if (!installed && !LoadedFromCache) {
			message = "   Addon needs downloading...\n";
			color = ColorInfo;
		}
		else {
			if (!installed)
				folder = details.m_pchFileName;

			uint itemState = SteamGameServerUGC.GetItemState(details.m_nPublishedFileId);
			if (LoadedFromCache)
				itemState = folder.Contains(".bin") ? (uint)EItemState.k_EItemStateLegacyItem : (uint)EItemState.k_EItemStateInstalled;
			else if ((itemState & (uint)(EItemState.k_EItemStateInstalled | EItemState.k_EItemStateNeedsUpdate)) == (uint)EItemState.k_EItemStateInstalled && timeStamp != details.m_rtimeUpdated)
				itemState |= (uint)EItemState.k_EItemStateNeedsUpdate;

			bool legacy = (itemState & (uint)EItemState.k_EItemStateLegacyItem) != 0;
			if (legacy && !g_FullFileSystem.FileExists(folder, "GAME")) {
				message = "   Addon file cache missing, redownloading...\n";
				color = ColorInfo;
			}
			else if ((itemState & (uint)EItemState.k_EItemStateNeedsUpdate) != 0 && AutoUpdate) {
				message = "   Addon downloaded, needs updating...\n";
				color = ColorInfo;
			}
			else {
				if ((itemState & (uint)EItemState.k_EItemStateNeedsUpdate) != 0)
					ConColorMsg(ColorInfo, "   Addon downloaded, needs updating, but autoupdates are disabled\n");

				if (!legacy) {
					ConColorMsg(ColorGood, "   Mounted!\n");
					MountSteamUGCAddon(in details, folder);
					RemoveAddon(workshopID);
					return;
				}

				IFileHandle? file = g_FullFileSystem.Open(cachePath, FileOpenOptions.Read | FileOpenOptions.Binary, "MOD");
				if (file == null) {
					message = "   Failed to open .gma, redownloading...\n";
					color = ColorHighlight;
				}
				else {
					Span<byte> timestamp = stackalloc byte[sizeof(uint)];
					timestamp.Clear();
					using (file) {
						file.Stream.Seek(AddonFormat.TimestampOffset, SeekOrigin.Begin);
						file.Stream.ReadAtLeast(timestamp, timestamp.Length, false);
					}

					if (BitConverter.ToUInt32(timestamp) == details.m_rtimeUpdated || !AutoUpdate) {
						MountAddon(in details, cachePath, true);
						ConColorMsg(ColorGood, "   Mounted!\n");
						RemoveAddon(workshopID);
						return;
					}

					message = "   Addon needs updating...\n";
					color = ColorHighlight;
				}
			}
		}

		ConColorMsg(color, message);

		bool done = false;
		bool success = false;
		EResult downloadResult = EResult.k_EResultNone;
		Callback<DownloadItemResult_t>? downloaded = null;

		if (!SteamGameServerUGC.DownloadItem(details.m_nPublishedFileId, true)) {
			Warning($"WS: Failed to start download for {workshopID}!\n");
			done = true;
		}
		else {
			downloaded = Callback<DownloadItemResult_t>.CreateGameServer(result => {
				if (result.m_nPublishedFileId.m_PublishedFileId != workshopID)
					return;

				done = true;
				success = result.m_eResult == EResult.k_EResultOK;
				downloadResult = result.m_eResult;
			});

			while (!done) {
				Thread.Sleep(DownloadWait);

				if (!SteamGameServerUGC.GetItemDownloadInfo(details.m_nPublishedFileId, out ulong bytesDownloaded, out ulong bytesTotal))
					Warning($"WS: GetItemDownloadInfo failed for {workshopID}!\n");

				BaseFileSystem.get.RunSteamCallbacks();

				if (bytesTotal != 0)
					ConColorMsg(ColorHighlight, $"   Downloading [ {bytesDownloaded * 100 / bytesTotal}% of {Bootil.String.Format.Memory(bytesTotal)} ]\n");
			}
		}

		downloaded?.Unregister();

		if (!success) {
			ConColorMsg(ColorError, $"   Download Failed! {SteamResult.ToString(downloadResult)}!\n");
			RemoveAddon(workshopID);
			return;
		}

		ConColorMsg(ColorGood, "   Downloaded!\n");

		if (!SteamGameServerUGC.GetItemInstallInfo(details.m_nPublishedFileId, out ulong size, out folder, MAX_PATH, out _)) {
			if (!LoadedFromCache) {
				ConColorMsg(ColorError, "   Error! Could not retrieve intall info\n");
				RemoveAddon(workshopID);
				return;
			}

			folder = details.m_pchFileName;
		}

		if (size == 0)
			size = (ulong)g_FullFileSystem.Size(folder, "GAME");

		if ((SteamGameServerUGC.GetItemState(details.m_nPublishedFileId) & (uint)EItemState.k_EItemStateLegacyItem) == 0) {
			MountSteamUGCAddon(in details, folder);
			ConColorMsg(ColorGood, "   Mounted!\n");
			RemoveAddon(workshopID);
			return;
		}

		byte[]? compressed = null;
		if (size != 0) {
			using IFileHandle? file = g_FullFileSystem.Open(folder, FileOpenOptions.Read | FileOpenOptions.Binary, "GAME");
			if (file != null) {
				compressed = new byte[file.Stream.Length];
				file.Stream.ReadExactly(compressed);
			}
		}

		if (compressed == null) {
			ConColorMsg(ColorError, "   Error! Failed to read the downloaded file\n");
			RemoveAddon(workshopID);
			return;
		}

		ConColorMsg(ColorHighlight, "   Extracting...\n");

		int compressedSize = (int)size;
		if (BitConverter.ToUInt32(compressed, compressedSize - sizeof(uint)) == AddonFormat.CompressionSignature)
			compressedSize -= 8;
		else
			ConColorMsg(ColorError, "   Warning! Addon was created with a 3rd party tool, which might cause install/load issues.\n");

		using MemoryStream extracted = new();
		LZMA.Extract(compressed.AsSpan(0, compressedSize), extracted);

		extracted.Seek(AddonFormat.TimestampOffset, SeekOrigin.Begin);
		extracted.Write(BitConverter.GetBytes(details.m_rtimeUpdated));

		using (IFileHandle? file = g_FullFileSystem.Open(cachePath, FileOpenOptions.Write | FileOpenOptions.Binary, "MOD"))
			file?.Stream.Write(extracted.GetBuffer(), 0, (int)extracted.Length);

		ConColorMsg(ColorHighlight, "   Mounting...\n");
		MountAddon(in details, cachePath, true);
		ConColorMsg(ColorGood, "   Mounted!\n");

		RemoveAddon(workshopID);
	}

	static void RemoveAddon(ulong workshopID) {
		int index = Addons.FindIndex(x => x.m_nPublishedFileId.m_PublishedFileId == workshopID);
		if (index != -1)
			Addons.RemoveAt(index);
	}

	static void MountSteamUGCAddon(in SteamUGCDetails_t details, string folder) {
		ReadOnlySpan<char> file = g_FullFileSystem.FindFirstEx($"{folder}/*.gma", "", out ulong findHandle);
		if (!file.IsEmpty)
			MountAddon(in details, $"{folder}/{file}", false);

		g_FullFileSystem.FindClose(findHandle);
	}

	static void MountAddon(in SteamUGCDetails_t details, string path, bool legacy) {
		IAddonSystem.Information info = new() {
			Title = details.m_rgchTitle,
			File = path,
			Tags = details.m_rgchTags,
			Failure = "",
			TimeUpdated = details.m_rtimeUpdated,
			Models = 0,
			WorkshopID = details.m_nPublishedFileId.m_PublishedFileId,
			Creator = details.m_ulSteamIDOwner,
			HContentFile = details.m_hFile.m_UGCHandle,
			Size = (uint)details.m_nFileSize,
			HContentPreview = details.m_hPreviewFile.m_UGCHandle,
			CanUpdate = false,
			Downloaded = true,
			Failed = false,
			Legacy = legacy || LoadedFromCache,
			Unloaded = false
		};

		g_FullFileSystem.Addons().AddAddon(in info);
	}

	static bool LoadCachedAddonList() {
		if (!g_FullFileSystem.FileExists("cfg/srcds_addons.txt", "DEFAULT_WRITE_PATH")) {
			ConColorMsg(ColorError, "WS: Would load workshop content from cache file, but it doesn't exist!\n");
			return false;
		}

		ConColorMsg(ColorError, "WS: Loading workshop content from cache file, disabling updates..\n");
		Addons.Clear();

		KeyValues kv = new("srcds_addons");
		if (!kv.LoadFromFile(g_FullFileSystem, "cfg/srcds_addons.txt", "DEFAULT_WRITE_PATH"))
			return false;

		for (KeyValues? sub = kv.GetFirstSubKey(); sub != null; sub = sub.GetNextKey()) {
			SteamUGCDetails_t details = default;
			details.m_nPublishedFileId = new PublishedFileId_t(Bootil.String.To.UInt64(sub.Name));
			details.m_eResult = EResult.k_EResultOK;
			details.m_rgchTitle = new string(sub.GetString("name", ""));
			details.m_pchFileName = new string(sub.GetString("path", ""));
			details.m_rgchTags = "";
			Addons.Add(details);
		}

		LoadedFromCache = true;
		AutoUpdate = false;
		return true;
	}

	static void CacheAddonList(List<SteamUGCDetails_t> addons) {
		if (LoadedFromCache)
			return;

		KeyValues kv = new("srcds_addons");
		foreach (SteamUGCDetails_t details in addons) {
			ulong workshopID = details.m_nPublishedFileId.m_PublishedFileId;
			if (!SteamGameServerUGC.GetItemInstallInfo(details.m_nPublishedFileId, out _, out string folder, MAX_PATH, out _)) {
				Warning($"Failed to cache addon location of {workshopID}\n");
				return;
			}

			KeyValues addon = kv.AddSubKey(new KeyValues(workshopID.ToString()));
			addon.SetString("name", details.m_rgchTitle);
			addon.SetString("path", folder);
		}

		kv.WriteToFile(g_FullFileSystem, "cfg/srcds_addons.txt", "DEFAULT_WRITE_PATH");
	}

	static void ClearUnusedGMAs() {
		ConColorMsg(ColorInfo, "WS: Cleaning up unused files in /cache/workshop/..\n");

		ReadOnlySpan<char> file = g_FullFileSystem.FindFirstEx("cache/workshop/*.gma", "MOD", out ulong findHandle);
		while (!file.IsEmpty) {
			string fileName = new(file);
			string workshopID = fileName;
			Bootil.String.File.StripExtension(ref workshopID);

			ulong id = Bootil.String.To.UInt64(workshopID);
			string path = "cache/workshop/" + fileName;
			if (id == 0) {
				ConColorMsg(ColorError, $"   Removing '{path}' - Invalid filename, must be Workshop ID\n");
				g_FullFileSystem.RemoveFile(path, "MOD");
			}
			else if ((SteamGameServerUGC.GetItemState(new PublishedFileId_t(id)) & (uint)EItemState.k_EItemStateInstalled) == 0) {
				ConColorMsg(ColorHighlight, $"   Removing '{path}' - Workshop item is no longer installed\n");
				g_FullFileSystem.RemoveFile(path, "MOD");
			}

			file = g_FullFileSystem.FindNext(findHandle);
		}

		g_FullFileSystem.FindClose(findHandle);
		ConColorMsg(ColorInfo, "WS: Finished cleanup!\n");
	}
}
