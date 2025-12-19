using SevenZip.Buffer;

using Source.Common;
using Source.Common.Commands;
using Source.Common.DataCache;
using Source.Common.Filesystem;
using Source.Common.Utilities;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

	public StudioHeader? Header = null;
	public readonly VCollide VCollisionData = new();
	public readonly StudioHWData HardwareData = new();

	public VertexFileHeader? VertexCache = null;

	public StudioDataFlags Flags;
	public int RefCount;
	public VirtualModel? VirtualModel;

	public object? UserData;

	public Memory<short> AutoplaySequenceList;

	public int AnimBlockCount;
	public object? AnimBlock; // todo: research what this is
}

public class MDLCache(IFileSystem fileSystem) : IMDLCache, IStudioDataCache
{
	static readonly ConVar r_rootlod = new("r_rootlod", "0", FCvar.Archive, "Root LOD", 0, Studio.MAX_NUM_LODS);
	static readonly ConVar mod_forcedata = new("mod_forcedata", "0", 0, "Forces all model file data into cache on model load.");
	static readonly ConVar mod_test_not_available = new("mod_test_not_available", "0", FCvar.Cheat);
	static readonly ConVar mod_test_mesh_not_available = new("mod_test_mesh_not_available", "0", FCvar.Cheat);
	static readonly ConVar mod_test_verts_not_available = new("mod_test_verts_not_available", "0", FCvar.Cheat);
	// these do nothing for onw
	static readonly ConVar mod_load_mesh_async = new("mod_load_mesh_async", "0", 0);
	static readonly ConVar mod_load_anims_async = new("mod_load_anims_async", "0", 0);
	static readonly ConVar mod_load_vcollide_async = new("mod_load_vcollide_async", "0", 0);

	static readonly ConVar mod_trace_load = new("mod_trace_load", "0", 0);
	static readonly ConVar mod_lock_mdls_on_load = new("mod_lock_mdls_on_load", "0", 0);
	static readonly ConVar mod_load_fakestall = new("mod_load_fakestall", "0", 0, "Forces all ANI file loading to stall for specified ms\n");

	public int AddRef(MDLHandle_t handle) {
		return ++HandleToMDLDict[handle].RefCount;
	}

	public void BeginLock() {
		throw new NotImplementedException();
	}

	public void BeginMapLoad() {
		// TODO: Actually make locking do something, we're super synchronous right now
		foreach(var mdl in HandleToMDLDict) {
			if((mdl.Value.Flags & StudioDataFlags.LockedMDL) != 0) {
				mdl.Value.Flags &= ~StudioDataFlags.LockedMDL;
			}
		}
	}

	public void EndLock() {
		throw new NotImplementedException();
	}

	public void EndMapLoad() {
		FinishPendingLoads();
		// TODO: Remove stray MDL's
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

	}

	public void Flush(MDLCacheFlush flushFlags = MDLCacheFlush.All) {
		throw new NotImplementedException();
	}

	public void Flush(MDLHandle_t handle, MDLCacheFlush flushFlags = MDLCacheFlush.All) {
		throw new NotImplementedException();
	}

	public Memory<byte> GetAnimBlock(MDLHandle_t handle, int block) {
		throw new NotImplementedException();
	}

	public bool GetAsyncLoad(MDLCacheDataType type) {
		throw new NotImplementedException();
	}

	public Memory<short> GetAutoplayList(MDLHandle_t handle) {
		if (handle == MDLHANDLE_INVALID)
			return Array.Empty<short>();

		VirtualModel? virtualModel = GetVirtualModel(handle);
		if(virtualModel != null) 
			return virtualModel.AutoplaySequences.Base();

		StudioData studioData = HandleToMDLDict[handle];
		return studioData.AutoplaySequenceList;
	}

	public ref int GetFrameUnlockCounterPtr(MDLCacheDataType type) {
		throw new NotImplementedException();
	}

	public StudioHWData? GetHardwareData(MDLHandle_t handle) {
		StudioData studioData = HandleToMDLDict[handle];

		if ((studioData.Flags & (StudioDataFlags.StudioMeshLoaded | StudioDataFlags.NoStudioMesh)) == 0)
			if (!LoadHardwareData(handle))
				return null;

		if ((studioData.Flags & StudioDataFlags.NoStudioMesh) != 0)
			return null;

		return studioData.HardwareData;
	}

	private bool LoadHardwareData(MDLHandle_t handle) {
		StudioData studioData = HandleToMDLDict[handle];

		StudioHeader? studioHdr = GetStudioHdr(handle);
		if (studioHdr == null || studioHdr.NumBodyParts == 0) {
			studioData.Flags |= StudioDataFlags.NoStudioMesh;
			return true;
		}

		if ((studioData.Flags & StudioDataFlags.NoStudioMesh) != 0) {
			return false;
		}

		// Vertex data is required to call LoadModel(), so make sure that's ready
		if (GetVertexData(handle) == null) {
			if ((studioData.Flags & StudioDataFlags.NoVertexData) != 0)
				studioData.Flags |= StudioDataFlags.NoStudioMesh;

			return false;
		}

		Msg($"MDLCache: Begin load VTX {GetModelName(handle)}\n");
		return BuildHardwareData(handle, studioData, studioHdr);
	}
	IStudioRender? studioRender;
	IStudioRender StudioRender => studioRender ??= Singleton<IStudioRender>();
	private bool BuildHardwareData(uint handle, StudioData studioData, StudioHeader studioHdr) {
		Span<char> fileName = stackalloc char[MAX_PATH];
		MakeFilename(handle, GetVTXExtension(), fileName);
		fileName = fileName.SliceNullTerminatedString();

		MemoryStream vtxHeader = new();
		if (!ReadFileNative(fileName, "GAME", vtxHeader))
			return false;

		vtxHeader.Position = 0;
		OptimizedModel.FileHeader? vtxHdr = new(vtxHeader.GetBuffer());
		if (vtxHdr.Version != OptimizedModel.OPTIMIZED_MODEL_FILE_VERSION) {
			Warning($"Error Index File for '{studioHdr.GetName()}' version {vtxHdr.Version} should be {OptimizedModel.OPTIMIZED_MODEL_FILE_VERSION}\n");
			vtxHdr = null;
		}
		else if (vtxHdr.Checksum != studioHdr.Checksum) {
			Warning($"Error Index File for '{studioHdr.GetName()}' checksum {vtxHdr.Checksum} should be {studioHdr.Checksum}\n");
			vtxHdr = null;
		}

		if (vtxHdr == null) {
			studioData.Flags |= StudioDataFlags.NoStudioMesh;
			return false;
		}

		bool loaded = StudioRender.LoadModel(studioHdr, vtxHeader.GetBuffer(), studioData.HardwareData);

		if (loaded)
			studioData.Flags |= StudioDataFlags.StudioMeshLoaded;
		else
			studioData.Flags |= StudioDataFlags.NoStudioMesh;

		// todo: cacheNotify

		return true;
	}

	private ReadOnlySpan<char> GetVTXExtension() {
		return ".dx90.vtx";
	}

	public ReadOnlySpan<char> GetModelName(MDLHandle_t handle) {
		if (handle == MDLHANDLE_INVALID)
			return ERROR_MODEL;

		return HandleToFileDict[handle].String();
	}

	public int GetRef(MDLHandle_t handle) {
		throw new NotImplementedException();
	}
	const string ERROR_MODEL = "models/error.mdl";
	public StudioHeader? GetStudioHdr(MDLHandle_t handle) {
		if (handle == MDLHANDLE_INVALID)
			return null;

		StudioHeader? hdr = HandleToMDLDict[handle].Header;
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

	private bool ReadMDLFile(MDLHandle_t handle, ReadOnlySpan<char> mdlFileName, MemoryStream buf) {
		Span<char> fileName = stackalloc char[MAX_PATH];
		strcpy(fileName, mdlFileName);
		StrTools.FixSlashes(fileName);
		fileName = fileName.SliceNullTerminatedString();

		Msg($"MDLCache: Load studiohdr {fileName}\n");

		bool ok = ReadFileNative(fileName, "GAME", buf);
		if (!ok) {
			DevWarning($"Failed to load {mdlFileName}!\n");
			return false;
		}

		StudioHeader? studioHdr = ReinterpretDataToStudioHdr(buf);
		if (studioHdr == null) {
			DevWarning($"Failed to read model {mdlFileName} from buffer!\n");
			return false;
		}
		if (studioHdr.ID != IDSTUDIOHEADER) {
			DevWarning($"Model {mdlFileName} not a .MDL format file!\n");
			return false;
		}

		if(studioHdr.NumIncludeModels == 0) {
			int count = studioHdr.CountAutoplaySequences();
			if(count != 0) {
				AllocateAutoplaySequences(HandleToMDLDict[handle], count);
				studioHdr.CopyAutoplaySequences(ref HandleToMDLDict[handle].AutoplaySequenceList, count);
			}
		}

		HandleToMDLDict[handle].Header = studioHdr;
		studioHdr.VirtualModel = handle;

		if (!VerifyHeaders(studioHdr)) {
			DevWarning($"Model {mdlFileName} has mismatched .vvd + .vtx files!\n");
			return false;
		}

		return true;
	}

	private void AllocateAutoplaySequences(StudioData studioData, int count) {
		studioData.AutoplaySequenceList = new short[count];
	}

	private bool VerifyHeaders(StudioHeader studioHdr) {
		// Temporary todo
		return true;
	}

	private StudioHeader? ReinterpretDataToStudioHdr(MemoryStream buf) {
		// NOTE: We now assume that the buffer is locked.
		// TODO: Size check the memory stream before reading!!!

		// Justification for GetBuffer: If the buffer won't have any changes made to it, then we don't need
		// to store another copy of the byte data and can use the MemoryStream's array. When MemoryStream disposes,
		// the internal array will still remain ref'd by the Memory<byte>, so
		// Some operations in studio .mdl's require accessing data later on in the file beyond just the header data
		// and that's what's being set here
		StudioHeader header = new(buf.GetBuffer());
		SpanBinaryReader br = new(header.Data.Span);

		br.Read(out header.ID);
		br.Read(out header.Version);
		br.Read(out header.Checksum);
		br.ReadInto<byte>(header.Name);
		br.Read(out header.Length);

		br.Read(out header.EyePosition);
		br.Read(out header.IllumPosition);
		br.Read(out header.HullMin);
		br.Read(out header.HullMax);
		br.Read(out header.ViewBoundingBoxMin);
		br.Read(out header.ViewBoundingBoxMax);

		header.Flags = (StudioHdrFlags)br.Read<int>();

		br.Read(out header.NumBones);
		br.Read(out header.BoneIndex);
		br.Read(out header.NumBoneControllers);
		br.Read(out header.BoneControllerIndex);
		br.Read(out header.NumHitboxSets);
		br.Read(out header.HitboxSetIndex);
		br.Read(out header.NumLocalAnim);
		br.Read(out header.LocalAnimIndex);
		br.Read(out header.NumLocalSeq);
		br.Read(out header.LocalSeqIndex);
		br.Read(out header.ActivityListVersion);
		br.Read(out header.EventsIndexed);
		br.Read(out header.NumTextures);
		br.Read(out header.TextureIndex);

		br.Read(out header.NumCDTextures);
		br.Read(out header.CDTextureIndex);
		br.Read(out header.NumSkinRef);
		br.Read(out header.NumSkinFamilies);
		br.Read(out header.SkinIndex);
		br.Read(out header.NumBodyParts);
		br.Read(out header.BodyPartIndex);
		br.Read(out header.NumLocalAttachments);
		br.Read(out header.LocalAttachmentIndex);
		br.Read(out header.NumLocalNodes);
		br.Read(out header.LocalNodeIndex);
		br.Read(out header.LocalNodeNameIndex);
		br.Read(out header.NumFlexDesc);
		br.Read(out header.FlexDescIndex);
		br.Read(out header.NumFlexControllers);
		br.Read(out header.FlexControllerIndex);
		br.Read(out header.NumFlexRules);
		br.Read(out header.FlexRuleIndex);
		br.Read(out header.NumIKChains);
		br.Read(out header.IKChainIndex);
		br.Read(out header.NumMouths);
		br.Read(out header.MouthIndex);
		br.Read(out header.NumLocalPoseParameters);
		br.Read(out header.LocalPoseParamIndex);
		br.Read(out header.SurfacePropIndex);
		br.Read(out header.KeyValueIndex);
		br.Read(out header.KeyValueSize);
		br.Read(out header.NumLocalIKAutoplayLocks);
		br.Read(out header.LocalIKAutoplayLockIndex);
		br.Read(out header.Mass);
		br.Read(out header.Contents);
		br.Read(out header.NumIncludeModels);
		br.Read(out header.IncludeModelIndex);
		br.Advance(sizeof(int)); // Virtual model pointer
		br.Read(out header.SzAnimBlockNameIndex);
		br.Read(out header.NumAnimBlocks);
		br.Read(out header.AnimBlockIndex);
		br.Read(out header.AnimBlockModel);
		br.Read(out header.BoneTableByNameIndex);
		br.Read(out header.VertexBase);
		br.Read(out header.IndexBase);

		br.Read(out header.ConstDirectionalLightDot);
		br.Read(out header.RootLOD);
		br.Read(out header.NumAllowedRootLODs);
		br.Advance(sizeof(byte)); // Unused
		br.Advance(sizeof(int)); // Unused 4
		br.Read(out header.NumFlexControllerUI);
		br.Read(out header.FlexControllerUIIndex);
		br.Read(out header.VertAnimFixedPointScale);
		br.Advance(sizeof(int)); // Unused 3
		br.Read(out header.StudioHDR2Index);
		br.Advance(sizeof(int)); // Unused 2

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

	public VCollide? GetVCollideEx(MDLHandle_t handle, bool synchronousLoad = true) {
		if (handle == MDLHANDLE_INVALID)
			return null;

		StudioData studioData = HandleToMDLDict[handle];

		if ((studioData.Flags & StudioDataFlags.VCollisionLoaded) == 0)
			UnserializeVCollide(handle, synchronousLoad);

		if (studioData.VCollisionData.SolidCount == 0)
			return null;

		return studioData.VCollisionData;
	}

	private void UnserializeVCollide(MDLHandle_t handle, bool synchronousLoad) {
		StudioData studioData = HandleToMDLDict[handle];

		studioData.Flags &= ~StudioDataFlags.VCollisionLoaded;
		//studioData.VCollisionData.ClearInstantiatedReference();
		// ^^ Likely don't need this?
		{
			VirtualModel? virtualModel = GetVirtualModel(handle);
			if (virtualModel != null) {
				for (int i = 1; i < virtualModel.Group.Count; i++) {
					MDLHandle_t sharedHandle = (MDLHandle_t)virtualModel.Group[i].Cache!;
					StudioData data = HandleToMDLDict[sharedHandle];
					if ((data.Flags & StudioDataFlags.VCollisionLoaded) == 0)
						UnserializeVCollide(sharedHandle, synchronousLoad);

					if (data.VCollisionData.SolidCount > 0) {
						data.VCollisionData.CopyInstantiatedReferenceTo(studioData.VCollisionData);
						studioData.Flags |= StudioDataFlags.VCollisionShared;
						return;
					}
				}
			}
		}

		Span<char> fileName = stackalloc char[MAX_PATH];
		MakeFilename(handle, ".phy", fileName);
		fileName = fileName.SliceNullTerminatedString();
		bool asyncLoad = false;

		Msg($"MDLCache: {(asyncLoad ? "Async" : "Sync")} load vcollide {GetModelName(handle)}\n");
	}

	private void MakeFilename(MDLHandle_t handle, ReadOnlySpan<char> extension, Span<char> fileName) {
		strcpy(fileName, GetActualModelName(handle));
		StrTools.SetExtension(fileName, extension);
		StrTools.FixSlashes(fileName);
		StrTools.ToLower(fileName);
	}

	public bool GetVCollideSize(MDLHandle_t handle, out int VCollideSize) {
		throw new NotImplementedException();
	}

	public VertexFileHeader? GetVertexData(MDLHandle_t handle) {
		if (mod_test_not_available.GetBool())
			return null;

		if (mod_test_verts_not_available.GetBool())
			return null;

		return CacheVertexData(GetStudioHdr(handle));
	}

	public VirtualModel? GetVirtualModel(MDLHandle_t handle) {
		if (mod_test_not_available.GetBool())
			return null;

		if (handle == MDLHANDLE_INVALID)
			return null;

		StudioHeader? studioHdr = GetStudioHdr(handle);

		if (studioHdr == null)
			return null;

		return GetVirtualModelFast(studioHdr, handle);
	}

	public VirtualModel? GetVirtualModelFast(StudioHeader studioHdr, MDLHandle_t handle) {
		if (studioHdr.NumIncludeModels == 0)
			return null;

		if (!HandleToMDLDict.TryGetValue(handle, out StudioData? studioData))
			return null;

		if (studioData.VirtualModel == null) {
			DevMsg(2, $"Loading virtual model for {studioHdr.GetName()}\n");

			studioData.VirtualModel = AllocateVirtualModel(handle);

			// Group has to be zero to ensure refcounting is correct
			var group = studioData.VirtualModel.Group.Count;
			studioData.VirtualModel.Group.Add(new());

			Assert(group == 0);
			studioData.VirtualModel.Group[group].Cache = handle;

			// Add all dependent data
			studioData.VirtualModel.AppendModels(0, studioHdr);
		}

		return studioData.VirtualModel;
	}

	private VirtualModel AllocateVirtualModel(MDLHandle_t handle) {
		StudioData studioData = HandleToMDLDict[handle];
		Assert(studioData.VirtualModel == null);
		studioData.VirtualModel = new VirtualModel();

		Assert(studioData.AnimBlockCount == 0);
		Assert(studioData.AnimBlock == null);

		return studioData.VirtualModel;
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

	public StudioHeader? LockStudioHdr(MDLHandle_t handle) {
		if (handle == MDLHANDLE_INVALID)
			return null;

		StudioHeader? pStdioHdr = GetStudioHdr(handle);
		if (pStdioHdr == null)
			return null;

		return pStdioHdr;
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

	private void ShutdownStudioData(MDLHandle_t handle) {
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
		StudioHeader? studioHdr = GetStudioHdr(handle);
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
			studioHdr.GetAnimBlock(i);
		}

		// cache the vertexes
		if (studioHdr.NumBodyParts != 0) {
			CacheVertexData(studioHdr);
			GetHardwareData(handle);
		}
	}

	public VertexFileHeader? CacheVertexData(StudioHeader? studioHdr) {
		VertexFileHeader? vvdHdr;
		Assert(studioHdr != null);
		MDLHandle_t handle = studioHdr.VirtualModel;


		Assert(handle != MDLHANDLE_INVALID);

		vvdHdr = HandleToMDLDict[handle].VertexCache;
		if (vvdHdr != null)
			return vvdHdr;

		HandleToMDLDict[handle].VertexCache = null;

		return LoadVertexData(studioHdr);
	}

	private VertexFileHeader? LoadVertexData(StudioHeader studioHdr) {
		Span<char> fileName = stackalloc char[MAX_PATH];
		MDLHandle_t handle = studioHdr.VirtualModel;
		Assert(HandleToMDLDict[handle].VertexCache == null);

		StudioData studioData = HandleToMDLDict[handle];

		if ((studioData.Flags & StudioDataFlags.NoVertexData) != 0)
			return null;

		// load the VVD file
		// use model name for correct path
		MakeFilename(handle, ".vvd", fileName);
		fileName = fileName.SliceNullTerminatedString();

		Msg($"MDLCache: Begin load VVD {fileName}\n");
		MemoryStream vvdHeader = new();
		if (!ReadFileNative(fileName, "GAME", vvdHeader))
			return null;

		vvdHeader.Position = 0;
		HandleToMDLDict[handle].VertexCache = ReadVertices(vvdHeader);
		return HandleToMDLDict[handle].VertexCache;
	}

	private VertexFileHeader ReadVertices(MemoryStream ms) {
		// Justification for GetBuffer: If the buffer won't have any changes made to it, then we don't need
		// to store another copy of the byte data and can use the MemoryStream's array. When MemoryStream disposes,
		// the internal array will still remain ref'd by the Memory<byte>.
		VertexFileHeader vvdHeader = new(ms.GetBuffer());
		// TODO: Can we simplify reading the .mdl into a constructor like this?

		return vvdHeader;
	}

	public void UnlockStudioHdr(MDLHandle_t handle) {

	}
}
