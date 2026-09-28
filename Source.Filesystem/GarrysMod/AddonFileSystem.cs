using Source.Common.GarrysMod;

using Steamworks;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Filesystem.GarrysMod;

public class AddonFileSystem : Addon.FileSystem
{
	public void AddFile(ref IAddonSystem.Information info) {
		throw new NotImplementedException();
	}

	public void AddFile(in SteamUGCDetails_t unk1) {
		throw new NotImplementedException();
	}

	public void AddJob<T>(T job) where T : Addon.Job.Base {
		throw new NotImplementedException();
	}

	public void AddonDownloaded(ref IAddonSystem.Information info) {
		throw new NotImplementedException();
	}

	public void AddSubscription(in SteamUGCDetails_t unk1) {
		throw new NotImplementedException();
	}

	public void Clear() {
		throw new NotImplementedException();
	}

	public void ClearAllGMAs() {
		throw new NotImplementedException();
	}

	public ref readonly IAddonSystem.Information FindFileOwner(ReadOnlySpan<char> unk1) {
		throw new NotImplementedException();
	}

	public List<IAddonSystem.Information> GetList() {
		throw new NotImplementedException();
	}

	public string GetSteamUGCFile(ulong workshopID, bool unk1) {
		if (SteamUGC.GetItemInstallInfo(new PublishedFileId_t(workshopID), out _, out string folder, 260, out _)) {
			ReadOnlySpan<char> file = g_FullFileSystem.FindFirstEx($"{folder}/*.*", "", out ulong findHandle);
			while (!file.IsEmpty) {
				string ext = Path.GetExtension(new string(file)).TrimStart('.');
				if (ext == "gma" || !unk1) {
					if (ext == "gma" || ext == "dupe" || ext == "gms" || ext == "dem") {
						g_FullFileSystem.FindClose(findHandle);
						return $"{folder}/{file}";
					}
				}

				file = g_FullFileSystem.FindNext(findHandle);
			}

			g_FullFileSystem.FindClose(findHandle);
		}

		return "";
	}

	public List<SteamUGCDetails_t> GetSubList() {
		throw new NotImplementedException();
	}

	public List<IAddonSystem.UGCInfo> GetUGCList() {
		throw new NotImplementedException();
	}

	public bool HasChanges() {
		throw new NotImplementedException();
	}

	public string IsAddonValidPreInstall(in SteamUGCDetails_t details) {
		if (details.m_bBanned)
			return "Addon is banned";

		if (details.m_eFileType != EWorkshopFileType.k_EWorkshopFileTypeCommunity)
			return "Bad workshop file type";

		if (details.m_rgchTitle.Length < 1)
			return "Addon is hidden, banned or doesn't exist";

		if (details.m_nConsumerAppID.m_AppId != 4000)
			return "Bad consumer AppID";

		if ((SteamUGC.GetItemState(details.m_nPublishedFileId) & (uint)EItemState.k_EItemStateLegacyItem) != 0 && details.m_rtimeCreated > 0x5e3351e0)
			return "Addon too new to use old API";

		return "";
	}

	public bool IsSubscribed(ulong workshopID) {
		throw new NotImplementedException();
	}

	public void Load() {
		throw new NotImplementedException();
	}

	public void MarkChanged() {
		throw new NotImplementedException();
	}

	public int MountFile(ReadOnlySpan<char> file, List<string>? files, ulong workshopID, ulong unk1, int unk2) {
		throw new NotImplementedException();
	}

	public void MountFloatingAddons() {
		throw new NotImplementedException();
	}

	public int Notify() {
		throw new NotImplementedException();
	}

	public void NotifyAddonFailedToDownload(ref IAddonSystem.Information info) {
		throw new NotImplementedException();
	}

	public void Refresh() {

	}

	public void Save() {
		throw new NotImplementedException();
	}

	public void ScanForSubscriptions(ReadOnlySpan<char> unk1) {
		throw new NotImplementedException();
	}

	public void SetDownloadNotify(IAddonDownloadNotification unk1) {
		throw new NotImplementedException();
	}

	public void SetShouldMount(ReadOnlySpan<char> unk1, bool unk2) {
		throw new NotImplementedException();
	}

	public bool ShouldMount(ReadOnlySpan<char> unk1) {
		throw new NotImplementedException();
	}

	public bool ShouldMount(ulong unk1) {
		throw new NotImplementedException();
	}

	public void Shutdown() {
		throw new NotImplementedException();
	}

	public void Think() {
		throw new NotImplementedException();
	}

	public void UnmountAddon(ulong workshopID, ReadOnlySpan<char> reason) {
		throw new NotImplementedException();
	}

	public void UnmountServerAddons() {
		throw new NotImplementedException();
	}
}
