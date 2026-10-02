namespace Source.Common.GarrysMod;

public interface IAddonDownloadNotification{
	void Start(); // Calls GM:WorkshopStart
	void StartDownload(ulong wsid, ulong imgid, ReadOnlySpan<char> title, ulong size ); // Calls GM:WorkshopDownloadFile
	void FinishDownload(ulong wsid); // Calls GM:WorkshopDownloadedFile
	void Finish(); // Calls GM:WorkshopEnd
	void DownloadProgress(ulong wsid, ulong imgid, ReadOnlySpan<char> title, uint unk1, uint unk2); // Calls GM:WorkshopDownloadProgress
	void ExtractProgress(ulong wsid, ulong imgid, ReadOnlySpan<char> title, uint percent ); // Calls GM:WorkshopExtractProgress
	void DownloadTotals(int num, int max); // Calls GM:WorkshopDownloadTotals
	void SubscriptionsProgress(int num, int max); // Calls GM:WorkshopSubscriptionsProgress
	void SendMessage(Span<char> message); // Calls GM:WorkshopSubscriptionsMessage
	void NotifySubscriptionChanges(); // Calls GM:WorkshopSubscriptionsChanged
	void NotifyAddonConflict(ulong wsid1, ulong wsid2, ReadOnlySpan<char> filename ); // Calls GM:OnNotifyAddonConflict
}
