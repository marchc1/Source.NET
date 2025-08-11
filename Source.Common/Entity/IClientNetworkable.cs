using Source.Common.Networking.DataTable;

namespace Source.Common.Entity;

public enum DataUpdateType
{
	CREATED = 0,	// indicates it was created +and+ entered the pvs
//	ENTERED_PVS,
	DATATABLE_CHANGED,
//	LEFT_PVS,
//	DESTROYED,		// FIXME: Could enable this, but it's a little worrying
								// since it changes a bunch of existing code
};

public abstract class IClientNetworkable
{
	public abstract IClientUnknown? GetIClientUnknown();
	public abstract void Release();
	public abstract ClientClass? GetClientClass();
	public abstract void SetDestroyedOnRecreateEntities();
	public abstract void OnPreDataChanged(DataUpdateType updateType);
	public abstract void OnDataChanged(DataUpdateType updateType);

	// Called when data is being updated across the network.
	// Only low-level entities should need to know about these.
	public abstract void PreDataUpdate(DataUpdateType updateType);
	public abstract void PostDataUpdate(DataUpdateType updateType);
	public abstract byte[] GetDataTableBasePtr();
}
