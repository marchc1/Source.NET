using SevenZip.Buffer;

using Source.Common;
using Source.Common.Commands;
using Source.Common.DataCache;
using Source.Common.Filesystem;
using Source.Common.Utilities;

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Source.DataCache;

public enum StudioDataFlags : ushort
{
	StudioMeshLoaded = 0x0001,
	VCollisionLoaded = 0x0002,
	ErrorModel = 0x0004,
	NoStudioMesh = 0x0008,
	NoVertexData = 0x0010,
	VCollisionShared = 0x0020,
	LockedMDL = 0x0040,
}

public class StudioData
{
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

public class MDLCache(IFileSystem fileSystem) : IMDLCache
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
	readonly ConcurrentDictionary<MDLHandle_t, UtlSymbol> HandleToFileDict = [];
	readonly ConcurrentDictionary<MDLHandle_t, StudioData> HandleToMDLDict = [];
	MDLHandle_t curHandle;
	MDLHandle_t NewHandle() => Interlocked.Increment(ref curHandle);


	public MDLHandle_t FindMDL(ReadOnlySpan<char> mdlRelativePath) {
		Span<char> fixedName = stackalloc char[MAX_PATH];
		strcpy(fixedName, mdlRelativePath);
		StrTools.RemoveDotSlashes(fixedName, '/');
		fixedName = fixedName.SliceNullTerminatedString();

		UtlSymbol fixedNameHash = new(fixedName);
		if (!FileToMDLDict.TryGetValue(fixedNameHash, out var info)) {
			info = InitStudioData(NewHandle());
			FileToMDLDict.TryAdd(fixedNameHash, info);
			HandleToFileDict.TryAdd(info.Handle, fixedNameHash);
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
	const string ERROR_MODEL = "models/error.mdl";
	public StudioHDR? GetStudioHdr(MDLHandle_t handle) {
		if (handle == MDLHANDLE_INVALID)
			return null;

		StudioHDR? hdr = HandleToMDLDict[handle].Header;
		if (hdr == null) {
			ReadOnlySpan<char> modelName = GetActualModelName(handle);
			DevMsg($"Loading {modelName}\n");

			MemoryStream buf = new();
			if (!ReadMDLFile(handle, modelName, buf)) {
				bool ok = false;
				if ((HandleToMDLDict[handle].Flags & StudioDataFlags.ErrorModel) == 0) {
					buf.SetLength(0);

					HandleToMDLDict[handle].Flags |= StudioDataFlags.ErrorModel;
					ok = ReadMDLFile(handle, ERROR_MODEL, buf);
				}

				if (!ok) {
					Error($"Model {modelName} not found and {ERROR_MODEL} couldn't be loaded");
					return null;
				}
			}

			hdr = HandleToMDLDict[handle].Header;
		}

		return hdr;
	}

	public const int IDSTUDIOHEADER = (('T' << 24) + ('S' << 16) + ('D' << 8) + 'I');

	private bool ReadMDLFile(uint handle, ReadOnlySpan<char> mdlFileName, MemoryStream buf) {
		Span<char> fileName = stackalloc char[MAX_PATH];
		strcpy(fileName, mdlFileName);
		StrTools.FixSlashes(fileName);

		Msg($"MDLCache: Load studiohdr {fileName}\n");

		bool ok = ReadFileNative(fileName, "GAME", buf);
		if (!ok) {
			DevWarning($"Failed to load {mdlFileName}!\n");
			return false;
		}

		StudioHDR? studioHdr = ReinterpretDataToStudioHdr(buf);
		if (studioHdr == null) {
			DevWarning($"Failed to read model {mdlFileName} from buffer!\n");
			return false;
		}
		if (studioHdr.ID != IDSTUDIOHEADER) {
			DevWarning($"Model {mdlFileName} not a .MDL format file!\n");
			return false;
		}

		studioHdr.VirtualModel = handle;

		if (!VerifyHeaders(studioHdr)) {
			DevWarning($"Model {mdlFileName} has mismatched .vvd + .vtx files!\n");
			return false;
		}

		return true;
	}

	private bool VerifyHeaders(StudioHDR studioHdr) {
		throw new NotImplementedException();
	}

	private StudioHDR? ReinterpretDataToStudioHdr(MemoryStream buf) {

		StudioHDR header = new();
		using BinaryReader br = new(buf);
		header.ID = br.ReadInt32();
		header.Version = br.ReadInt32();
		header.Checksum = br.ReadInt32();
		br.ReadASCIIStringInto(header.Name);
		header.Length = br.ReadInt32();

		br.ReadInto(ref header.EyePosition);
		br.ReadInto(ref header.IllumPosition);
		br.ReadInto(ref header.HullMin);
		br.ReadInto(ref header.HullMax);
		br.ReadInto(ref header.ViewBoundingBoxMin);
		br.ReadInto(ref header.ViewBoundingBoxMax);

		header.Flags = (StudioHdrFlags)br.ReadInt32();

		header.NumBones = br.ReadInt32();
		header.BoneIndex = br.ReadInt32();
		header.NumBoneControllers = br.ReadInt32();
		header.BoneControllerIndex = br.ReadInt32();
		header.NumHitboxSets = br.ReadInt32();
		header.HitboxSetIndex = br.ReadInt32();
		header.NumLocalAnim = br.ReadInt32();
		header.LocalAnimIndex = br.ReadInt32();
		header.NumLocalSeq = br.ReadInt32();
		header.LocalSeqIndex = br.ReadInt32();
		header.ActivityListVersion = br.ReadInt32();
		header.EventsIndexed = br.ReadInt32();
		header.NumTextures = br.ReadInt32();
		header.TextureIndex = br.ReadInt32();

		header.NumCDTextures = br.ReadInt32();
		header.CDTextureIndex = br.ReadInt32();
		header.NumSkinRef = br.ReadInt32();
		header.NumSkinFamilies = br.ReadInt32();
		header.SkinIndex = br.ReadInt32();
		header.NumBodyParts = br.ReadInt32();
		header.BodyPartIndex = br.ReadInt32();
		header.NumLocalNodes = br.ReadInt32();
		header.LocalNodeIndex = br.ReadInt32();
		header.LocalNodeNameIndex = br.ReadInt32();
		header.NumFlexDesc = br.ReadInt32();
		header.FlexDescIndex = br.ReadInt32();
		header.NumFlexControllers = br.ReadInt32();
		header.FlexControllerIndex = br.ReadInt32();
		header.NumFlexRules = br.ReadInt32();
		header.FlexRuleIndex = br.ReadInt32();
		header.NumIKChains = br.ReadInt32();
		header.IKChainIndex = br.ReadInt32();
		header.NumMouths = br.ReadInt32();
		header.MouthIndex = br.ReadInt32();
		header.NumLocalPoseParameters = br.ReadInt32();
		header.LocalPoseParamIndex = br.ReadInt32();
		header.SurfacePropIndex = br.ReadInt32();
		header.KeyValueIndex = br.ReadInt32();
		header.KeyValueSize = br.ReadInt32();
		header.NumLocalIKAutoplayLocks = br.ReadInt32();
		header.LocalIKAutoplayLockIndex = br.ReadInt32();
		header.Mass = br.ReadSingle();
		header.Contents = br.ReadInt32();
		header.NumIncludeModels = br.ReadInt32();
		header.IncludeModelIndex = br.ReadInt32();

		header.SzAnimBlockNameIndex = br.ReadInt32();
		header.NumAnimBlocks = br.ReadInt32();
		header.AnimBlockIndex = br.ReadInt32();
		header.AnimBlockModel = br.ReadInt32();
		header.BoneTableByNameIndex = br.ReadInt32();
		header.VertexBase = br.ReadInt32();
		header.IndexBase = br.ReadInt32();
		header.ConstDirectionalLightDot = br.ReadByte();
		header.RootLOD = br.ReadByte();
		header.NumAllowedRootLODs = br.ReadByte();
		br.ReadNothing(1);
		header.StudioHDR2Index = br.ReadInt32();
		br.ReadNothing(1);

		return header;
	}

	private bool ReadFileNative(ReadOnlySpan<char> fileName, ReadOnlySpan<char> path, MemoryStream buf) {
		using var file = fileSystem.Open(fileName, FileOpenOptions.Read, path);
		if (file == null)
			return false;

		file.Stream.CopyTo(buf);
		buf.Position = 0;
		return true;
	}

	private ReadOnlySpan<char> GetActualModelName(MDLHandle_t handle) {
		if (handle == MDLHANDLE_INVALID)
			return ERROR_MODEL;

		if ((HandleToMDLDict[handle].Flags & StudioDataFlags.ErrorModel) != 0)
			return ERROR_MODEL;

		return HandleToFileDict[handle].String();
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
