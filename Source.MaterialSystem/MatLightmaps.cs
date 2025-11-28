using Source.Bitmap;
using Source.Common.Bitmap;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Common.ShaderAPI;

using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.MaterialSystem;

public static class ColorSpace
{
	static float[] textureToLinear = new float[256];  // texture (0..255) to linear (0..1)
	static int[] linearToTexture = new int[1024];   // linear (0..1) to texture (0..255)
	static int[] linearToScreen = new int[1024];    // linear (0..1) to gamma corrected vertex light (0..255)
	static float[] g_LinearToVertex = new float[4096];   // linear (0..4) to screen corrected vertex space (0..1?)
	static int[] linearToLightmap = new int[4096];  // linear (0..4) to screen corrected texture value (0..255)

	public static void LinearToLightmap(Span<byte> pDstRGB, ReadOnlySpan<float> pSrcRGB) {
		Vector3 tmpVect = default;
		int i, j;
		for (j = 0; j < 3; j++) {
			i = MathLib.RoundFloatToInt(pSrcRGB[j] * 1024); // assume 0..4 range
			if (i < 0) {
				i = 0;
			}
			if (i > 4091)
				i = 4091;
			tmpVect[j] = g_LinearToVertex[i];
		}

		MathLib.ColorClamp(ref tmpVect);

		pDstRGB[0] = MathLib.RoundFloatToByte(tmpVect[0] * 255.0f);
		pDstRGB[1] = MathLib.RoundFloatToByte(tmpVect[1] * 255.0f);
		pDstRGB[2] = MathLib.RoundFloatToByte(tmpVect[2] * 255.0f);
	}
}

public class MatLightmaps
{
	private readonly MaterialSystem MaterialSystem;
	readonly IMaterialSystemHardwareConfig HardwareConfig = Singleton<IMaterialSystemHardwareConfig>();
	readonly IShaderAPI ShaderAPI = Singleton<IShaderAPI>();


	public MatLightmaps(MaterialSystem materialSystem) {
		MaterialSystem = materialSystem;
	}

	public void CleanupLightmaps() {

		if (LightmapPages != null) {
			for (int i = 0; i < GetNumLightmapPages(); i++)
				ShaderAPI.DeleteTexture(LightmapPageTextureHandles[i].Handle);
			LightmapPages = null;
		}
		NumLightmapPages = 0;
	}

	public int NumSortIDs = 0;
	public int NumLightmapPages = 0;

	internal int GetNumSortIDs() => NumSortIDs;

	public void BeginLightmapAllocation() {
		NumSortIDs = 0;
		ImagePackers.Clear();
		ImagePackers.Add(new(MaterialSystem));
		ImagePackers[0].Reset(0, GetMaxLightmapPageWidth(), GetMaxLightmapPageHeight());
		SetCurrentMaterialInternal(null);
		CurrentWhiteLightmapMaterial = null;
		NumSortIDs = 0;
		ResetMaterialLightmapPageInfo();
		EnumerateMaterials();
	}

	public void ResetMaterialLightmapPageInfo() {
		foreach (var material in MaterialSystem.MaterialDict) {
			material.SetMinLightmapPageID(9999);
			material.SetMaxLightmapPageID(-9999);
			material.SetNeedsWhiteLightmap(false);
		}
	}

	public void EnumerateMaterials() {
		int id = 0;
		foreach (var material in MaterialSystem.MaterialDict)
			material.SetEnumerationID(id++);
	}

	struct LightmapPageInfo
	{
		public ushort Width;
		public ushort Height;
		public int Flags;
	}

	class DynamicLightmap
	{
		public void Init() {
			LightmapLocked = -1;
			FrameID = 0;
			CurrentDynamicIndex = 0;
			for (int i = 0; i < COUNT_DYNAMIC_LIGHTMAP_PAGES; i++)
				LightmapLockFrame[i] = 0;
		}

		public int LightmapLocked;
		public int FrameID;
		public int CurrentDynamicIndex;
		public readonly int[] LightmapLockFrame = new int[COUNT_DYNAMIC_LIGHTMAP_PAGES];
		public readonly ImagePacker[] ImagePackers = new ImagePacker[COUNT_DYNAMIC_LIGHTMAP_PAGES];
	}

	readonly List<ImagePacker> ImagePackers = [];
	LightmapPageInfo[]? LightmapPages;

	readonly DynamicLightmap dynamic = new();

	public int AllocateLightmap(int width, int height, Span<int> offsetIntoLightmapPage, IMaterial imaterial) {
		if (imaterial is not IMaterialInternal material) {
			Warning("Programming error: MatLightmaps.AllocateLightmap: NULL material\n");
			return NumSortIDs;
		}

		int i;
		int packCount = ImagePackers.Count;
		if (GetCurrentMaterialInternal() != material) {
			for (i = packCount - 1; --i >= 0;) {
				ImagePackers.RemoveAt(i);
				--packCount;
			}

			if (GetCurrentMaterialInternal() != null) {
				ImagePackers[0].IncrementSortId();
				++NumSortIDs;
			}

			SetCurrentMaterialInternal(material);

			Assert(material.GetMinLightmapPageID() > material.GetMaxLightmapPageID());
			Assert(GetCurrentMaterialInternal() != null);

			GetCurrentMaterialInternal()!.SetMinLightmapPageID(GetNumLightmapPages());
			GetCurrentMaterialInternal()!.SetMaxLightmapPageID(GetNumLightmapPages());
		}

		bool added = false;
		for (i = 0; i < packCount; ++i) {
			added = ImagePackers[i].AddBlock(width, height, out offsetIntoLightmapPage[0], out offsetIntoLightmapPage[1]);
			if (added)
				break;
		}

		if (!added) {
			++NumSortIDs;
			i = ImagePackers.Count; ImagePackers.Add(new(MaterialSystem));
			ImagePackers[i].Reset(NumSortIDs, GetMaxLightmapPageWidth(), GetMaxLightmapPageHeight());
			++NumLightmapPages;
			if (!ImagePackers[i].AddBlock(width, height, out offsetIntoLightmapPage[0], out offsetIntoLightmapPage[1]))
				Error($"MaterialSystem_Interface_t::AllocateLightmap: lightmap ({width}x{height}) too big to fit in page ({GetMaxLightmapPageWidth()}x{GetMaxLightmapPageHeight()})\n");

			GetCurrentMaterialInternal()!.SetMaxLightmapPageID(GetNumLightmapPages());
		}

		return ImagePackers[i].GetSortId();
	}
	int GetMaxLightmapPageHeight() {
		int height = 256;

		if (height > MaterialSystem.HardwareConfig.MaxTextureHeight())
			height = MaterialSystem.HardwareConfig.MaxTextureHeight();

		return height;
	}
	public  int GetNumLightmapPages() => NumLightmapPages;
	public  int GetMaxLightmapPageWidth() {
		int width = 512;
		if (width > MaterialSystem.HardwareConfig.MaxTextureWidth())
			width = MaterialSystem.HardwareConfig.MaxTextureWidth();

		return width;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public IMaterialInternal? GetCurrentMaterialInternal() => MaterialSystem.GetRenderContextInternal().GetCurrentMaterialInternal();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetCurrentMaterialInternal(IMaterialInternal? material) => MaterialSystem.GetRenderContextInternal().SetCurrentMaterialInternal(material);

	int FirstDynamicLightmap;

	public void EndLightmapAllocation() {
		NumLightmapPages++;
		NumSortIDs++;

		FirstDynamicLightmap = NumLightmapPages;
		dynamic.Init();

		int lastLightmapPageWidth, lastLightmapPageHeight;
		int nLastIdx = ImagePackers.Count;
		ImagePackers[nLastIdx - 1].GetMinimumDimensions(out lastLightmapPageWidth, out lastLightmapPageHeight);
		ImagePackers.Clear();

		LightmapPages = new LightmapPageInfo[GetNumLightmapPages()];

		for (int i = 0; i < GetNumLightmapPages(); i++) {
			bool lastStaticLightmap = (i == (FirstDynamicLightmap - 1));
			LightmapPages[i].Width = (ushort)(lastStaticLightmap ? lastLightmapPageWidth : GetMaxLightmapPageWidth());
			LightmapPages[i].Height = (ushort)(lastStaticLightmap ? lastLightmapPageHeight : GetMaxLightmapPageHeight());
			LightmapPages[i].Flags = 0;

			AllocateLightmapTexture(i);
		}
	}

	private void AllocateLightmapTexture(int lightmap) {
		bool bUseDynamicTextures = HardwareConfig.PreferDynamicTextures();

		CreateTextureFlags flags = CreateTextureFlags.Managed;
		LightmapPageTextureHandles.EnsureCount(lightmap + 1);

		Span<char> debugName = stackalloc char[256];
		sprintf(debugName, "[lightmap %d]").D(lightmap);

		ImageFormat imageFormat;
		switch (HardwareConfig.GetHDRType()) {
			default:
				Assert(0);
				goto case HDRType.None;
			case HDRType.None:
				imageFormat = ImageFormat.RGBA8888;
				flags |= CreateTextureFlags.SRGB;
				break;
			case HDRType.Integer:
				imageFormat = ImageFormat.RGBA16161616;
				break;
			case HDRType.Float:
				imageFormat = ImageFormat.RGBA16161616F;
				break;
		}

		switch (LightmapsState) {
			case LightmapsState.Default:
				// Allow allocations in default state
				{
					LightmapPageTextureHandles[lightmap] = new() {
						Handle = ShaderAPI.CreateTexture(
							GetLightmapWidth(lightmap), GetLightmapHeight(lightmap), 1,
							imageFormat,
							1, 1, flags, debugName, TEXTURE_GROUP_LIGHTMAP
						),
						Format = imageFormat
					};

					ShaderAPI.ModifyTexture(LightmapPageTextureHandles[lightmap].Handle);
					ShaderAPI.TexMinFilter(TexFilterMode.Linear);
					ShaderAPI.TexMagFilter(TexFilterMode.Linear);

					InitLightmapBits(lightmap);
				}
				break;

			case LightmapsState.Released:
				DevMsg($"AllocateLightmapTexture({lightmap}) in released lightmap state (STATE_RELEASED), delayed till \"Restore\".\n");
				return;

			default:
				Warning($"AllocateLightmapTexture({lightmap}) in unknown lightmap state ({LightmapsState}), skipped.\n");
				AssertMsg(false, "AllocateLightmapTexture(?) in unknown lightmap state (?)");
				return;
		}
	}

	private int GetLightmapWidth(int lightmap) {
		switch (lightmap) {
			default:
				Assert(lightmap >= 0 && lightmap < GetNumLightmapPages());
				return LightmapPages![lightmap].Width;

			case StandardLightmap.UserDefined:
				AssertMsg(false, "Can't use MatLightmaps to get properties of StandardLightmap.UserDefined");
				return 1;

			case StandardLightmap.White:
			case StandardLightmap.WhiteBump:
				return 1;
		}
	}

	private int GetLightmapHeight(int lightmap) {
		switch (lightmap) {
			default:
				Assert(lightmap >= 0 && lightmap < GetNumLightmapPages());
				return LightmapPages![lightmap].Height;

			case StandardLightmap.UserDefined:
				AssertMsg(false, "Can't use MatLightmaps to get properties of StandardLightmap.UserDefined");
				return 1;

			case StandardLightmap.White:
			case StandardLightmap.WhiteBump:
				return 1;
		}
	}


	private void InitLightmapBits(int lightmap) {
		int width = GetLightmapWidth(lightmap);
		int height = GetLightmapHeight(lightmap);

		PixelWriter writer = new();

		ShaderAPI.ModifyTexture(LightmapPageTextureHandles[lightmap].Handle);
		if (!ShaderAPI.TexLock(0, 0, 0, 0, width, height, ref writer))
			return;

		if (writer.IsUsingFloatFormat()) {
			for (int j = 0; j < height; ++j) {
				writer.Seek(0, j);
				for (int k = 0; k < width; ++k) {
					writer.WritePixel(1, 1, 1);
				}
			}
		}
		else {
			for (int j = 0; j < height; ++j) {
				writer.Seek(0, j);
				for (int k = 0; k < width; ++k) {
					writer.WritePixel(0, 0, 0);
				}
			}
		}

		ShaderAPI.TexUnlock();
	}

	int LockedLightmap;
	public bool LockLightmap(int lightmap) {
		if (LockedLightmap != -1) 
			ShaderAPI.TexUnlock();
		
		ShaderAPI.ModifyTexture(LightmapPageTextureHandles[lightmap].Handle);
		int pageWidth = LightmapPages![lightmap].Width;
		int pageHeight = LightmapPages![lightmap].Height;
		if (!ShaderAPI.TexLock(0, 0, 0, 0, pageWidth, pageHeight, ref LightmapPixelWriter)) {
			Assert(false);
			return false;
		}
		LockedLightmap = lightmap;
		return true;
	}

	IMaterialInternal? CurrentWhiteLightmapMaterial;

	internal int AllocateWhiteLightmap(IMaterial? imaterial) {
		if (imaterial is not IMaterialInternal material) {
			Warning("Programming error: MatLightmaps.AllocateWhiteLightmap: NULL material\n");
			return NumSortIDs;
		}

		if (CurrentWhiteLightmapMaterial == null || (CurrentWhiteLightmapMaterial != material)) {
			if (GetCurrentMaterialInternal() != null || CurrentWhiteLightmapMaterial != null)
				NumSortIDs++;

			CurrentWhiteLightmapMaterial = material;
			material.SetNeedsWhiteLightmap(true);
		}

		return NumSortIDs;
	}

	internal void GetSortInfo(Span<MaterialSystem_SortInfo> sortInfoArray) {
		int sortId = 0;
		ComputeSortInfo(sortInfoArray, ref sortId, false);
		ComputeWhiteLightmappedSortInfo(sortInfoArray, ref sortId, false);
		Assert(NumSortIDs == sortId);
	}

	private void ComputeSortInfo(Span<MaterialSystem_SortInfo> info, ref int sortId, bool v) {
		int lightmapPageID;
		foreach (var material in MaterialSystem.MaterialDict) {
			if (material.GetMinLightmapPageID() > material.GetMaxLightmapPageID())
				continue;

			for (lightmapPageID = material.GetMinLightmapPageID(); lightmapPageID <= material.GetMaxLightmapPageID(); ++lightmapPageID) {
				info[sortId].Material = material; // queue friendly review later
				info[sortId].LightmapPageID = lightmapPageID;

				++sortId;
			}
		}
	}
	int UpdatingLightmapsStackDepth;

	public void BeginUpdateLightmaps() {
		UpdatingLightmapsStackDepth++;
	}

	public void EndUpdateLightmaps() {
		UpdatingLightmapsStackDepth--;
		if (UpdatingLightmapsStackDepth <= 0 && LockedLightmap != -1) {
			ShaderAPI.TexUnlock();
			LockedLightmap = -1;
		}
	}

	private void ComputeWhiteLightmappedSortInfo(Span<MaterialSystem_SortInfo> info, ref int sortId, bool v) {
		foreach (var material in MaterialSystem.MaterialDict) {
			// TODO FIXME: The original plan was to not rely on reference counts and instead rely on C# object finalizers
			// and pushing unload events to the main thread. However, I think this is a bad idea for several reasons now.
			// I am reminded by it by this     \/--- statement where it checks if the material is referenced.
			if (material.GetNeedsWhiteLightmap()) {
				info[sortId].Material = material;
				if (material.GetPropertyFlag(MaterialPropertyTypes.NeedsBumpedLightmaps))
					info[sortId].LightmapPageID = StandardLightmap.WhiteBump;
				else
					info[sortId].LightmapPageID = StandardLightmap.White;

				sortId++;
			}
		}
	}

	internal void GetLightmapPageSize(int lightmapPageID, out int width, out int height) {
		switch (lightmapPageID) {
			default:
				Assert(lightmapPageID >= 0 && lightmapPageID < GetNumLightmapPages());
				width = LightmapPages![lightmapPageID].Width;
				height = LightmapPages![lightmapPageID].Height;
				break;

			case StandardLightmap.UserDefined:
				width = height = 1;
				Assert("Can't use CMatLightmaps to get properties of MATERIAL_SYSTEM_LIGHTMAP_PAGE_USER_DEFINED");
				break;

			case StandardLightmap.White:
			case StandardLightmap.WhiteBump:
				width = height = 1;
				break;
		}
	}

	public void ReleaseLightmapPages() {
		switch (LightmapsState) {
			case LightmapsState.Default:

				break;
			default:
				return;
		}

		for (int i = 0; i < GetNumLightmapPages(); i++)
			ShaderAPI.DeleteTexture(LightmapPageTextureHandles[i].Handle);

		LightmapsState = LightmapsState.Released;
	}

	const int COUNT_DYNAMIC_LIGHTMAP_PAGES = 1;

	internal void UpdateLightmap(int lightmapPageID, Span<int> lightmapSize, Span<int> offsetIntoLightmapPage, Span<float> floatImage, Span<float> floatImageBump1, Span<float> floatImageBump2, Span<float> floatImageBump3) {
		bool hasBump = false;
		int uSize = 1;
		FloatBitMap? pfmOut = null;
		if (!floatImageBump1.IsEmpty && !floatImageBump2.IsEmpty && !floatImageBump3.IsEmpty) {
			hasBump = true;
			uSize = 4;
		}

		if (lightmapPageID >= GetNumLightmapPages() || lightmapPageID < 0) {
			Error($"UpdateLightmap lightmapPageID={lightmapPageID} out of range\n");
			return;
		}
		bool bDynamic = IsDynamicLightmap(lightmapPageID);

		if (bDynamic) {
			int dynamicIndex = lightmapPageID - FirstDynamicLightmap;
			Assert(dynamicIndex < COUNT_DYNAMIC_LIGHTMAP_PAGES);
			dynamic.CurrentDynamicIndex = (dynamicIndex + 1) % COUNT_DYNAMIC_LIGHTMAP_PAGES; //-V1063
		}

		ShaderAPI.ModifyTexture(LightmapPageTextureHandles[lightmapPageID].Handle);
		if (!ShaderAPI.TexLock(0, 0, offsetIntoLightmapPage[0], offsetIntoLightmapPage[1], lightmapSize[0] * uSize, lightmapSize[1], ref LightmapPixelWriter))
			return;

		if (hasBump) {
			switch (HardwareConfig.GetHDRType()) {
				case HDRType.None:
					BumpedLightmapBitsToPixelWriter_LDR(floatImage, floatImageBump1, floatImageBump2, floatImageBump3, lightmapSize, offsetIntoLightmapPage, pfmOut);
					break;
				case HDRType.Integer:
					throw new NotImplementedException();
				//BumpedLightmapBitsToPixelWriter_HDRI(floatImage, floatImageBump1, floatImageBump2, floatImageBump3, lightmapSize, offsetIntoLightmapPage, pfmOut);
				//break;
				case HDRType.Float:
					throw new NotImplementedException();
					//BumpedLightmapBitsToPixelWriter_HDRF(floatImage, floatImageBump1, floatImageBump2, floatImageBump3, lightmapSize, offsetIntoLightmapPage, pfmOut);
					//break;
			}
		}
		else {
			switch (HardwareConfig.GetHDRType()) {
				case HDRType.None:
					LightmapBitsToPixelWriter_LDR(floatImage, lightmapSize, offsetIntoLightmapPage, pfmOut);
					break;
				case HDRType.Integer:
					throw new NotImplementedException();
				//LightmapBitsToPixelWriter_HDRI(floatImage, lightmapSize, offsetIntoLightmapPage, pfmOut);
				//break;
				case HDRType.Float:
					throw new NotImplementedException();
				//LightmapBitsToPixelWriter_HDRF(floatImage, lightmapSize, offsetIntoLightmapPage, pfmOut);
				//break;
				default:
					Assert(0);
					break;
			}
		}

		LightmapPixelWriter.Dispose();
		ShaderAPI.TexUnlock();
	}

	private unsafe void LightmapBitsToPixelWriter_LDR(Span<float> floatImage, Span<int> lightmapSize, Span<int> offsetIntoLightmapPage, FloatBitMap? pfmOut) {
		Span<float> src = floatImage;
		Span<byte> color = stackalloc byte[4];
		for (int t = 0; t < lightmapSize[1]; ++t) {
			LightmapPixelWriter.Seek(offsetIntoLightmapPage[0], offsetIntoLightmapPage[1] + t);
			for (int s = 0; s < lightmapSize[0]; ++s, src = src[(sizeof(Vector4) / sizeof(float))..]) {
				memreset(color);
				ColorSpace.LinearToLightmap(color, src);
				color[3] = MathLib.RoundFloatToByte(src[3] * 255.0f);
				LightmapPixelWriter.WritePixel(color[0], color[1], color[2], color[3]);
			}
		}
	}

	PixelWriterMem LightmapPixelWriter;

	private void BumpedLightmapBitsToPixelWriter_LDR(Span<float> floatImage, Span<float> floatImageBump1, Span<float> floatImageBump2, Span<float> floatImageBump3, Span<int> lightmapSize, Span<int> offsetIntoLightmapPage, FloatBitMap? pfmOut) {
		throw new NotImplementedException();
	}

	private bool IsDynamicLightmap(int lightmapPageID) {
		return false; // todo
	}

	internal ShaderAPITextureHandle_t GetLightmapPageTextureHandle(int lightmapPageID) {
		return LightmapPageTextureHandles[lightmapPageID].Handle;
	}

	struct LightmapInfo
	{
		public ShaderAPITextureHandle_t Handle;
		public ImageFormat Format;
	}
	readonly List<LightmapInfo> LightmapPageTextureHandles = [];
	LightmapsState LightmapsState;
}

public enum LightmapsState
{
	Default,
	Released
}
