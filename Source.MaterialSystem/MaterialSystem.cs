using Microsoft.Extensions.DependencyInjection;

using Source.Common;
using Source.Common.Bitmap;
using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Launcher;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Common.ShaderAPI;
using Source.MaterialSystem.Surface;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source.MaterialSystem;

public class MaterialSystem : IMaterialSystem, IShaderUtil
{
	public readonly MaterialDict MaterialDict;
	nint graphics;

	public IShaderUtil GetShaderUtil() => this;

	readonly IServiceProvider services;

	public readonly IFileSystem FileSystem;
	public readonly TextureManager TextureSystem;
	public readonly IShaderSystem ShaderSystem;
	public IShaderDevice ShaderDevice;
	public IShaderAPI ShaderAPI;
	public readonly IMeshMgr MeshMgr;
	public readonly IMaterialSystemHardwareConfig HardwareConfig;
	public readonly MaterialSystem_Config Config;

	public static void DLLInit(IServiceCollection services) {
		services.AddSingleton<MatSystemSurface>();
		services.AddSingleton<IMatSystemSurface>(x => x.GetRequiredService<MatSystemSurface>());
		services.AddSingleton<ISurface>(x => x.GetRequiredService<MatSystemSurface>());
		services.AddSingleton<ITextureManager, TextureManager>();
		services.AddSingleton<MaterialSystem_Config>();
	}

	public MaterialSystem(IServiceProvider services) {
		MaterialDict = new(this);
		this.services = services;

		FileSystem = services.GetRequiredService<IFileSystem>();
		ShaderAPI = services.GetRequiredService<IShaderAPI>()!;
		ShaderDevice = services.GetRequiredService<IShaderDevice>();
		TextureSystem = (services.GetRequiredService<ITextureManager>() as TextureManager)!;
		MeshMgr = services.GetRequiredService<IMeshMgr>(); // todo: interface
		HardwareConfig = services.GetRequiredService<IMaterialSystemHardwareConfig>(); // todo: interface
		ShaderSystem = services.GetRequiredService<IShaderSystem>();
		Config = services.GetRequiredService<MaterialSystem_Config>()!;

		// Link up
		ShaderAPI.PreInit(this, services);

		TextureSystem.MaterialSystem = this;

		ShaderSystem.LoadAllShaderDLLs();
		TextureSystem.Init();
		ShaderSystem.Init();
		CreateDebugMaterials();
		MatLightmaps = new(this);
	}

	ILauncherManager launcherMgr;


	public void ModInit() {
		launcherMgr = services.GetRequiredService<ILauncherManager>();
		matContext = new(() => new(this));
		UpdateConfig(false);
	}

	private bool UpdateConfig(bool forceUpdate) {
		MaterialSystem_Config config = new();
		Config.CopyInstantiatedReferenceTo(config);
		ReadConfigFromConVars(config);
		return OverrideConfig(config, forceUpdate);
	}

	private bool OverrideConfig(MaterialSystem_Config config, bool forceUpdate) {
		if (!ShaderDevice.IsUsingGraphics()) {
			ColorSpace.SetGamma(2.2f, 2.2f, IMaterialSystem.OVERBRIGHT, Config.AllowCheats, false);
		}
		return true;
	}

	private void ReadConfigFromConVars(MaterialSystem_Config config) {

	}

	public void ModShutdown() {

	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	unsafe static void raylibSpew(int logLevel, sbyte* text, sbyte* args) {
		//var message = Logging.GetLogMessage((nint)text, (nint)args);

		/*Dbg._SpewMessage((TraceLogLevel)logLevel switch {
			TraceLogLevel.Info => SpewType.Message,
			TraceLogLevel.Trace => SpewType.Log,
			TraceLogLevel.Debug => SpewType.Message,
			TraceLogLevel.Warning => SpewType.Warning,
			TraceLogLevel.Error => SpewType.Warning,
			TraceLogLevel.Fatal => SpewType.Error,
			_ => SpewType.Message,
		}, "raylib", 1, new Color(255, 255, 255), message + "\n");*/
	}

	public static ConVar mat_fullbright = new("0", FCvar.Cheat);
	public static ConVar mat_normalmaps = new("0", FCvar.Cheat);


	public static ConVar mat_specular = new("1", 0, "Enable/Disable specularity for perf testing.  Will cause a material reload upon change.");
	public static ConVar mat_bumpmap = new("1", 0);
	public static ConVar mat_phong = new("1", 0);
	public static ConVar mat_parallaxmap = new("1", FCvar.Hidden);
	public static ConVar mat_reducefillrate = new("0", 0);

	public uint DebugVarsSignature;

	public ITexture GetErrorTexture() => TextureSystem.ErrorTexture();
	public unsafe void BeginFrame(double frameTime) {
		if (!ThreadInMainThread() || IsInFrame())
			return;


		DebugVarsSignature = (uint)(
			((mat_specular.GetInt() != 0) ? 1 : 0)
			+ (mat_normalmaps.GetInt() << 1)
			+ (mat_fullbright.GetInt() << 2)
		);

		var renderContext = GetRenderContextInternal();

		renderContext.MarkRenderDataUnused(true);
		renderContext.BeginFrame();
		renderContext.SetFrameTime(frameTime);

		Assert(!InFrame);
		InFrame = true;
	}

	bool InFrame = false;


	private Matrix4x4 GetScreenMatrix() {
		launcherMgr.DisplayedSize(out int screenWidth, out _);
		int renderWidth = 0, renderHeight = 0;
		launcherMgr.RenderedSize(false, ref renderWidth, ref renderHeight);
		float scaleRatio = (float)renderWidth / (float)screenWidth;
		MathLib.MatrixBuildScale(out Matrix4x4 m, scaleRatio, scaleRatio, 1);
		return m;
	}

	public bool IsInFrame() => InFrame;

	public void EndFrame() {
		if (!ThreadInMainThread() || !IsInFrame())
			return;

		GetRenderContextInternal().EndFrame();

		Assert(InFrame);
		InFrame = false;
	}

	ulong FrameNum;

	public void SwapBuffers() {
		GetRenderContextInternal().SwapBuffers();
		FrameNum++;
	}

	ThreadLocal<MatRenderContext> matContext;
	public IMatRenderContext GetRenderContext() => matContext!.Value!;

	public bool SetMode(IWindow window, MaterialSystem_Config config) {
		int width = config.VideoMode.Width;
		int height = config.VideoMode.Height;

		bool previouslyUsingGraphics = ShaderDevice.IsUsingGraphics();
		ConvertModeStruct(config, out ShaderDeviceInfo info);
		if (!ShaderAPI.SetMode(window, in info))
			return false;

		TextureSystem.FreeStandardRenderTargets();
		TextureSystem.AllocateStandardRenderTargets();

		if (!previouslyUsingGraphics) {
			TextureSystem.RestoreRenderTargets();
			TextureSystem.RestoreNonRenderTargetTextures();
		}



		launcherMgr.RenderedSize(true, ref width, ref height);
		return true;
	}

	private void AllocateStandardTextures() {
		
	}

	private void ConvertModeStruct(MaterialSystem_Config config, out ShaderDeviceInfo mode) {
		mode = new ShaderDeviceInfo();
		mode.DisplayMode.Width = config.VideoMode.Width;
		mode.DisplayMode.Height = config.VideoMode.Height;
		mode.DisplayMode.Format = config.VideoMode.Format;
		mode.DisplayMode.RefreshRateNumerator = config.VideoMode.RefreshRate;
		mode.DisplayMode.RefreshRateDenominator = config.VideoMode.RefreshRate >= 0 ? 1 : 0;
		mode.BackBufferCount = 1;
		mode.AASamples = config.AASamples;
		mode.AAQuality = config.AAQuality;
		mode.Driver = config.Driver;
		mode.WindowedSizeLimitWidth = (int)config.WindowedSizeLimitWidth;
		mode.WindowedSizeLimitHeight = (int)config.WindowedSizeLimitHeight;

		mode.Windowed = config.Windowed();
		mode.Resizing = config.Resizing();
		mode.UseStencil = config.Stencil();
		mode.LimitWindowedSize = config.LimitWindowedSize();
		mode.WaitForVSync = config.WaitForVSync();
		mode.ScaleToOutputResolution = config.ScaleToOutputResolution();
		mode.UsingMultipleWindows = config.UsingMultipleWindows();
	}

	IMaterial IMaterialSystem.CreateMaterial(ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGroup, KeyValues keyValues) => CreateMaterial(materialName, textureGroup, keyValues);
	IMaterial IMaterialSystem.CreateMaterial(ReadOnlySpan<char> materialName, KeyValues keyValues) => CreateMaterial(materialName, TEXTURE_GROUP_OTHER, keyValues);

	public IMaterialInternal CreateMaterial(ReadOnlySpan<char> materialName, KeyValues? keyValues)
		=> CreateMaterial(materialName, MaterialDefines.TEXTURE_GROUP_OTHER, keyValues);
	public IMaterialInternal CreateMaterial(ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGroup, KeyValues? keyValues) {
		IMaterialInternal material;
		lock (this) {
			material = new Material(this, materialName, textureGroup, keyValues);
		}

		MaterialDict.AddMaterialToMaterialList(material);
		return material;
	}


	public IMaterial? GetCurrentMaterial() {
		return GetRenderContext().GetCurrentMaterial();
	}

	public bool CanUseEditorMaterials() {
		return false; //todo
	}

	public bool IsInStubMode() => false;

	public bool OnDrawMesh(IMesh mesh, int firstIndex, int indexCount) {
		if (IsInStubMode())
			return false;

		return GetRenderContextInternal().OnDrawMesh(mesh, firstIndex, indexCount);
	}

	public IMatRenderContextInternal GetRenderContextInternal() => matContext!.Value!;

	public bool InFlashlightMode() {
		return GetRenderContextInternal().InFlashlightMode();
	}

	public bool OnSetPrimitiveType(IMesh mesh, MaterialPrimitiveType type) {
		return GetRenderContextInternal().OnSetPrimitiveType(mesh, type);
	}

	public bool OnFlushBufferedPrimitives() {
		throw new NotImplementedException();
	}

	public void SyncMatrices() => GetRenderContextInternal().SyncMatrices();
	public void SyncMatrix(MaterialMatrixMode mode) => GetRenderContextInternal().SyncMatrix(mode);

	public ITexture FindTexture(ReadOnlySpan<char> textureName, ReadOnlySpan<char> textureGroupName, bool complain, int additionalCreationFlags) {
		ITextureInternal? texture = TextureSystem.FindOrLoadTexture(textureName, textureGroupName, additionalCreationFlags);
		Assert(texture != null);
		if (texture != null && texture.IsError()) {
			if (complain) {
				DevWarning($"Texture '{textureName}' not found.\n");
			}
		}

		return texture;
	}

	internal ReadOnlySpan<char> GetForcedTextureLoadPathID() {
		return "GAME";
	}
	public IMaterial FindMaterialEx(ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGroupName, MaterialFindContext context, bool complain, ReadOnlySpan<char> complainPrefix) {
		Span<char> tempNameBuffer = stackalloc char[materialName.Length];
		for (int i = 0; i < materialName.Length; i++) {
			char c = materialName[i];
			tempNameBuffer[i] = c == '\\' ? '/' : c;
		}
		IMaterialInternal? existingMaterial = MaterialDict.FindMaterial(tempNameBuffer, false);

		if (existingMaterial != null)
			return existingMaterial;

		Span<char> vmtName = stackalloc char["materials/".Length + tempNameBuffer.Length];
		"materials/".CopyTo(vmtName);
		tempNameBuffer.CopyTo(vmtName["materials/".Length..]);

		List<FileNameHandle_t>? includes = null;
		KeyValues keyValues = new("vmt");
		KeyValues patchKeyValues = new("vmt_patches");
		if (!Material.LoadVMTFile(FileSystem, ref keyValues, patchKeyValues, vmtName, true, null)) {
			keyValues = null!;
			patchKeyValues = null!;
		}
		else {
			int len = tempNameBuffer.Length + ".vmt".Length;
			Span<char> matNameWithExtension = stackalloc char[len];
			tempNameBuffer.CopyTo(matNameWithExtension);
			".vmt".CopyTo(matNameWithExtension[tempNameBuffer.Length..]);

			IMaterialInternal? mat = null;
			if (keyValues.Name.Equals("subrect", StringComparison.OrdinalIgnoreCase)) {
				mat = MaterialDict.AddMaterialSubRect(matNameWithExtension, textureGroupName, keyValues, patchKeyValues);
			}
			else {
				mat = MaterialDict.AddMaterial(matNameWithExtension, textureGroupName);
				if (ShaderDevice.IsUsingGraphics()) {
					mat.PrecacheVars(keyValues, patchKeyValues, includes, context);
					ForcedTextureLoadPathID = null;
				}
			}
			keyValues = null!;
			patchKeyValues = null!;

			return mat;
		}

		if (complain) {
			Assert(!tempNameBuffer.IsEmpty);

			if (MaterialDict.NoteMissing(vmtName)) {
				if (!complainPrefix.IsEmpty)
					DevWarning(complainPrefix);

				DevWarning($"material \"{vmtName}\" not found.\n");
			}
		}

		return errorMaterial;
	}

	public string? ForcedTextureLoadPathID;

	public IMaterial? FindMaterial(ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGroupName, bool complain, ReadOnlySpan<char> complainPrefix) {
		return FindMaterialEx(materialName, textureGroupName, MaterialFindContext.None, complain, complainPrefix);
	}

	public void CreateDebugMaterials() {
		KeyValues vmtKeyValues;


		vmtKeyValues = new("UnlitGeneric");
		vmtKeyValues.SetInt("$model", 1);
		vmtKeyValues.SetFloat("$decalscale", 0.05f);
		vmtKeyValues.SetString("$basetexture", "error");
		errorMaterial = CreateMaterial("___error.vmt", vmtKeyValues);
	}

	public IMaterial? FindProceduralMaterial(ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGroupName, KeyValues keyValues) {
		IMaterialInternal? material = MaterialDict.FindMaterial(materialName, true);
		if (keyValues != null) {
			if (material != null) {
				keyValues = null;
			}
			else {
				material = CreateMaterial(materialName, textureGroupName, keyValues);
			}

			return material;
		}
		else {
			if (material == null)
				return GetErrorMaterial();

			return material;
		}
	}

	private IMaterial? GetErrorMaterial() {
		throw new NotImplementedException();
	}

	public void RestoreShaderObjects(IServiceProvider services, int changeFlags) {
		if (services != null) {
			ShaderAPI = services.GetRequiredService<IShaderAPI>();
			ShaderDevice = services.GetRequiredService<IShaderDevice>();
		}

		foreach (var material in MaterialDict) {
			// material.ReportVarChanged TODO
		}

		TextureSystem.RestoreRenderTargets();
		Restore?.Invoke();
		TextureSystem.RestoreNonRenderTargetTextures();
	}

	// TODO: How much of this is needed these days... I'm fairly sure not a lot of it
	bool AllocatingRenderTargets;
	public void BeginRenderTargetAllocation() {
		AllocatingRenderTargets = true;
	}

	public void EndRenderTargetAllocation() {
		ShaderAPI.FlushBufferedPrimitives();
		AllocatingRenderTargets = false;

		// I believe this step is unnecessary (and breaks how textures work rn)
		/*
		if (ShaderAPI.CanDownloadTextures()) {
			ShaderDevice.ReleaseResources();
			ShaderDevice.ReacquireResources();
		}
		*/
	}

	public ITexture CreateProceduralTexture(ReadOnlySpan<char> textureName, ReadOnlySpan<char> textureGroup, int wide, int tall, ImageFormat format, TextureFlags flags) {
		return TextureSystem.CreateProceduralTexture(textureName, textureGroup, wide, tall, 1, format, flags)!;
	}

	public ITexture? CreateNamedRenderTargetTextureEx(ReadOnlySpan<char> rtName, int w, int h, RenderTargetSizeMode sizeMode, ImageFormat format, MaterialRenderTargetDepth depthMode, TextureFlags textureFlags, CreateRenderTargetFlags renderTargetFlags) {
		RenderTargetType rtType;

		switch (depthMode) {
			case MaterialRenderTargetDepth.Separate:
				rtType = RenderTargetType.WithDepth;
				break;
			case MaterialRenderTargetDepth.None:
				rtType = RenderTargetType.NoDepth;
				break;
			case MaterialRenderTargetDepth.Only:
				rtType = RenderTargetType.OnlyDepth;
				break;
			case MaterialRenderTargetDepth.Shared:
			default:
				rtType = RenderTargetType.RenderTarget;
				break;
		}

		ITextureInternal? tex = TextureSystem.CreateRenderTargetTexture(rtName, w, h, sizeMode, format, rtType, textureFlags, renderTargetFlags);

		if (!AllocatingRenderTargets)
			EndRenderTargetAllocation();

		return tex;
	}

	int RT_FB_WidthOverride;
	int RT_FB_HeightOverride;

	public void GetRenderTargetFrameBufferDimensions(out int fbWidth, out int fbHeight) {
		if (RT_FB_WidthOverride > 0 && RT_FB_HeightOverride > 0) {
			fbWidth = RT_FB_WidthOverride;
			fbHeight = RT_FB_HeightOverride;
		}
		else ShaderAPI.GetBackBufferDimensions(out fbWidth, out fbHeight);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetNumSortIDs() => MatLightmaps.GetNumSortIDs();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void GetSortInfo(Span<MaterialSystem_SortInfo> sortInfoArray) => MatLightmaps.GetSortInfo(sortInfoArray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void BeginLightmapAllocation() => MatLightmaps.BeginLightmapAllocation();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void EndLightmapAllocation() {
		MatLightmaps.EndLightmapAllocation();
		AllocateStandardTextures();
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public short AllocateLightmap(int allocationWidth, int allocationHeight, Span<int> offsetIntoLightmapPage, IMaterial? material) => (short)MatLightmaps.AllocateLightmap(allocationWidth, allocationHeight, offsetIntoLightmapPage, material);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public short AllocateWhiteLightmap(IMaterial? material) => (short)MatLightmaps.AllocateWhiteLightmap(material);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void GetLightmapPageSize(int lightmap, out int width, out int height) => MatLightmaps.GetLightmapPageSize(lightmap, out width, out height);

	public void UpdateLightmap(int lightmapPageID, Span<int> lightmapSize, Span<int> offsetIntoLightmapPage, Span<float> floatImage, Span<float> floatImageBump1, Span<float> floatImageBump2, Span<float> floatImageBump3)
		=> MatLightmaps.UpdateLightmap(lightmapPageID, lightmapSize, offsetIntoLightmapPage, floatImage, floatImageBump1, floatImageBump2, floatImageBump3);


	public void GetBackBufferDimensions(out int width, out int height) => ShaderAPI.GetBackBufferDimensions(out width, out height);

	public void BeginUpdateLightmaps() => MatLightmaps.BeginUpdateLightmaps();
	public void EndUpdateLightmaps() => MatLightmaps.EndUpdateLightmaps();

	public void BindStandardTexture(Sampler sampler, StandardTextureId id) => GetRenderContext().BindStandardTexture(sampler, id);

	public IMaterialProxy? DetermineProxyReplacements(Material material, KeyValues fallbackKeyValues) {
		throw new NotImplementedException();
	}

	IMaterialProxyFactory? MaterialProxyFactory;

	public IMaterialProxyFactory? GetMaterialProxyFactory() => MaterialProxyFactory;
	public void SetMaterialProxyFactory(IMaterialProxyFactory? factory) {
		UncacheAllMaterials();
		MaterialProxyFactory = factory;
	}

	private void UncacheAllMaterials() {
		// todo: finish me!!
		foreach(var material in MaterialDict){
			material.Uncache();
		}
	}

	public event Action? Restore;

	public IMaterialInternal errorMaterial;
	public readonly MatLightmaps MatLightmaps;
}

public enum MatrixStackFlags : uint
{
	Dirty = 1 << 0
}
public struct MatrixStackItem
{
	public Matrix4x4 Matrix;
}

public struct RenderTargetStackElement
{
	public ITexture? RenderTarget0;
	public ITexture? RenderTarget1;
	public ITexture? RenderTarget2;
	public ITexture? RenderTarget3;

	public readonly ITexture? this[int index] => index switch {
		0 => RenderTarget0,
		1 => RenderTarget1,
		2 => RenderTarget2,
		3 => RenderTarget3,
		_ => null
	};

	public ITexture? DepthTexture;

	public int ViewX;
	public int ViewY;
	public int ViewW;
	public int ViewH;

	public readonly int Size =>
		(RenderTarget0 != null ? 1 : 0) +
		(RenderTarget1 != null ? 1 : 0) +
		(RenderTarget2 != null ? 1 : 0) +
		(RenderTarget3 != null ? 1 : 0);

	public RenderTargetStackElement(int viewX, int viewY, int viewW, int viewH) {
		this.ViewX = viewX;
		this.ViewY = viewY;
		this.ViewW = viewW;
		this.ViewH = viewH;
	}
	public RenderTargetStackElement(ITexture? rt0, int viewX, int viewY, int viewW, int viewH) : this(viewX, viewY, viewW, viewH) {
		RenderTarget0 = rt0;
	}
	public RenderTargetStackElement(ITexture? rt0, ITexture? rt1, int viewX, int viewY, int viewW, int viewH) : this(viewX, viewY, viewW, viewH) {
		RenderTarget0 = rt0;
		RenderTarget1 = rt1;
	}
	public RenderTargetStackElement(ITexture? rt0, ITexture? rt1, ITexture? rt2, int viewX, int viewY, int viewW, int viewH) : this(viewX, viewY, viewW, viewH) {
		RenderTarget0 = rt0;
		RenderTarget1 = rt1;
		RenderTarget2 = rt2;
	}
	public RenderTargetStackElement(ITexture? rt0, ITexture? rt1, ITexture? rt2, ITexture? rt3, int viewX, int viewY, int viewW, int viewH) : this(viewX, viewY, viewW, viewH) {
		RenderTarget0 = rt0;
		RenderTarget1 = rt1;
		RenderTarget2 = rt2;
		RenderTarget3 = rt3;
	}
}
