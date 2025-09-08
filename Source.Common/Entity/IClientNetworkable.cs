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

public interface IClientNetworkable
{
	public IClientUnknown? GetIClientUnknown();
	public void Release();
	// public ClientClass? GetClientClass();
	public void SetDestroyedOnRecreateEntities();
	public void OnPreDataChanged(DataUpdateType updateType);
	public void OnDataChanged(DataUpdateType updateType);

	// Called when data is being updated across the network.
	// Only low-level entities should need to know about these.
	public void PreDataUpdate(DataUpdateType updateType);
	public void PostDataUpdate(DataUpdateType updateType);
	public IntPtr GetDataTableBasePtr();
	public void Init(int entityNum, int serialNum);
}
