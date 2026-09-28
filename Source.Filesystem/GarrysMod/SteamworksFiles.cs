using Source.FileSystem;

using Steamworks;

namespace Source.Filesystem.GarrysMod;

public class SteamworksSubscribedFiles
{
	const uint PageSize = 800;

	public uint Page;
	public uint Received;
	public bool Querying;
	public bool Succeeded;
	public int Pending;
	public readonly LinkedList<SteamUGCDetails_t> Details = [];
	public uint Remaining;
	public uint Total;
	public PublishedFileId_t[] Items = [];
	readonly CallResult<SteamUGCQueryCompleted_t> QueryCompleted;

	public SteamworksSubscribedFiles() {
		QueryCompleted = CallResult<SteamUGCQueryCompleted_t>.Create(OnQueryCompleted);
	}

	public bool IsDone() => Pending == 0 && !Querying;

	public bool Start() {
		if (Querying || Pending != 0)
			return false;

		Querying = true;
		Page = 0;
		Received = 0;
		Details.Clear();

		Total = SteamUGC.GetNumSubscribedItems();
		Remaining = Total;
		Items = new PublishedFileId_t[Total];

		uint count = SteamUGC.GetSubscribedItems(Items, Total);
		if (count != Total)
			Warning($"SubscribedFiles: File count mismatch! Got {count} vs {Total} expected\n");

		RequestPage();
		return true;
	}

	void Finish() => Querying = false;

	void RequestPage() {
		uint count = Math.Min(Total - Page * PageSize, PageSize);
		Remaining -= count;

		UGCQueryHandle_t query = SteamUGC.CreateQueryUGCDetailsRequest(Items[(int)(Page * PageSize)..], count);
		if (query == UGCQueryHandle_t.Invalid) {
			Warning("Failed to CreateQueryUserUGCRequest for subscription list\n");
			Finish();
			return;
		}

		SteamAPICall_t call = SteamUGC.SendQueryUGCRequest(query);
		if (call == SteamAPICall_t.Invalid) {
			Warning("Failed to SendQueryUGCRequest for subscription list\n");
			Finish();
			return;
		}

		QueryCompleted.Set(call);
		Page++;
		Pending++;
	}

	void OnQueryCompleted(SteamUGCQueryCompleted_t result, bool ioFailure) {
		Pending--;

		if (ioFailure || result.m_eResult != EResult.k_EResultOK) {
			Warning($"Error getting subcriptions list! Error: {Source.Common.GarrysMod.SteamResult.ToString(result.m_eResult)}\n");
			BaseFileSystem.get.MenuSystem()!.SendProblemToMenu("subscriptions_failed", 1, "");
			SteamUGC.ReleaseQueryUGCRequest(result.m_handle);
			Finish();
			return;
		}

		for (uint i = 0; i < result.m_unNumResultsReturned; i++) {
			if (!SteamUGC.GetQueryUGCResult(result.m_handle, i, out SteamUGCDetails_t details)) {
				Warning("SubscribedFiles: GetQueryUGCResult failed!\n");
				continue;
			}

			Received++;
			Details.AddLast(details);
		}

		if (Remaining != 0)
			RequestPage();

		SteamUGC.ReleaseQueryUGCRequest(result.m_handle);

		if (Pending == 0) {
			Succeeded = true;
			Finish();
		}
	}
}

public class SteamworksFileDetailsRequest
{
	public bool Failed;
	public SteamUGCDetails_t Details;
	readonly Action<SteamworksFileDetailsRequest> Callback;
	readonly CallResult<SteamUGCRequestUGCDetailsResult_t> DetailsResult;

	public SteamworksFileDetailsRequest(ulong workshopID, Action<SteamworksFileDetailsRequest> callback) {
		Callback = callback;
		DetailsResult = CallResult<SteamUGCRequestUGCDetailsResult_t>.Create(OnRequestUGCDetails);

		SteamAPICall_t call = SteamUGC.RequestUGCDetails(new PublishedFileId_t(workshopID), 0);
		if (call == SteamAPICall_t.Invalid)
			Failed = true;
		else
			DetailsResult.Set(call);
	}

	void OnRequestUGCDetails(SteamUGCRequestUGCDetailsResult_t result, bool ioFailure) {
		if (ioFailure || result.m_details.m_eResult != EResult.k_EResultOK)
			Failed = true;

		Details = result.m_details;
		Callback(this);
		DetailsResult.Cancel();
	}
}
