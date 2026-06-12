using Source.Common.Bitbuffers;

namespace Source.Common;

public enum ShouldTransmiteState {
	Start,
	End
}

public enum DataUpdateType {
	Created,
	DataTableChanged
}

public enum InPVS : byte
{
	Yes = 0x0001,
	ThisFrame = 0x0002,
	NeedsNotify = 0x0004
}

public interface IClientNetworkable {
	IClientUnknown GetIClientUnknown();
	void Release();
	ClientClass GetClientClass();
	void NotifyShouldTransmit(ShouldTransmiteState state);
	void OnPreDataChanged(DataUpdateType updateType);
	void OnDataChanged(DataUpdateType updateType);
	void PreDataUpdate(DataUpdateType updateType);
	void PostDataUpdate(DataUpdateType updateType);
	bool IsDormant();
	int EntIndex();
	object GetDataTableBasePtr();
	void ReceiveMessage(int classID, bf_read msg);
	void SetDestroyedOnRecreateEntities();
	void OnDataUnchangedInPVS();
}
