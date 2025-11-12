using Source.Common;
using Source.Common.DataCache;

namespace Source.DataCache;

public class MDLCache : IMDLCache
{
	public int AddRef(uint handle) {
		throw new NotImplementedException();
	}

	public void BeginLock() {
		throw new NotImplementedException();
	}

	public void BeginMapLoad() {
		throw new NotImplementedException();
	}

	public void EndLock() {
		throw new NotImplementedException();
	}

	public void EndMapLoad() {
		throw new NotImplementedException();
	}

	public uint FindMDL(ReadOnlySpan<char> mdlRleativePath) {
		throw new NotImplementedException();
	}

	public void FinishPendingLoads() {
		throw new NotImplementedException();
	}

	public void Flush(MDLCacheFlush flushFlags = MDLCacheFlush.All) {
		throw new NotImplementedException();
	}

	public void Flush(uint handle, MDLCacheFlush flushFlags = MDLCacheFlush.All) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetAnimBlock(uint handle, int block) {
		throw new NotImplementedException();
	}

	public bool GetAsyncLoad(MDLCacheDataType type) {
		throw new NotImplementedException();
	}

	public ref int GetFrameUnlockCounterPtr(MDLCacheDataType type) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetModelName(uint handle) {
		throw new NotImplementedException();
	}

	public int GetRef(uint handle) {
		throw new NotImplementedException();
	}

	public StudioHDR GetStudioHdr(uint handle) {
		throw new NotImplementedException();
	}

	public T? GetUserData<T>(uint handle) {
		throw new NotImplementedException();
	}

	public VCollide GetVCollide(uint handle) {
		throw new NotImplementedException();
	}

	public VCollide GetVCollideEx(uint handle, bool synchronousLoad = true) {
		throw new NotImplementedException();
	}

	public bool GetVCollideSize(uint handle, out int VCollideSize) {
		throw new NotImplementedException();
	}

	public VirtualModel GetVirtualModel(uint handle) {
		throw new NotImplementedException();
	}

	public VirtualModel GetVirtualModelFast(StudioHDR studioHdr, uint handle) {
		throw new NotImplementedException();
	}

	public void InitPreloadData(bool rebuild) {
		throw new NotImplementedException();
	}

	public bool IsDataLoaded(uint handle, MDLCacheDataType type) {
		throw new NotImplementedException();
	}

	public bool IsErrorModel(uint handle) {
		throw new NotImplementedException();
	}

	public StudioHDR LockStudioHdr(uint handle) {
		throw new NotImplementedException();
	}

	public void MarkAsLoaded(uint handle) {
		throw new NotImplementedException();
	}

	public void MarkFrame() {
		throw new NotImplementedException();
	}

	public bool PreloadModel(uint handle) {
		throw new NotImplementedException();
	}

	public int Release(uint handle) {
		throw new NotImplementedException();
	}

	public void ResetErrorModelStatus(uint handle) {
		throw new NotImplementedException();
	}

	public bool SetAsyncLoad(MDLCacheDataType type, bool bAsync) {
		throw new NotImplementedException();
	}

	public void SetCacheNotify(IMDLCacheNotify notify) {
		throw new NotImplementedException();
	}

	public void SetUserData<T>(uint handle, T? data) {
		throw new NotImplementedException();
	}

	public void ShutdownPreloadData() {
		throw new NotImplementedException();
	}

	public void TouchAllData(uint handle) {
		throw new NotImplementedException();
	}

	public void UnlockStudioHdr(uint handle) {
		throw new NotImplementedException();
	}
}
