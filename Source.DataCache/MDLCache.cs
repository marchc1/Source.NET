using Source.Common;
using Source.Common.DataCache;

namespace Source.DataCache;

public class MDLCache : IMDLCache
{
	public int AddRef(MDLHandle_t handle) {
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

	public MDLHandle_t FindMDL(ReadOnlySpan<char> mdlRleativePath) {
		throw new NotImplementedException();
	}

	public void FinishPendingLoads() {
		throw new NotImplementedException();
	}

	public void Flush(MDLCacheFlush flushFlags = MDLCacheFlush.All) {
		throw new NotImplementedException();
	}

	public void Flush(MDLHandle_t handle, MDLCacheFlush flushFlags = MDLCacheFlush.All) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetAnimBlock(MDLHandle_t handle, int block) {
		throw new NotImplementedException();
	}

	public bool GetAsyncLoad(MDLCacheDataType type) {
		throw new NotImplementedException();
	}

	public short[] GetAutoplayList(uint handle) {
		throw new NotImplementedException();
	}

	public ref int GetFrameUnlockCounterPtr(MDLCacheDataType type) {
		throw new NotImplementedException();
	}

	public StudioHWData GetHardwareData(uint handle) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetModelName(MDLHandle_t handle) {
		throw new NotImplementedException();
	}

	public int GetRef(MDLHandle_t handle) {
		throw new NotImplementedException();
	}

	public StudioHDR GetStudioHdr(MDLHandle_t handle) {
		throw new NotImplementedException();
	}

	public T? GetUserData<T>(MDLHandle_t handle) {
		throw new NotImplementedException();
	}

	public VCollide GetVCollide(MDLHandle_t handle) {
		throw new NotImplementedException();
	}

	public VCollide GetVCollideEx(MDLHandle_t handle, bool synchronousLoad = true) {
		throw new NotImplementedException();
	}

	public bool GetVCollideSize(MDLHandle_t handle, out int VCollideSize) {
		throw new NotImplementedException();
	}

	public VertexFileHeader GetVertexData(uint handle) {
		throw new NotImplementedException();
	}

	public VirtualModel GetVirtualModel(MDLHandle_t handle) {
		throw new NotImplementedException();
	}

	public VirtualModel GetVirtualModelFast(StudioHDR studioHdr, MDLHandle_t handle) {
		throw new NotImplementedException();
	}

	public void InitPreloadData(bool rebuild) {
		throw new NotImplementedException();
	}

	public bool IsDataLoaded(MDLHandle_t handle, MDLCacheDataType type) {
		throw new NotImplementedException();
	}

	public bool IsErrorModel(MDLHandle_t handle) {
		throw new NotImplementedException();
	}

	public StudioHDR LockStudioHdr(MDLHandle_t handle) {
		throw new NotImplementedException();
	}

	public void MarkAsLoaded(MDLHandle_t handle) {
		throw new NotImplementedException();
	}

	public void MarkFrame() {
		throw new NotImplementedException();
	}

	public bool PreloadModel(MDLHandle_t handle) {
		throw new NotImplementedException();
	}

	public int Release(MDLHandle_t handle) {
		throw new NotImplementedException();
	}

	public void ResetErrorModelStatus(MDLHandle_t handle) {
		throw new NotImplementedException();
	}

	public bool SetAsyncLoad(MDLCacheDataType type, bool async) {
		throw new NotImplementedException();
	}

	public void SetCacheNotify(IMDLCacheNotify notify) {
		throw new NotImplementedException();
	}

	public void SetUserData<T>(MDLHandle_t handle, T? data) {
		throw new NotImplementedException();
	}

	public void ShutdownPreloadData() {
		throw new NotImplementedException();
	}

	public void TouchAllData(MDLHandle_t handle) {
		throw new NotImplementedException();
	}

	public void UnlockStudioHdr(MDLHandle_t handle) {
		throw new NotImplementedException();
	}
}
