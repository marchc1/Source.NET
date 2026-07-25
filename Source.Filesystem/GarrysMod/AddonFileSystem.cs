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

	public void GetSteamUGCFile(ulong workshopID, bool unk1) {
		throw new NotImplementedException();
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

	public void IsAddonValidPreInstall(SteamUGCDetails_t unk1) {
		throw new NotImplementedException();
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

	public int MountFile(ReadOnlySpan<char> unk1, List<string> unk2) {
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

	public void UnmountAddon(ulong workshopID) {
		throw new NotImplementedException();
	}

	public void UnmountServerAddons() {
		throw new NotImplementedException();
	}
}
