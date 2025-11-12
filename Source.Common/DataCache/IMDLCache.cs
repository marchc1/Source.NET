namespace Source.Common.DataCache;

public enum MDLCacheDataType
{
	StudioHDR,
	StudioHWData,
	VCollide,

	AnimBlock,
	VirtualModel,
	Vertexes,
	DecodedAnimBlock
}

public interface IMDLCacheNotify
{
	void OnDataLoaded(MDLCacheDataType type, MDLHandle_t handle);
	void OnDataUnloaded(MDLCacheDataType type, MDLHandle_t handle);
}

public enum MDLCacheFlush : uint
{
	StudioHDR = 0x01,
	StudioHWData = 0x02,
	VCollide = 0x04,
	AnimBlock = 0x08,
	VirtualModel = 0x10,
	Autoplay = 0x20,
	Vertexes = 0x40,

	IgnoreLock = 0x80000000,
	All = 0xFFFFFFFF
}

public interface IMDLCache
{
	void SetCacheNotify(IMDLCacheNotify notify);
	MDLHandle_t FindMDL(ReadOnlySpan<char> mdlRleativePath);

	int AddRef(MDLHandle_t handle);
	int Release(MDLHandle_t handle);
	int GetRef(MDLHandle_t handle);

	StudioHDR GetStudioHdr(MDLHandle_t handle);
	// StudioHWData GetHardwareData(MDLHandle_t handle);
	VCollide GetVCollide(MDLHandle_t handle);
	ReadOnlySpan<char> GetAnimBlock(MDLHandle_t handle, int block);
	VirtualModel GetVirtualModel(MDLHandle_t handle);
	// int GetAutoplayList(MDLHandle_t handle, unsigned short** pOut);
	// VertexFileHeader GetVertexData(MDLHandle_t handle);

	void TouchAllData(MDLHandle_t handle);

	void SetUserData<T>(MDLHandle_t handle, T? data);
	T? GetUserData<T>(MDLHandle_t handle);

	bool IsErrorModel(MDLHandle_t handle);

	void Flush(MDLCacheFlush flushFlags = MDLCacheFlush.All);
	void Flush(MDLHandle_t handle, MDLCacheFlush flushFlags = MDLCacheFlush.All);

	ReadOnlySpan<char> GetModelName(MDLHandle_t handle);

	VirtualModel GetVirtualModelFast(StudioHDR studioHdr, MDLHandle_t handle);

	void BeginLock();
	void EndLock();
	void FinishPendingLoads();

	VCollide GetVCollideEx(MDLHandle_t handle, bool synchronousLoad = true);
	bool GetVCollideSize(MDLHandle_t handle, out int VCollideSize);

	bool GetAsyncLoad(MDLCacheDataType type);
	bool SetAsyncLoad(MDLCacheDataType type, bool bAsync);

	void BeginMapLoad();
	void EndMapLoad();
	void MarkAsLoaded(MDLHandle_t handle);

	void InitPreloadData(bool rebuild);
	void ShutdownPreloadData();

	bool IsDataLoaded(MDLHandle_t handle, MDLCacheDataType type);

	ref int GetFrameUnlockCounterPtr(MDLCacheDataType type);

	StudioHDR LockStudioHdr(MDLHandle_t handle);
	void UnlockStudioHdr(MDLHandle_t handle);

	bool PreloadModel(MDLHandle_t handle);

	void ResetErrorModelStatus(MDLHandle_t handle);

	void MarkFrame();
}
