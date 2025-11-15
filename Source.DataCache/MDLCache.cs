using Source.Common;
using Source.Common.DataCache;

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Source.DataCache;

public enum StudioDataFlags : ushort
{
	StudioMeshLoaded = 0x0001,
	VCollisionLoaded = 0x0002,
	Model = 0x0004,
	NoStudioMesh = 0x0008,
	NoVertexData = 0x0010,
	VCollisionShared = 0x0020,
	LockedMDL = 0x0040,
}

public class StudioData {
	public MDLHandle_t Handle;

	public StudioHDR? Header = null;
	public readonly VCollide VCollisionData = new();
	public readonly StudioHWData HardwareData = new();

	public byte[]? VertexCache = null;

	public StudioDataFlags Flags;
	public int RefCount;
	public VirtualModel? VirtualModel;

	public object? UserData;
}

public class MDLCache : IMDLCache
{
	public int AddRef(MDLHandle_t handle) {
		return ++HandleToMDLDict[handle].RefCount;
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

	readonly ConcurrentDictionary<UtlSymId_t, StudioData> FileToMDLDict = [];
	readonly ConcurrentDictionary<UtlSymId_t, StudioData> HandleToMDLDict = [];
	MDLHandle_t curHandle;
	MDLHandle_t NewHandle() => Interlocked.Increment(ref curHandle);


	public MDLHandle_t FindMDL(ReadOnlySpan<char> mdlRelativePath) {
		Span<char> fixedName = stackalloc char[MAX_PATH];
		strcpy(fixedName, mdlRelativePath);
		StrTools.RemoveDotSlashes(fixedName, '/');

		UtlSymId_t fixedNameHash = fixedName.Hash();
		if (!FileToMDLDict.TryGetValue(fixedNameHash, out var info)) {
			info = InitStudioData(NewHandle());
			FileToMDLDict.TryAdd(fixedNameHash, info);
		}

		AddRef(info.Handle);
		return info.Handle;
	}

	private StudioData InitStudioData(MDLHandle_t handle) {
		StudioData studioData = new();
		studioData.Handle = handle;
		HandleToMDLDict.TryAdd(handle, studioData);
		return studioData;
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
		if (handle == MDLHANDLE_INVALID || !HandleToMDLDict.TryGetValue(handle, out StudioData? data))
			return false;

		switch (type) {
			case MDLCacheDataType.StudioHDR:
				return data.Header != null;
			case MDLCacheDataType.StudioHWData:
				return (data.Flags & StudioDataFlags.StudioMeshLoaded) != 0;
			case MDLCacheDataType.VCollide:
				return (data.Flags & StudioDataFlags.VCollisionLoaded) != 0;
			case MDLCacheDataType.AnimBlock: {
					return false; // todo
				}
			case MDLCacheDataType.VirtualModel:
				return data.VirtualModel != null;
			case MDLCacheDataType.Vertexes:
				return data.VertexCache != null;
		}

		return false;
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
		// NOTE: It can be null during shutdown because multiple studiomdls
		// could be referencing the same virtual model
		if (!HandleToMDLDict.TryGetValue(handle, out StudioData? data))
			return 0;

		Assert(data.RefCount > 0);

		int nRefCount = --data.RefCount;
		if (nRefCount <= 0) {
			ShutdownStudioData(handle);
			HandleToMDLDict.Remove(handle, out _);
		}

		return nRefCount;
	}

	private void ShutdownStudioData(uint handle) {
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
		if (handle == MDLHANDLE_INVALID)
			return;
		HandleToMDLDict[handle].UserData = (object?)data;
	}

	public void ShutdownPreloadData() {
		throw new NotImplementedException();
	}

	public void TouchAllData(MDLHandle_t handle) {
		StudioHDR? studioHdr = GetStudioHdr(handle);
		VirtualModel? vModel = GetVirtualModel(handle);
		if (vModel != null) {
			for (int i = 1; i < vModel.Group.Count; ++i) {
				// ????????????????
				MDLHandle_t childHandle = (MDLHandle_t)(nint)vModel.Group[i].Cache! & 0xffff;
				if (childHandle != MDLHANDLE_INVALID)
					GetStudioHdr(childHandle);
			}
		}

		for (int i = 1; i < studioHdr.NumAnimBlocks; ++i) {
			// studioHdr.GetAnimBlock(i);
		}

		// cache the vertexes
		if (studioHdr.NumBodyParts != 0) {
			CacheVertexData(studioHdr);
			GetHardwareData(handle);
		}
	}

	private void CacheVertexData(StudioHDR studioHdr) {
		// todo
	}

	public void UnlockStudioHdr(MDLHandle_t handle) {
		throw new NotImplementedException();
	}
}
