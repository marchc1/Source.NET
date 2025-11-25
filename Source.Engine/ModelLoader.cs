using CommunityToolkit.HighPerformance;

using Source.Common;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.DataCache;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Formats.BSP;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Common.Utilities;

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Xml.Linq;
namespace Source.Engine;

public ref struct MapLoadHelper
{
	internal static WorldBrushData? Map;
	internal static string? LoadName;
	internal static string? MapName;
	internal static Stream? MapFileHandle;
	internal static BSPHeader MapHeader;
	static Host? Host;
	public static bool Init(Model? model, ReadOnlySpan<char> loadName) {
		Host = Singleton<Host>();
		ModelLoader ModelLoader = (ModelLoader)Singleton<IModelLoader>();
		IFileSystem fileSystem = Singleton<IFileSystem>();

		Map = null;
		LoadName = null;
		MapFileHandle = null;

		if (model == null)
			MapName = new(loadName);
		else
			MapName = model.StrName.String();

		MapFileHandle = fileSystem.Open(loadName, FileOpenOptions.Read | FileOpenOptions.Binary)?.Stream;
		if (MapFileHandle == null) {
			Host.Error($"MapLoadHelper.Init, unable to open {MapName}");
			return false;
		}

		if (!MapFileHandle.ReadToStruct(ref MapHeader) || MapHeader.Identifier != BSPFileCommon.IDBSPHEADER) {
			MapFileHandle.Close();
			MapFileHandle = null;
			Host.Error($"MapLoadHelper.Init, map {MapName} has wrong identifier\n");
			return false;
		}

		if (MapHeader.Version < BSPFileCommon.MINBSPVERSION || MapHeader.Version > BSPFileCommon.BSPVERSION) {
			MapFileHandle.Close();
			MapFileHandle = null;
			Host.Error($"MapLoadHelper.Init, map {MapName} has wrong version ({MapHeader.Version} when expecting {BSPFileCommon.BSPVERSION})\n");
			return false;
		}

		LoadName = new(loadName);

#if !SWDS
		InitDLightGlobals(MapHeader.Version);
#endif
		Map = ModelLoader.WorldBrushData;

		Assert(MapFileHandle != null);
		return true;
	}

	public static void Shutdown() {
		if (MapFileHandle != null) {
			MapFileHandle.Close();
			MapFileHandle = null;
		}

		LoadName = null;
		MapName = null;
		memreset(ref MapHeader);
		Map = null;
	}

	private static void InitDLightGlobals(int version) {

	}

	public readonly WorldBrushData GetMap() => Map!;

	public readonly LumpIndex LumpID;
	public readonly int LumpSize;
	public readonly int LumpOffset;
	public readonly int LumpVersion;

	public static nint GetLumpSize(LumpIndex lumpId) {
		ref BSPLump lump = ref MapHeader.Lumps[(int)lumpId];
		int uncompressedSize = lump.UncompressedSize;
		return uncompressedSize != 0 ? uncompressedSize : lump.FileLength;
	}
	public MapLoadHelper(LumpIndex lumpToLoad) {
		LumpID = lumpToLoad;
		ref BSPLump lump = ref MapHeader.Lumps[(int)lumpToLoad];
		LumpSize = lump.FileLength;
		LumpOffset = lump.FileOffset;
		LumpVersion = lump.Version;
	}

	public readonly bool LoadLumpData<T>(Span<T> output) where T : unmanaged {
		ref BSPLump lump = ref MapHeader.Lumps[(int)LumpID];
		T[]? ret = LoadLumpData<T>();
		if (ret == null)
			return false;
		ret.AsSpan().ClampedCopyTo(output);
		return true;
	}

	public readonly T[] LoadLumpData<T>(bool throwIfNoElements = false, int maxElements = 0, bool sysErrorIfOOB = false) where T : unmanaged {
		ref BSPLump lump = ref MapHeader.Lumps[(int)LumpID];
		string? error;

		T[]? data = lump.ReadBytes<T>(MapFileHandle!);
		if (data == null) {
			error = $"ModelLoader: funny {LumpID} lump size in {LoadName}";
			goto doError;
		}

		if (throwIfNoElements && data.Length < 1) {
			error = $"ModelLoader: lump {LumpID} has no elements in map {LoadName}";
			goto doError;
		}

		if (maxElements > 0 && data.Length > maxElements) {
			error = $"ModelLoader: lump {LumpID} has too many elements ({data.Length} > {maxElements}) in map {LoadName}";
			goto doError;
		}

		return data;
	doError:
		if (sysErrorIfOOB)
			Sys.Error(error);
		else
			Host!.Error(error);
		return [];
	}
}

public class MDLCacheNotify : IMDLCacheNotify
{
	public void OnDataLoaded(MDLCacheDataType type, uint handle) {
		throw new NotImplementedException();
	}

	public void OnDataUnloaded(MDLCacheDataType type, uint handle) {
		throw new NotImplementedException();
	}

	public static readonly MDLCacheNotify s = new();
}

public class ModelLoader(Sys Sys, IFileSystem fileSystem, Host Host,
						 IEngineVGuiInternal EngineVGui, MatSysInterface materials,
						 CollisionModelSubsystem CM, IMaterialSystemHardwareConfig materialSystemHardwareConfig,
						 IMDLCache MDLCache, IStudioRender StudioRender, IBaseClientDLL g_ClientDLL, MatSysInterface matSys) : IModelLoader
{
	public int GetCount() {
		throw new NotImplementedException();
	}

	public object? GetExtraData(Model? model) {
		if (model == null)
			return null;

		switch (model.Type) {
			case ModelType.Sprite: {
					// sprites don't use the real cache yet
					if (model.Type == ModelType.Sprite) {
						// The sprite got unloaded.
						return model.Sprite.Sprite; // TODO
					}
				}
				break;

			case ModelType.Studio:
				return MDLCache.GetStudioHdr(model.Studio);
			default:
			case ModelType.Brush:
				// Should never happen
				Assert(false);
				break;
		}

		return null;
	}

	public int GetModelFileSize(ReadOnlySpan<char> name) {
		throw new NotImplementedException();
	}

	public Model GetModelForIndex(int i) {
		throw new NotImplementedException();
	}

	public Model? GetModelForName(ReadOnlySpan<char> name, ModelLoaderFlags referenceType) {
		Model? model = FindModel(name);
		Model? retval = LoadModel(model, ref referenceType);

		return retval;
	}

	InlineArray64<char> ActiveMapName;
	InlineArray64<char> LoadName;

	Model? WorldModel;
	double accumulatedModelLoadTimeStudio;
	double accumulatedModelLoadTimeVCollideSync;
	double accumulatedModelLoadTimeVCollideAsync;
	double accumulatedModelLoadTimeMaterialNamesOnly;
	double accumulatedModelLoadTimeVirtualModel;

	private Model? LoadModel(Model? mod, ref ModelLoaderFlags referenceType) {
		mod!.LoadFlags |= referenceType;

		bool touchAllData = false;
		int serverCount = Host.GetServerCount();
		if (mod.ServerCount != serverCount) {
			mod.ServerCount = serverCount;
			touchAllData = true;
		}

		if (mod.Type == ModelType.Studio && 0 == (mod.LoadFlags & ModelLoaderFlags.LoadedByPreload)) {
			throw new Exception("Studiomodels still need work");
		}

		if ((mod.LoadFlags & ModelLoaderFlags.Loaded) != 0)
			return mod;

		double st = Platform.Time;
		mod.StrName.String()!.FileBase(LoadName);
		if (Host.developer.GetInt() > 1)
			DevMsg($"Loading: {mod.StrName.String()}\n");

		mod.Type = GetTypeFromName(mod.StrName);
		if (mod.Type == ModelType.Invalid)
			mod.Type = ModelType.Studio;

		switch (mod.Type) {
			case ModelType.Sprite: {
					double t1 = Platform.Time;
					Sprite_LoadModel(mod);
					double t2 = Platform.Time;
					accumulatedModelLoadTimeStudio = (t2 - t1);
				}
				break;
			case ModelType.Studio: {
					double t1 = Platform.Time;
					Studio_LoadModel(mod, touchAllData);
					double t2 = Platform.Time;
					accumulatedModelLoadTimeStudio = (t2 - t1);
				}
				break;
			case ModelType.Brush: {
					fileSystem.AddSearchPath(mod.StrName, "GAME", SearchPathAdd.ToHead);

					// exclude textures later

					strcpy(ActiveMapName, mod.StrName);

					fileSystem.BeginMapAccess();
					Map_LoadModel(mod);
					fileSystem.EndMapAccess();
				}
				break;
		}
		return mod;
	}

	private void Sprite_LoadModel(Model mod) {
		Assert((mod.LoadFlags & ModelLoaderFlags.Loaded) == 0);

		mod.LoadFlags |= ModelLoaderFlags.Loaded;

		mod.Type = ModelType.Sprite;
		mod.Sprite.Sprite = new EngineSprite();

		// Fake the bounding box. We need it for PVS culling, and we don't
		// know the scale at which the sprite is going to be rendered at
		// when we load it
		mod.Mins = mod.Maxs = new(0, 0, 0);

		// Figure out the real load name..
		Span<char> loadName = stackalloc char[MAX_PATH];
		bool bIsVideo;
		BuildSpriteLoadName(mod.StrName, loadName, out bIsVideo);
		GetSpriteInfo(loadName, bIsVideo, out mod.Sprite.Width, out mod.Sprite.Height, out mod.Sprite.NumFrames);

#if !SWDS
		if (g_ClientDLL != null && mod.Sprite.Sprite != null)
			g_ClientDLL.InitSprite(mod.Sprite.Sprite, loadName);
#endif
	}

	private void GetSpriteInfo(Span<char> pName, bool bIsVideo, out int nWidth, out int nHeight, out int nFrameCount) {
		nFrameCount = 1;
		nWidth = nHeight = 1;

		// TODO: videos

		IMaterial? pMaterial = null;
		pMaterial = matSys.GL_LoadMaterial(pName, MaterialDefines.TEXTURE_GROUP_OTHER);
		if (pMaterial != null) {
			// Store off our source height, width, frame count
			nWidth = (int)pMaterial.GetMappingWidth();
			nHeight = (int)pMaterial.GetMappingHeight();
			nFrameCount = pMaterial.GetNumAnimationFrames();
		}

		if (pMaterial == matSys.MaterialEmpty) 
			DevMsg($"Missing sprite material {pName}\n");
	}

	private void BuildSpriteLoadName(ReadOnlySpan<char> name, Span<char> pOut, out bool isVideo) {
		Span<char> szBase = stackalloc char[MAX_PATH];
		isVideo = false;
		bool bIsVMT = false;
		ReadOnlySpan<char> pExt = name.GetFileExtension();
		if (!pExt.IsEmpty) {
			bIsVMT = pExt.CompareTo("vmt", StringComparison.OrdinalIgnoreCase) != 0;
			if (!bIsVMT) {
				if (false) {
					isVideo = false; // Need to implement video sprites later
				}
			}
		}

		if ((false || bIsVMT) && name.Contains('/') || name.Contains('\\')) {
			// The material system cannot handle a prepended "materials" dir
			// Keep .avi extensions on the material to load avi-based materials
			if (bIsVMT) {
				ReadOnlySpan<char> nameStart = name;
				if (name.StartsWith("materials/") ||
					name.StartsWith("materials\\")) {
					// skip past materials/
					nameStart = name[10..];
				}
				StrTools.StripExtension(nameStart, pOut);
			}
			else {
				// name is good as is
				name.CopyTo(pOut);
			}
		}
		else {
			name.FileBase(szBase);
			unsafe {
				sprintf(pOut, "sprites/%s").S(szBase);
			}
		}

		return;
	}

	static readonly ConVar mod_touchalldata = new("1", 0, "Touch model data during level startup");
	static readonly ConVar mod_forcetouchdata = new("1", 0, "Forces all model file data into cache on model load.");

	private void Studio_LoadModel(Model model, bool touchAllData) {
		if (!mod_touchalldata.GetBool())
			touchAllData = false;

		bool preloaded = (model.LoadFlags & ModelLoaderFlags.LoadedByPreload) != 0;

		bool loadPhysics = true;
		if (model.LoadFlags == ModelLoaderFlags.StaticProp)
			loadPhysics = false;

		model.LoadFlags |= ModelLoaderFlags.Loaded;
		model.LoadFlags &= ~ModelLoaderFlags.LoadedByPreload;

		if (!preloaded) {
			model.Studio = MDLCache.FindMDL(model.StrName);
			MDLCache.SetUserData(model.Studio, model);
			InitStudioModelState(model);
		}

		MDLCache.GetStudioHdr(model.Studio);
		if (loadPhysics && !preloaded) {
			bool synchronous = touchAllData;
			double t1 = Platform.Time;
			MDLCache.GetVCollideEx(model.Studio, synchronous);
			double t2 = Platform.Time;

			if (synchronous)
				accumulatedModelLoadTimeVCollideSync += t2 - t1;
			else
				accumulatedModelLoadTimeVCollideAsync += t2 - t1;
		}

		{
			double t1 = Platform.Time;
			model.Materials = default;

			IMaterial[] materials = ArrayPool<IMaterial>.Shared.Rent(128);
			int nMaterials = Mod_GetModelMaterials(model, materials);

			if ((model.LoadFlags & ModelLoaderFlags.Dynamic) != 0) {
				model.Materials = new Memory<IMaterial>(new IMaterial[nMaterials]);
				for (int i = 0; i < nMaterials; i++)
					model.Materials.Span[i] = materials[i];
			}

			if (nMaterials != 0)
				model.LoadFlags |= ModelLoaderFlags.TouchedMaterials;

			double t2 = Platform.Time;
			accumulatedModelLoadTimeMaterialNamesOnly += (t2 - t1);

			if (touchAllData || preloaded)
				Mod_TouchAllData(model, Host.GetServerCount());

			ArrayPool<IMaterial>.Shared.Return(materials, true);
		}
	}

	private int Mod_GetModelMaterials(Model model, Span<IMaterial> materials) {
		StudioHeader studioHdr;
		int found = 0;
		int i;

		switch (model.Type) {
			case ModelType.Brush: {
					for (i = 0; i < model.Brush.NumModelSurfaces; ++i) {
						ref BSPMSurface2 surfID = ref SurfaceHandleFromIndex(model.Brush.FirstModelSurface + i, model.Brush.Shared);
						if ((MSurf_Flags(ref surfID) & SurfDraw.NoDraw) != 0)
							continue;

						IMaterial? material = MSurf_TexInfo(ref surfID, model.Brush.Shared).Material;

						int j = found;
						while (--j >= 0) {
							if (materials[j] == material)
								break;
						}
						if (j < 0)
							materials[found++] = material!;

						if (found >= materials.Length)
							return found;
					}
				}
				break;

			case ModelType.Studio:
				if (!model.Materials.IsEmpty) {
					model.Materials.Span.CopyTo(materials);
				}
				else {
					studioHdr = MDLCache.GetStudioHdr(model.Studio);
					found = StudioRender.GetMaterialList(studioHdr, materials);
				}
				break;

			default:
				Assert(0);
				break;
		}

		return found;
	}

	private void Mod_TouchAllData(Model model, int serverCount) {
		double t1 = Platform.Time;

		VirtualModel virtualModel = MDLCache.GetVirtualModel(model.Studio);

		double t2 = Platform.Time;
		accumulatedModelLoadTimeVirtualModel += (t2 - t1);

		if (virtualModel != null && serverCount >= 1) {
			for (int i = 1; i < virtualModel.Group.Count; ++i) {
				// What the hell?
				MDLHandle_t childHandle = (MDLHandle_t)virtualModel.Group[i].Cache & 0xffff;
				Model? childModel = MDLCache.GetUserData<Model>(childHandle);
				if (childModel != null) {
					childModel.LoadFlags |= (model.LoadFlags & ModelLoaderFlags.ReferenceMask);
					childModel.LoadFlags |= ModelLoaderFlags.Loaded;
					childModel.LoadFlags &= ~ModelLoaderFlags.LoadedByPreload;
					childModel.ServerCount = serverCount;
				}
			}
		}

		if (!mod_forcetouchdata.GetBool())
			return;

		MDLCache.TouchAllData(model.Studio);
	}

	private void InitStudioModelState(Model model) {
		Assert(model.Type == ModelType.Studio);

		if (MDLCache.IsDataLoaded(model.Studio, MDLCacheDataType.StudioHDR))
			MDLCacheNotify.s.OnDataLoaded(MDLCacheDataType.StudioHDR, model.Studio);

		if (MDLCache.IsDataLoaded(model.Studio, MDLCacheDataType.StudioHWData))
			MDLCacheNotify.s.OnDataLoaded(MDLCacheDataType.StudioHWData, model.Studio);

		if (MDLCache.IsDataLoaded(model.Studio, MDLCacheDataType.VCollide))
			MDLCacheNotify.s.OnDataLoaded(MDLCacheDataType.VCollide, model.Studio);
	}

	private bool Map_IsValid(ReadOnlySpan<char> name, bool quiet = false) {
		if (name.IsEmpty || name[0] == '\0') {
			if (!quiet)
				ConMsg("ModelLoader.Map_IsValid: Empty mapname!\n");
			return false;
		}

		ReadOnlySpan<char> baseName = name.UnqualifiedFileName();

		bool illegalChar = false;
		for (int i = 0; i < baseName.Length; i++) {
			if (baseName[i] <= 31)
				illegalChar = true;

			switch (baseName[i]) {
				case '<':
				case '>':
				case ':':
				case '"':
				case '/':
				case '\\':
				case '|':
				case '?':
				case '*':
				case ';':
				case '\'':
					illegalChar = true;
					break;
			}
		}

		if (illegalChar) {
			AssertMsg(false, "Map with illegal characters in filename");
			Warning("Map with illegal characters in filename\n");
			return false;
		}

		IFileHandle? mapFile = fileSystem.Open(name, FileOpenOptions.Read | FileOpenOptions.Binary, "GAME");
		if (mapFile != null) {
			BSPHeader header = default;
			Span<byte> outWrite = MemoryMarshal.Cast<BSPHeader, byte>(new Span<BSPHeader>(ref header));
			int len = mapFile.Stream.Read(outWrite);
			mapFile.Dispose();
			if (len != outWrite.Length) {
				if (!quiet)
					Warning($"ModelLoader.Map_IsValid: Map '{name}' was not large enough to contain a BSP header\n");
			}
			else {
				if (header.Identifier == BSPFileCommon.IDBSPHEADER) {
					if (header.Version >= BSPFileCommon.MINBSPVERSION && header.Version <= BSPFileCommon.BSPVERSION) {
						name.ClampedCopyTo(LastMapFile);
						return true;
					}
					else if (!quiet)
						Warning($"ModelLoader.Map_IsValid: Map '{name}' bsp version {header.Version}, expecting {BSPFileCommon.MINBSPVERSION} to {BSPFileCommon.BSPVERSION}\n");
				}
				else if (!quiet)
					Warning($"ModelLoader.Map_IsValid: Map '{name}' is not a valid BSP file\n");
			}
		}
		else if (!quiet)
			Warning($"ModelLoader.Map_IsValid: No such map '{name}'\n");

		return false;
	}

	InlineArrayMaxPath<char> LastMapFile;

	int MapLoadCount;
	public readonly WorldBrushData WorldBrushData = new();

	private void Map_LoadModel(Model mod) {
		MapLoadCount++;
		double startTime = Platform.Time;

#if !SWDS
		EngineVGui.UpdateProgressBar(LevelLoadingProgress.LoadWorldModel);
#endif

		SetWorldModel(mod);
		mod.Brush.Shared = WorldBrushData;
		mod.Brush.RenderHandle = 0;

		Common.TimestampedLog("Loading map");
		CM.LoadMap(mod.StrName, false, out uint checksum);

		mod.Type = ModelType.Brush;
		mod.LoadFlags |= ModelLoaderFlags.Loaded;
		if (!MapLoadHelper.Init(mod, ((Span<char>)(ActiveMapName)).SliceNullTerminatedString()))
			return;

		Mod_LoadVertices();
		BSPEdge[] edges = Mod_LoadEdges();
		Mod_LoadSurfedges(edges);
		Mod_LoadPlanes();
		// Mod_LoadOcclusion();
		Mod_LoadTexdata();
		Mod_LoadTexinfo();

#if !SWDS
		EngineVGui.UpdateProgressBar(LevelLoadingProgress.LoadWorldModel);
#endif

		if (materialSystemHardwareConfig.GetHDRType() != HDRType.None && MapLoadHelper.GetLumpSize(LumpIndex.LightingHDR) > 0) {
			MapLoadHelper mlh = new(LumpIndex.LightingHDR);
			Map_LoadLighting(mlh);
		}
		else {
			MapLoadHelper mlh = new(LumpIndex.Lighting);
			Map_LoadLighting(mlh);
		}

		Mod_LoadPrimitives();
		Mod_LoadPrimVerts();
		Mod_LoadPrimIndices();

#if !SWDS
		EngineVGui.UpdateProgressBar(LevelLoadingProgress.LoadWorldModel);
#endif

		Mod_LoadFaces();
		Mod_LoadVertNormals();
		Mod_LoadVertNormalIndices();

		MapLoadHelper.Shutdown();
		double elapsed = Platform.Time - startTime;
		Common.TimestampedLog($"Map_LoadModel: Finish - loading took {elapsed:F4} seconds");
	}

	private void Map_LoadLighting(MapLoadHelper lh) {
		if (lh.LumpSize == 0) {
			lh.GetMap().LightData = null;
			return;
		}

		Assert((lh.LumpSize % (sizeof(byte) * 4)) == 0);
		Assert(lh.LumpVersion != 0);

		lh.GetMap().LightData = lh.LoadLumpData<ColorRGBExp32>();
	}

	private void Mod_LoadPrimitives() {
		MapLoadHelper lh = new MapLoadHelper(LumpIndex.Primitives);
		BSPPrimitive[] inPrims = lh.LoadLumpData<BSPPrimitive>();
		BSPMPrimitive[] outPrims = new BSPMPrimitive[inPrims.Length];

		lh.GetMap().Primitives = outPrims;
		for (int i = 0; i < outPrims.Length; i++) {
			ref BSPPrimitive inPrim = ref inPrims[i];
			ref BSPMPrimitive outPrim = ref outPrims[i];
			outPrim.FirstIndex = inPrim.FirstIndex;
			outPrim.FirstVert = inPrim.FirstVert;
			outPrim.IndexCount = inPrim.IndexCount;
			outPrim.Type = (BSPPrimType)inPrim.Type;
			outPrim.VertCount = inPrim.VertCount;
		}
	}
	private void Mod_LoadPrimVerts() {
		MapLoadHelper lh = new MapLoadHelper(LumpIndex.PrimVerts);
		BSPPrimVert[] inPrims = lh.LoadLumpData<BSPPrimVert>();
		BSPMPrimVert[] outPrims = new BSPMPrimVert[inPrims.Length];

		lh.GetMap().PrimVerts = outPrims;
		for (int i = 0; i < outPrims.Length; i++) {
			ref BSPPrimVert inPrim = ref inPrims[i];
			ref BSPMPrimVert outPrim = ref outPrims[i];
			outPrim.Position = inPrim.Position;
		}
	}
	private void Mod_LoadPrimIndices() {
		MapLoadHelper lh = new MapLoadHelper(LumpIndex.PrimIndices);
		lh.GetMap().PrimIndices = lh.LoadLumpData<ushort>();
	}
	public static ref BSPFace FaceHandleFromIndex(int surfaceIndex, WorldBrushData data) => ref data.Faces![surfaceIndex];
	public static ref BSPMSurface2 SurfaceHandleFromIndex(int surfaceIndex, WorldBrushData data) => ref data.Surfaces2![surfaceIndex];
	public static ref CollisionPlane MSurf_Plane(ref BSPMSurface2 surfID) => ref surfID.Plane.GetReference();
	public static unsafe int MSurf_Index(ref BSPMSurface2 surfID, WorldBrushData? data = null) => (int)surfID.SurfNum;
	public static ref int MSurf_FirstVertIndex(ref BSPMSurface2 surfID) => ref surfID.FirstVertIndex;
	public static ref uint MSurf_FirstVertNormal(ref BSPMSurface2 surfID, WorldBrushData? data = null) {
		data ??= Singleton<CommonHostState>().WorldBrush;
		int surfaceIndex = MSurf_Index(ref surfID, data);
		return ref data!.SurfaceNormals![surfaceIndex].FirstVertNormal;
	}
	public static Span<short> MSurf_LightmapMins(ref BSPMSurface2 surfID, WorldBrushData? data = null) {
		data ??= Singleton<CommonHostState>().WorldBrush;
		int surfaceIndex = MSurf_Index(ref surfID, data);
		return data!.SurfaceLighting![surfaceIndex].LightmapMins;
	}
	public static Span<short> MSurf_LightmapExtents(ref BSPMSurface2 surfID, WorldBrushData? data = null) {
		data ??= Singleton<CommonHostState>().WorldBrush;
		int surfaceIndex = MSurf_Index(ref surfID, data);
		return data!.SurfaceLighting![surfaceIndex].LightmapExtents;
	}
	public static ref SurfDraw MSurf_Flags(ref BSPMSurface2 surfID) => ref surfID.Flags;
	public static bool SurfaceHasDispInfo(ref BSPMSurface2 surfID) => (MSurf_Flags(ref surfID) & SurfDraw.HasDisp) != 0;
	public static ref ushort MSurf_VertBufferIndex(ref BSPMSurface2 surfID) => ref surfID.VertBufferIndex;
	public static ushort MSurf_NumPrims(ref BSPMSurface2 surfID, WorldBrushData data) {
		if (SurfaceHasDispInfo(ref surfID) || !SurfaceHasPrims(ref surfID))
			return 0;

		int surfaceIndex = MSurf_Index(ref surfID, data);
		return data.Surfaces1![surfaceIndex].NumPrims;
	}
	public static ref short MSurf_MaterialSortID(ref BSPMSurface2 surfID) => ref surfID.MaterialSortID;
	public static bool SurfaceHasPrims(ref BSPMSurface2 surfID) => (MSurf_Flags(ref surfID) & SurfDraw.HasPrims) != 0;
	public static int MSurf_SortGroup(ref BSPMSurface2 surfID) => (int)(surfID.Flags & SurfDraw.SortGroupMask) >> (int)SurfDraw.SortGroupShift;
	public static void MSurf_SetSortGroup(ref BSPMSurface2 surfID, int sortGroup) => surfID.Flags |= (SurfDraw)((sortGroup << (int)SurfDraw.SortGroupShift) & (int)SurfDraw.SortGroupMask);
	public static ref ModelTexInfo MSurf_TexInfo(ref BSPMSurface2 surfID, WorldBrushData? data = null) => ref (data ?? Singleton<CommonHostState>().WorldBrush)!.TexInfo![surfID.TexInfo];
	public static Span<short> MSurf_OffsetIntoLightmapPage(ref BSPMSurface2 surfID, WorldBrushData? data = null) {
		data ??= Singleton<CommonHostState>().WorldBrush;
		return data!.SurfaceLighting![MSurf_Index(ref surfID, data)].OffsetIntoLightmapPage;
	}
	public static int MSurf_VertCount(ref BSPMSurface2 surfID) => (int)(((uint)surfID.Flags >> (int)SurfDraw.VertCountShift) & 0xFF);
	public static void MSurf_SetVertCount(ref BSPMSurface2 surfID, uint vertCount) {
		uint flags = (vertCount << (int)SurfDraw.VertCountShift) & (uint)SurfDraw.VertCountMask;
		surfID.Flags |= (SurfDraw)flags;
	}

	private void Mod_LoadFaces() {
		MapLoadHelper lh = new MapLoadHelper(LumpIndex.Faces);
		BSPFace[] inFaces = lh.LoadLumpData<BSPFace>();

		int count = inFaces.Length;
		BSPMSurface1[] out1 = new BSPMSurface1[count];
		BSPMSurface2[] out2 = new BSPMSurface2[count];
		BSPSurfaceLighting[] lighting = new BSPSurfaceLighting[count];

		WorldBrushData brushData = lh.GetMap();
		brushData.Faces = inFaces;
		brushData.Surfaces1 = out1;
		brushData.Surfaces2 = out2;
		brushData.SurfaceLighting = lighting;
		brushData.SurfaceNormals = new BSPMSurfaceNormal[count];
		brushData.NumSurfaces = count;

		int ti, di;

		for (int surfnum = 0; surfnum < count; surfnum++) {
			ref readonly BSPFace _in = ref inFaces[surfnum];
			ref BSPMSurface1 _out1 = ref out1[surfnum];
			ref BSPMSurface2 _out2 = ref out2[surfnum];
			ref BSPSurfaceLighting _light = ref lighting[surfnum];
			_out1.SurfNum = surfnum;
			_out2.SurfNum = surfnum;
			_light.SurfNum = surfnum;

			ref BSPMSurface2 surfID = ref SurfaceHandleFromIndex(surfnum, brushData);

			MSurf_FirstVertIndex(ref surfID) = _in.FirstEdge;

			int vertCount = _in.NumEdges;
			MSurf_Flags(ref surfID) = 0;
			Assert(vertCount <= 255);
			MSurf_SetVertCount(ref surfID, (uint)vertCount);

			int planenum = _in.PlaneNum;
			if (_in.OnNode != 0)
				MSurf_Flags(ref surfID) |= SurfDraw.Node;

			if (_in.Side != 0)
				MSurf_Flags(ref surfID) |= SurfDraw.PlaneBack;

			_out2.Plane = lh.GetMap().Planes![planenum];

			ti = _in.TexInfo;
			if (ti < 0 || ti >= lh.GetMap().NumTexInfo)
				Host.Error("Mod_LoadFaces: bad texinfo number");

			surfID.TexInfo = (ushort)ti;
			surfID.DynamicShadowsEnabled = _in.AreDynamicShadowsEnabled();
			ref ModelTexInfo tex = ref lh.GetMap().TexInfo![ti];

			if (tex.Material == null)
				tex.Material = materials.MaterialEmpty;

			if (Mod_LoadSurfaceLightingV1(ref _light, in _in, lh.GetMap().LightData))
				MSurf_Flags(ref surfID) |= SurfDraw.HasLightStyles;

			// set the drawing flags flag
			if ((tex.Flags & Surf.NoLight) != 0)
				MSurf_Flags(ref surfID) |= SurfDraw.NoLight;

			if ((tex.Flags & Surf.NoShadows) != 0)
				MSurf_Flags(ref surfID) |= SurfDraw.NoShadows;

			if ((tex.Flags & Surf.Warp) != 0)
				MSurf_Flags(ref surfID) |= SurfDraw.WaterSurface;

			if ((tex.Flags & Surf.Sky) != 0)
				MSurf_Flags(ref surfID) |= SurfDraw.Sky;

			di = _in.DispInfo;
			_out2.DispInfo = null;
			if (di != -1) {
				MSurf_Flags(ref surfID) |= SurfDraw.HasDisp;
			}
			else {
				Assert((tex.Flags & Surf.NoDraw) == 0);

				_out1.NumPrims = _in.GetNumPrims();
				_out1.FirstPrimID = _in.FirstPrimID;
				if (_in.GetNumPrims() != 0) {
					MSurf_Flags(ref surfID) |= SurfDraw.HasPrims;
					ref BSPMPrimitive prim = ref brushData.Primitives![_in.FirstPrimID];
					if (prim.VertCount > 0) {
						MSurf_Flags(ref surfID) |= SurfDraw.Dynamic;
					}
				}
			}

			// todo
			// _out2.ShadowDecals = SHADOW_DECAL_HANDLE_INVALID;
			// _out2.Decals = WORLD_DECAL_HANDLE_INVALID;

			// out2.FirstOverlayFragment = OVERLAY_FRAGMENT_INVALID;

			// CalcSurfaceExtents(in lh, ref surfID);
		}
	}

	private bool Mod_LoadSurfaceLightingV1(ref BSPSurfaceLighting light, in BSPFace _in, ColorRGBExp32[]? lightData) {
		light.LightmapExtents[0] = (short)_in.LightmapTextureSizeInLuxels[0];
		light.LightmapExtents[1] = (short)_in.LightmapTextureSizeInLuxels[1];
		light.LightmapMins[0] = (short)_in.LightmapTextureMinsInLuxels[0];
		light.LightmapMins[1] = (short)_in.LightmapTextureMinsInLuxels[1];

		int i = _in.LightOffset / 4;

		if (i == -1 || lightData == null) {
			light.Samples = null;

			for (i = 0; i < BSPFileCommon.MAXLIGHTMAPS; ++i)
				light.Styles[i] = 255;
		}
		else {
			light.Samples = new(lightData, i, 4);

			for (i = 0; i < BSPFileCommon.MAXLIGHTMAPS; ++i)
				light.Styles[i] = _in.Styles[i];
		}

		return ((light.Styles[0] != 0) && (light.Styles[0] != 255)) || (light.Styles[1] != 255);
	}

	private void Mod_LoadVertNormals() {
		MapLoadHelper lh = new MapLoadHelper(LumpIndex.VertNormals);
		Vector3[] inVertNormals = lh.LoadLumpData<Vector3>();

		lh.GetMap().VertNormals = inVertNormals;
	}
	private void Mod_LoadVertNormalIndices() {
		MapLoadHelper lh = new MapLoadHelper(LumpIndex.VertNormalIndices);
		ushort[] inVertNormalIndices = lh.LoadLumpData<ushort>();
		lh.GetMap().VertNormalIndices = inVertNormalIndices;
		int normalIndex = 0;
		for (int i = 0; i < lh.GetMap().NumSurfaces; i++) {
			ref BSPMSurface2 surfID = ref SurfaceHandleFromIndex(i, lh.GetMap());
			MSurf_FirstVertNormal(ref surfID, lh.GetMap()) = (uint)normalIndex;
			normalIndex += MSurf_VertCount(ref surfID);
		}
	}
	private void Mod_LoadTexdata() {
		MapLoadHelper.Map!.NumTexData = GetCollisionBSPData().NumSurfaces;
		MapLoadHelper.Map!.TexData = GetCollisionBSPData().MapSurfaces.Base();
	}

	private void Mod_LoadTexinfo() {
		MapLoadHelper lh = new MapLoadHelper(LumpIndex.TexInfo);
		BSPTexInfo[] inTexInfo = lh.LoadLumpData<BSPTexInfo>();
		ModelTexInfo[] outTexInfo = new ModelTexInfo[inTexInfo.Length];

		lh.GetMap().TexInfo = outTexInfo;
		lh.GetMap().NumTexInfo = inTexInfo.Length;

		bool loadtextures = true; // << todo: convar
		for (int i = 0; i < outTexInfo.Length; ++i) {
			ref BSPTexInfo _in = ref inTexInfo[i];
			ref ModelTexInfo _out = ref outTexInfo[i];
			for (int j = 0; j < 2; ++j) {
				for (int k = 0; k < 4; ++k) {
					_out.TextureVecsTexelsPerWorldUnits[j][k] = _in.TextureVecsTexelsPerWorldUnits[j][k];
					_out.LightmapVecsLuxelsPerWorldUnits[j][k] = _in.LightmapVecsLuxelsPerWorldUnits[j][k];
				}
			}

			_out.LuxelsPerWorldUnit = _out.LightmapVecsLuxelsPerWorldUnits[0].AsVector3().Length();
			_out.WorldUnitsPerLuxel = 1.0f / _out.LuxelsPerWorldUnit;
			_out.TexData = _in.TexData;
			_out.Flags = (Surf)_in.Flags;
			_out.TexInfoFlags = 0;

			if (loadtextures) {
				if (_in.TexData >= 0)
					_out.Material = materials.GL_LoadMaterial(lh.GetMap().TexData![_in.TexData].Name, MaterialDefines.TEXTURE_GROUP_WORLD);
				else {
					DevMsg($"Mod_LoadTexinfo: texdata < 0 (index=={i}/{outTexInfo.Length})\n");
					_out.Material = null;
				}
				if (_out.Material == null) {
					DevWarning($"Mod_LoadTexInfo: cannot find material named {lh.GetMap().TexData![_in.TexData].Name}\n");
					_out.Material = materials.MaterialEmpty;
				}
			}
			else
				_out.Material = materials.MaterialEmpty;
		}
	}

	private void Mod_LoadPlanes() {
		MapLoadHelper.Map!.Planes = GetCollisionBSPData().MapPlanes.Base();
		MapLoadHelper.Map!.NumPlanes = GetCollisionBSPData().NumPlanes;
	}

	private void Mod_LoadVertices() {
		MapLoadHelper lh = new MapLoadHelper(LumpIndex.Vertexes);
		lh.GetMap().Vertexes = lh.LoadLumpData<BSPVertex>();
	}

	private BSPEdge[] Mod_LoadEdges() {
		MapLoadHelper lh = new MapLoadHelper(LumpIndex.Edges);
		BSPEdge[] outData = lh.LoadLumpData<BSPEdge>();
		return outData;
	}
	private void Mod_LoadSurfedges(BSPEdge[] edges) {
		MapLoadHelper lh = new MapLoadHelper(LumpIndex.SurfEdges);
		int[] inData = lh.LoadLumpData<int>(throwIfNoElements: true, maxElements: BSPFileCommon.MAX_MAP_SURFEDGES);
		ushort[] outData = new ushort[inData.Length]; ;
		lh.GetMap().VertIndices = outData;

		for (int i = 0; i < outData.Length; i++) {
			int edge = inData[i];
			int index = 0;
			if (edge < 0) {
				edge = -edge;
				index = 1;
			}
			outData[i] = edges[edge].V[index];
		}
	}
	public void SetWorldModel(Model mod) {
		WorldModel = mod;
	}

	public void ClearWorldModel() {
		WorldModel = null;
	}



	private ModelType GetTypeFromName(ReadOnlySpan<char> modelName) => modelName.GetFileExtension() switch {
		"spr" or "vmt" => ModelType.Sprite,
		"bsp" => ModelType.Brush,
		"mdl" => ModelType.Studio,
		_ => ModelType.Invalid
	};

	readonly List<Model> InlineModels = [];
	readonly Dictionary<FileNameHandle_t, Model> Models = [];

	private Model? FindModel(ReadOnlySpan<char> name) {
		if (name.IsEmpty)
			Sys.Error("ModelLoader.FindModel: NULL name");


		if (name[0] == '*') {
			int.TryParse(name[1..], out int modelNum);
			if (IsWorldModelSet())
				Sys.Error($"bad inline model number {modelNum}, worldmodel not yet setup");

			if (modelNum < 1 || modelNum >= GetNumWorldSubmodels())
				Sys.Error($"bad inline model number {modelNum}");

			return InlineModels[modelNum];
		}

		Model? model = null;

		FileNameHandle_t fnHandle = fileSystem.FindOrAddFileName(name);

		if (!Models.TryGetValue(fnHandle, out model)) {
			model = new() {
				FileNameHandle = fnHandle,
				LoadFlags = ModelLoaderFlags.NotLoadedOrReferenced,
				StrName = name
			};

			Models[fnHandle] = model;
		}

		Assert(model);

		return model;
	}

	private bool IsWorldModelSet() {
		throw new NotImplementedException();
	}

	private int GetNumWorldSubmodels() {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetName(Model model) {
		throw new NotImplementedException();
	}

	public void Init() {

	}

	public void PurgeUnusedModels() {
		throw new NotImplementedException();
	}

	public Model? ReferenceModel(ReadOnlySpan<char> name, ModelLoaderFlags referenceType) {
		throw new NotImplementedException();
	}

	public void ResetModelServerCounts() {

	}

	public void UnreferenceAllModels(ModelLoaderFlags referenceType) {
		throw new NotImplementedException();
	}

	public void UnreferenceModel(Model model, ModelLoaderFlags referenceType) {
		throw new NotImplementedException();
	}

	internal static void Map_VisSetup(Model? worldModel, ReadOnlySpan<Vector3> origins, bool novis, out uint returnFlags) {
		// todo
		returnFlags = 0;
	}

	internal static int MSurf_FirstPrimID(ref BSPMSurface2 surfID, WorldBrushData bsp) {
		if (SurfaceHasDispInfo(ref surfID))
			return 0;
		int surfaceIndex = MSurf_Index(ref surfID, bsp);
		return bsp.Surfaces1![surfaceIndex].FirstPrimID;
	}

	public void Map_LoadDisplacements(MaterialSystem_SortInfo[] materialSortInfoArray, Model model) {
		if (!MapLoadHelper.Init(model, ActiveMapName))
			return;

		DispInfo_LoadDisplacements(materialSortInfoArray, model);
		MapLoadHelper.Shutdown();
	}

	private unsafe bool DispInfo_LoadDisplacements(MaterialSystem_SortInfo[] sortInfos, Model model) {
		nint numDisplacements = MapLoadHelper.GetLumpSize(LumpIndex.DispInfo);
		nint numLuxels = MapLoadHelper.GetLumpSize(LumpIndex.DispLightmapAlphas);
		nint numSamplePositionBytes = MapLoadHelper.GetLumpSize(LumpIndex.DispLightmapSamplePositions);

		model.Brush.Shared!.NumDispInfos = (int)numDisplacements;
		model.Brush.Shared!.DispInfos = DispInfo_CreateArray(numDisplacements);

		MapLoadHelper dispInfos = new MapLoadHelper(LumpIndex.DispInfo);

		DispLMAlpha.Clear(); DispLMAlpha.SetSize((int)numLuxels);
		MapLoadHelper dispLMAlphas = new MapLoadHelper(LumpIndex.DispLightmapAlphas);
		dispLMAlphas.LoadLumpData(DispLMAlpha.AsSpan());

		DispLMSamplePositions.Clear(); DispLMSamplePositions.SetSize((int)numLuxels);
		MapLoadHelper dispLMPositions = new MapLoadHelper(LumpIndex.DispLightmapSamplePositions);
		dispLMAlphas.LoadLumpData(DispLMSamplePositions.AsSpan());

		Span<BSPDispInfo> tempDisps = new BSPDispInfo[BSPFileCommon.MAX_MAP_DISPINFO];

		Span<DispVert> tempVerts = stackalloc DispVert[BSPFileCommon.MAX_DISPVERTS];
		Span<DispTri> tempTris = stackalloc DispTri[BSPFileCommon.MAX_DISPTRIS];

		MapLoadHelper dispVerts = new(LumpIndex.DispVerts);
		MapLoadHelper dispTris = new(LumpIndex.DispTris);

		int curVert = 0;
		int curTri = 0;

		List<CoreDispInfo> coreDisps = [];
		int disp = 0;
		for (disp = 0; disp < numDisplacements; disp++) {
			coreDisps.Add(new());
		}

		/*for (disp = 0; disp < numDisplacements; ++disp) {
			ref BSPDispInfo mapDisp = ref tempDisps[disp];

			int numVerts = BSPFileCommon.NUM_DISP_POWER_VERTS(mapDisp.Power);
			ErrorIfNot(numVerts <= BSPFileCommon.MAX_DISPVERTS, $"DispInfo_LoadDisplacements: invalid vertex count ({numVerts})");
			dispVerts.LoadLumpData(tempVerts);
			curVert += numVerts;

			int numTris = BSPFileCommon.NUM_DISP_POWER_TRIS(mapDisp.Power);
			ErrorIfNot(numTris <= BSPFileCommon.MAX_DISPTRIS, $"DispInfo_LoadDisplacements: invalid tri count ({numTris})");
			dispTris.LoadLumpData(tempTris);
			curTri += numTris;

			if (!DispInfo_CreateFromMapDisp(model, disp, ref mapDisp, coreDisps[disp], tempVerts, tempTris, sortInfos))
				return false;
		}*/

		return true;

	}

	public bool DispInfo_CreateFromMapDisp(Model world, int disp, ref BSPDispInfo mapDisp, CoreDispInfo coreDisp, Span<DispVert> verts, Span<DispTri> tris, MaterialSystem_SortInfo[] sortInfos) {
		return true;
	}

	readonly List<byte> DispLMAlpha = [];
	readonly List<byte> DispLMSamplePositions = [];

	private object? DispInfo_CreateArray(nint numDisplacements) {
		DispArray ret = new DispArray(numDisplacements);
		ret.CurTag = 1;
		for (nint i = 0; i < numDisplacements; i++) {
			ret.DispInfos[i] = new();
			ret.DispInfos[i].DispArray = ret;
		}
		return ret;
	}

	public void Shutdown() {

	}
}

public class DispArray(nint elements)
{
	public DispInfo[] DispInfos = new DispInfo[elements];
	public int CurTag;
}

public class CoreDispInfo
{

}
