using Microsoft.Extensions.DependencyInjection;

using Source.Common;
using Source.Common.Bitmap;
using Source.Common.Commands;
using Source.Common.Engine;
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

	readonly static ConVar mat_vsync = new("mat_vsync", "0", 0, "Force sync to vertical retrace", 0, 1);
	readonly static ConVar mat_forcehardwaresync = new(IsPC() ? "1" : "0", 0);
	readonly static ConVar mat_trilinear = new("0", 0);
	readonly static ConVar mat_forceaniso = new("1", FCvar.Archive); // 0 = Bilinear, 1 = Trilinear, 2+ = Aniso
	readonly static ConVar mat_filterlightmaps = new("1", 0);
	readonly static ConVar mat_filtertextures = new("1", 0);
	readonly static ConVar mat_mipmaptextures = new("1", 0);
	readonly static ConVar mat_vrmode_adapter = new("-1", 0);
	readonly static ConVar mat_showmiplevels = new("0", FCvar.Cheat, "color-code miplevels 2: normalmaps, 1: everything else"); // , callback: mat_showmiplevels_Callback_f
	readonly static ConVar mat_specular = new("1", 0, "Enable/Disable specularity for perf testing.  Will cause a material reload upon change.");
	readonly static ConVar mat_bumpmap = new("1", 0);
	readonly static ConVar mat_phong = new("1", 0);
	readonly static ConVar mat_parallaxmap = new("1", FCvar.Hidden | 0);
	readonly static ConVar mat_reducefillrate = new("0", 0);
	readonly static ConVar mat_picmip = new("0", FCvar.Archive, "", -1, 4);
	readonly static ConVar mat_slopescaledepthbias_normal = new("0.0f", FCvar.Cheat);
	readonly static ConVar mat_depthbias_normal = new("0.0f", FCvar.Cheat | 0);
	readonly static ConVar mat_slopescaledepthbias_decal = new("-0.5", FCvar.Cheat); // Reciprocals of these biases sent to API
	readonly static ConVar mat_depthbias_decal = new("-262144", FCvar.Cheat | 0);
	readonly static ConVar mat_slopescaledepthbias_shadowmap = new("16", FCvar.Cheat);
	readonly static ConVar mat_depthbias_shadowmap = new("0.0005", FCvar.Cheat);
	readonly static ConVar mat_monitorgamma = new("2.2", FCvar.Archive, "monitor gamma (typically 2.2 for CRT and 1.7 for LCD)", 1.6f, 2.6f);
	readonly static ConVar mat_monitorgamma_tv_range_min = new("16", 0);
	readonly static ConVar mat_monitorgamma_tv_range_max = new("255", 0);
	readonly static ConVar mat_monitorgamma_tv_exp = new("2.5", 0, "", 1.0f, 4.0f);
	readonly static ConVar mat_monitorgamma_tv_enabled = new("0", FCvar.Archive, "");
	readonly static ConVar mat_antialias = new("0", FCvar.Archive);
	readonly static ConVar mat_aaquality = new("0", FCvar.Archive);
	readonly static ConVar mat_diffuse = new("1", FCvar.Cheat);
	readonly static ConVar mat_showlowresimage = new("0", FCvar.Cheat);
	readonly static ConVar mat_fullbright = new("0", FCvar.Cheat);
	readonly static ConVar mat_normalmaps = new("0", FCvar.Cheat);
	readonly static ConVar mat_measurefillrate = new("0", FCvar.Cheat);
	readonly static ConVar mat_fillrate = new("0", FCvar.Cheat);
	readonly static ConVar mat_reversedepth = new("0", FCvar.Cheat);
	readonly static ConVar mat_bufferprimitives = new("1", 0);
	readonly static ConVar mat_drawflat = new("0", FCvar.Cheat);
	readonly static ConVar mat_softwarelighting = new("0", 0);
	readonly static ConVar mat_proxy = new("0", FCvar.Cheat, ""); // , callback: MatProxyCallback
	readonly static ConVar mat_norendering = new("0", FCvar.Cheat);
	readonly static ConVar mat_compressedtextures = new("1", 0);
	readonly static ConVar mat_fastspecular = new("1", 0, "Enable/Disable specularity for visual testing.  Will not reload materials and will not affect perf.");
	readonly static ConVar mat_fastnobump = new("0", FCvar.Cheat); // Binds 1-texel normal map for quick internal testing

	// These are not controlled by the material system, but are limited by settings in the material system
	readonly static ConVar r_shadowrendertotexture = new("0", FCvar.Archive);
	readonly static ConVar r_flashlightdepthtexture = new("1", 0);
	readonly static ConVar r_waterforceexpensive = new("0", FCvar.Archive);
	readonly static ConVar r_waterforcereflectentities = new("0", 0);
	readonly static ConVar mat_motion_blur_enabled = new("0", FCvar.Archive);

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

	public bool UpdateConfig(bool forceUpdate) {
		MaterialSystem_Config config = new();
		Config.CopyInstantiatedReferenceTo(config);
		ReadConfigFromConVars(config);
		return OverrideConfig(config, forceUpdate);
	}

	public bool OverrideConfig(MaterialSystem_Config config, bool forceUpdate) {
		bool redownloadLightmaps = false;
		bool redownloadTextures = false;
		bool recomputeSnapshots = false;
		bool reloadMaterials = false;
		bool resetAnisotropy = false;
		bool setStandardVertexShaderConstants = false;
		bool monitorGammaChanged = false;
		bool videoModeChange = false;
		bool resetTextureFilter = false;


		if (!ShaderDevice.IsUsingGraphics()) {
			// Config = config;
			config.CopyInstantiatedReferenceTo(Config);

			ColorSpace.SetGamma(2.2f, 2.2f, IMaterialSystem.OVERBRIGHT, Config.AllowCheats, false);

			return redownloadLightmaps;
		}

		// ShaderAPI.SetDefaultState();

		// if (config.HDREnabled() != Config.HDREnabled()) {
		// 	forceUpdate = true;
		// 	reloadMaterials = true;
		// }

		if (config.ShadowDepthTexture != Config.ShadowDepthTexture) {
			forceUpdate = true;
			reloadMaterials = true;
			recomputeSnapshots = true;
		}

		// Don't use compressed textures for the moment if we don't support them
		if (!HardwareConfig.SupportsCompressedTextures())
			config.CompressedTextures = false;

		if (forceUpdate) {
			MatLightmaps.EnableLightmapFiltering(config.FilterLightmaps);
			recomputeSnapshots = true;
			redownloadLightmaps = true;
			redownloadTextures = true;
			resetAnisotropy = true;
			setStandardVertexShaderConstants = true;
		}

		// toggle bump mapping
		if (config.DisableSpecular() != Config.DisableSpecular() || config.DisablePhong() != Config.DisablePhong()) {
			recomputeSnapshots = true;
			reloadMaterials = true;
			resetAnisotropy = true;
		}

		// toggle specularity
		if (config.DisableSpecular() != Config.DisableSpecular()) {
			recomputeSnapshots = true;
			reloadMaterials = true;
			resetAnisotropy = true;
		}

		// toggle parallax mapping
		if (config.EnableParallaxMapping() != Config.EnableParallaxMapping())
			reloadMaterials = true;

		// Reload materials if we want reduced fillrate
		if (config.ReduceFillrate() != Config.ReduceFillrate())
			reloadMaterials = true;

		// toggle reverse depth
		if (config.ReverseDepth != Config.ReverseDepth) {
			recomputeSnapshots = true;
			resetAnisotropy = true;
		}

		// toggle no transparency
		if (config.NoTransparency != Config.NoTransparency) {
			recomputeSnapshots = true;
			resetAnisotropy = true;
		}

		// toggle lightmap filtering
		if (config.FilterLightmaps != Config.FilterLightmaps)
			MatLightmaps.EnableLightmapFiltering(config.FilterLightmaps);

		// toggle software lighting
		if (config.SoftwareLighting != Config.SoftwareLighting)
			reloadMaterials = true;

		// generic things that cause us to redownload textures
		if (config.AllowCheats != Config.AllowCheats ||
				config.SkipMipLevels != Config.SkipMipLevels ||
				config.ShowMipLevels != Config.ShowMipLevels ||
				((config.CompressedTextures != Config.CompressedTextures) && HardwareConfig.SupportsCompressedTextures()) ||
				config.ShowLowResImage != Config.ShowLowResImage) {

			redownloadTextures = true;
			recomputeSnapshots = true;
			resetAnisotropy = true;
		}

		if (config.ForceTrilinear() != Config.ForceTrilinear())
			resetTextureFilter = true;

		if (config.ForceAnisotropicLevel != Config.ForceAnisotropicLevel) {
			resetAnisotropy = true;
			resetTextureFilter = true;
		}

		if (config.MonitorGamma != Config.MonitorGamma || config.GammaTVRangeMin != Config.GammaTVRangeMin ||
				config.GammaTVRangeMax != Config.GammaTVRangeMax || config.GammaTVExponent != Config.GammaTVExponent ||
				config.GammaTVEnabled != Config.GammaTVEnabled)
			monitorGammaChanged = true;

		if (config.VideoMode.Width != Config.VideoMode.Width ||
				config.VideoMode.Height != Config.VideoMode.Height ||
				config.VideoMode.RefreshRate != Config.VideoMode.RefreshRate ||
				config.AASamples != Config.AASamples ||
				config.AAQuality != Config.AAQuality ||
				config.Windowed() != Config.Windowed() ||
				config.Stencil() != Config.Stencil())
			videoModeChange = true;

		if (!config.Windowed() && (config.WaitForVSync() != Config.WaitForVSync()))
			videoModeChange = true;

		// Config = config;

		if (redownloadTextures || redownloadLightmaps)
			ColorSpace.SetGamma(2.2f, 2.2f, IMaterialSystem.OVERBRIGHT, Config.AllowCheats, false);

		if (resetAnisotropy || recomputeSnapshots || redownloadLightmaps ||
				redownloadTextures || resetAnisotropy || videoModeChange ||
				setStandardVertexShaderConstants || resetTextureFilter) {
			// ForceSingleThreaded();
		}

		if (reloadMaterials)
			ReloadMaterials();

		if (redownloadTextures) {
			if (ShaderAPI.CanDownloadTextures()) {
				TextureSystem.RestoreRenderTargets();
				TextureSystem.RestoreNonRenderTargetTextures();
			}
		}
		else if (resetTextureFilter) {
			// TextureSystem.ResetTextureFilteringState();
		}

		// Recompute all state snapshots
		if (recomputeSnapshots)
			RecomputeAllStateSnapshots();

		// if (resetAnisotropy)
		// ShaderAPI.SetAnisotropicLevel(config.ForceAnisotropicLevel);

		// if (setStandardVertexShaderConstants)
		// ShaderAPI.SetStandardVertexShaderConstants(IMaterialSystem.OVERBRIGHT);

		if (monitorGammaChanged) {
			// 	ShaderDevice.SetHardwareGammaRamp(config.MonitorGamma, config.GammaTVRangeMin, config.GammaTVRangeMax, config.GammaTVExponent, config.GammaTVEnabled);
		}

		if (videoModeChange) {
			ConvertModeStruct(config, out ShaderDeviceInfo info);
			ShaderAPI.ChangeVideoMode(info);
		}

		config.CopyInstantiatedReferenceTo(Config);

		// if (videoModeChange)
		// ForceSingleThreaded();

		return redownloadLightmaps;
	}

	private void ReadConfigFromConVars(MaterialSystem_Config config) {
		config.SetFlag(MaterialSystem_Config_Flags.NoWaitForVSync, !mat_vsync.GetBool());
		config.SetFlag(MaterialSystem_Config_Flags.ForceTrilinear, mat_trilinear.GetBool());
		config.SetFlag(MaterialSystem_Config_Flags.DisableSpecular, !mat_specular.GetBool());
		config.SetFlag(MaterialSystem_Config_Flags.DisableBumpmap, !mat_bumpmap.GetBool());
		config.SetFlag(MaterialSystem_Config_Flags.DisableBumpmap, !mat_phong.GetBool());
		config.SetFlag(MaterialSystem_Config_Flags.EnableParallaxMapping, mat_parallaxmap.GetBool());
		config.SetFlag(MaterialSystem_Config_Flags.ReduceFillrate, mat_reducefillrate.GetBool());
		config.ForceAnisotropicLevel = Math.Max(mat_forceaniso.GetInt(), 1);
		config.SkipMipLevels = mat_picmip.GetInt();
		config.SetFlag(MaterialSystem_Config_Flags.ForceHardwareSync, mat_forcehardwaresync.GetBool());
		config.SlopeScaleDepthBias_Decal = mat_slopescaledepthbias_decal.GetFloat();
		config.DepthBias_Decal = mat_depthbias_decal.GetFloat();
		config.SlopeScaleDepthBias_Normal = mat_slopescaledepthbias_normal.GetFloat();
		config.DepthBias_Normal = mat_depthbias_normal.GetFloat();
		config.SlopeScaleDepthBias_ShadowMap = mat_slopescaledepthbias_shadowmap.GetFloat();
		config.DepthBias_ShadowMap = mat_depthbias_shadowmap.GetFloat();
		config.MonitorGamma = mat_monitorgamma.GetFloat();
		config.GammaTVRangeMin = mat_monitorgamma_tv_range_min.GetFloat();
		config.GammaTVRangeMax = mat_monitorgamma_tv_range_max.GetFloat();
		config.GammaTVExponent = mat_monitorgamma_tv_exp.GetFloat();
		config.GammaTVEnabled = mat_monitorgamma_tv_enabled.GetBool();
		config.AASamples = mat_antialias.GetInt();
		config.AAQuality = mat_aaquality.GetInt();
		config.ShowDiffuse = mat_diffuse.GetBool();
		config.ShowNormalMap = mat_normalmaps.GetBool();
		config.ShowLowResImage = mat_showlowresimage.GetBool();
		config.MeasureFillRate = mat_measurefillrate.GetBool();
		config.VisualizeFillRate = mat_fillrate.GetBool();
		config.FilterLightmaps = mat_filterlightmaps.GetBool();
		config.FilterTextures = mat_filtertextures.GetBool();
		config.MipMapTextures = mat_mipmaptextures.GetBool();
		config.ShowMipLevels = (sbyte)mat_showmiplevels.GetInt();
		config.ReverseDepth = mat_reversedepth.GetBool();
		config.BufferPrimitives = mat_bufferprimitives.GetBool();
		config.DrawFlat = mat_drawflat.GetBool();
		config.SoftwareLighting = mat_softwarelighting.GetBool();
		config.ProxiesTestMode = (byte)mat_proxy.GetInt();
		config.SuppressRendering = mat_norendering.GetInt() != 0;
		config.CompressedTextures = mat_compressedtextures.GetBool();
		config.ShowSpecular = mat_fastspecular.GetBool();
		config.Fullbright = (byte)mat_fullbright.GetInt();
		config.FastNoBump = mat_fastnobump.GetInt() != 0;
		config.MotionBlur = mat_motion_blur_enabled.GetBool();
		config.SupportFlashlight = true; // mat_supportflashlight.GetBool();
		config.ShadowDepthTexture = r_flashlightdepthtexture.GetBool();
		config.SetFlag(MaterialSystem_Config_Flags.ENABLE_HDR, HardwareConfig.GetHDREnabled());
	}

	public int GetDisplayAdapterCount() {
		throw new NotImplementedException();
	}

	public int GetCurrentAdapter() => ShaderDevice.GetCurrentAdapter();

	public int GetModeCount(int adapter) => ShaderDevice.GetModeCount(adapter);
	public void GetModeInfo(int adapter, int mode, out MaterialVideoMode info) {
		ShaderDevice.GetModeInfo(adapter, mode, out ShaderDisplayMode shaderInfo);
		info.Width = shaderInfo.Width;
		info.Height = shaderInfo.Height;
		info.Format = shaderInfo.Format;
		info.RefreshRate = (int)(shaderInfo.RefreshRateNumerator / (double)shaderInfo.RefreshRateDenominator);
	}

	public void ModShutdown() {

	}

#if !SWDS
	[ConCommand("mat_setvideomode", "sets the width, height, windowed state of the material system")]
	static void mat_setvideomode(in TokenizedCommand args) {
		if (args.ArgC() != 4)
			return;

		int width = args.Arg(1, 0);
		int height = args.Arg(2, 0);
		int windowed = args.Arg(3, 0);

		Singleton<IVideoMode>().SetMode(width, height, windowed != 0);
	}
#endif

	[ConCommand("mat_savechanges", "saves current video configuration to the registry")]
	static void mat_savechanges(in TokenizedCommand args) {
		// commandLine.RemoveParm("-safe");
		UpdateMaterialSystemConfig();
	}

	static void UpdateMaterialSystemConfig() {
		// if (host_state.worldbrush && !host_state.worldbrush->lightdata) {
		// 	mat_fullbright.SetValue(1);
		// }

		bool lightmapsNeedReloading = Singleton<IMaterialSystem>().UpdateConfig(false); //fixme
		if (lightmapsNeedReloading) {

		}
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

	public uint DebugVarsSignature;

	public ITexture GetErrorTexture() => TextureSystem.ErrorTexture();
	public void BeginFrame(double frameTime) {
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

	public MaterialSystem_Config GetCurrentConfigForVideoCard() => Config;

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

	public void AddModeChangeCallBack(Action func) => ShaderAPI.AddModeChangeCallBack(func);

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

	void ReleaseShaderObjects() {
		// todo
	}

	public void RestoreShaderObjects(IServiceProvider? services, int changeFlags) {
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
		foreach (var material in MaterialDict) {
			material.Uncache();
		}
	}

	void ReloadTextures() {
		// todo
	}

	void ReloadMaterials(ReadOnlySpan<char> subString = default) {
		// todo
	}

	void RecomputeAllStateSnapshots() {
		// todo
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
