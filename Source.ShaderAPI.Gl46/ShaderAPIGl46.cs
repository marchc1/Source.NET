using K4os.Hash.xxHash;

using Microsoft.Extensions.DependencyInjection;

using OpenGL;

using Source.Bitmap;
using Source.Common;
using Source.Common.Bitmap;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Launcher;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Common.ShaderAPI;
using Source.Common.Utilities;

using System.Numerics;
using System.Text;
using System.Threading;

namespace Source.ShaderAPI.Gl46;

/// <summary>
/// Standardized OpenGL UBO binding locations. All uniform buffers are loaded with layout std140
/// </summary>
public enum UniformBufferBindingLocation
{
	/// <summary><b>source_matrices</b>: The model, view, and projection matrices</summary>
	SharedMatrices = 0,
	/// <summary><b>source_base_sharedUBO</b>: Shared uniforms that every shader can use.</summary>
	SharedBaseShader = 1,
	/// <summary><b>source_vertex_sharedUBO</b>: Shared uniforms that every vertex shader can use.</summary>
	SharedVertexShader = 2,
	/// <summary><b>source_pixel_sharedUBO</b>: Shared uniforms that every pixel shader can use.</summary>
	SharedPixelShader = 3,
	/// <summary><b>source_bone_matrices</b>: A <see cref="Matrix4x4"/>[<see cref="Studio.MAXSTUDIOBONES"/>] array.</summary>
	SharedBoneMatrices = 4
}

public struct GfxViewport
{
	public int X;
	public int Y;
	public int Width;
	public int Height;
	public float MinZ;
	public float MaxZ;
}

public enum CommitFuncType
{
	PerDraw,
	PerPass
}

public class ShaderAPIGl46 : IShaderAPI, IShaderDevice, IDebugTextureInfo
{
	public MeshMgr MeshMgr;
	public IShaderSystem ShaderManager;

	public static void DLLInit(IServiceCollection services) {
		services.AddSingleton(x => (IDebugTextureInfo)(ShaderAPIGl46)x.GetRequiredService<IShaderAPI>());
		services.AddSingleton(x => x.GetRequiredService<IShaderAPI>().GetShaderDevice());
		services.AddSingleton<IMeshMgr, MeshMgr>();
		services.AddSingleton<IMaterialSystemHardwareConfig, HardwareConfig>();
		services.AddSingleton<IShaderSystem, ShaderSystem>();
		services.AddSingleton<MaterialSystem_Config>();
		services.AddSingleton<MeshMgr>();
	}

	public GraphicsDriver GetDriver() => Driver;
	private bool ready;

	uint renderFBO;

	public bool OnDeviceInit() {
		AcquireInternalRenderTargets();

		CreateMatrixStacks();

		MeshMgr.MaterialSystem = Singleton<IMaterialSystem>();
		MeshMgr.ShaderAPI = this;
		MeshMgr.Init();
		Device!.SetSwapInterval(0);

		InitRenderState();

		ClearColor4ub(0, 0, 0, 1);
		ClearBuffers(true, true, true, -1, -1);

		return true;
	}

	DeviceState DeviceState = DeviceState.OK;

	public void ClearBuffers(bool clearColor, bool clearDepth, bool clearStencil, int renderTargetWidth = -1, int renderTargetHeight = -1) {
		if (IsDeactivated())
			return;

		FlushBufferedPrimitives();
		uint flags = 0;

		if (clearColor)
			flags |= GL_COLOR_BUFFER_BIT;
		if (clearDepth)
			flags |= GL_DEPTH_BUFFER_BIT;
		if (clearStencil)
			flags |= GL_STENCIL_BUFFER_BIT;

		if (clearDepth)
			glDepthMask(true);
		if (clearStencil)
			glStencilMask(0xFF);

		glClear(flags);
	}

	public void ClearColor3ub(byte r, byte g, byte b) => glClearColor(r / 255f, g / 255f, b / 255f, 1);
	public void ClearColor4ub(byte r, byte g, byte b, byte a) => glClearColor(r / 255f, g / 255f, b / 255f, a / 255f);

	VertexShaderHandle activeVertexShader = VertexShaderHandle.INVALID;
	PixelShaderHandle activePixelShader = PixelShaderHandle.INVALID;
	bool pipelineChanged = false;
	uint lastShader;
	public void BindVertexShader(in VertexShaderHandle vertexShader) {
		activeVertexShader = vertexShader;
		pipelineChanged = true;
	}

	public void BindPixelShader(in PixelShaderHandle pixelShader) {
		activePixelShader = pixelShader;
		pipelineChanged = true;
	}

	public void CallCommitFuncs(CommitFuncType func, bool usingFixedFunction, bool force = false) {

	}

	uint uboMatrices;
	uint uboBones;

	private unsafe void CreateMatrixStacks() {
		uboMatrices = glCreateBuffer();
		glObjectLabel(GL_BUFFER, uboMatrices, "ShaderAPI Shared Matrix UBO");
		glNamedBufferData(uboMatrices, sizeof(Matrix4x4) * 3, null, GL_DYNAMIC_DRAW);
		glBindBufferBase(GL_UNIFORM_BUFFER, (int)UniformBufferBindingLocation.SharedMatrices, uboMatrices);

		uboBones = glCreateBuffer();
		glObjectLabel(GL_BUFFER, uboBones, "ShaderAPI Shared Bone UBO");

		Matrix4x4* identityMatrices = stackalloc Matrix4x4[Studio.MAXSTUDIOBONES];
		for (int i = 0; i < Studio.MAXSTUDIOBONES; i++)
			identityMatrices[i] = Matrix4x4.Identity;

		glNamedBufferData(uboBones, sizeof(Matrix4x4) * Studio.MAXSTUDIOBONES, identityMatrices, GL_DYNAMIC_DRAW);
		glBindBufferBase(GL_UNIFORM_BUFFER, (int)UniformBufferBindingLocation.SharedBoneMatrices, uboBones);
	}

	private void AcquireInternalRenderTargets() {
		renderFBO = glCreateFramebuffer();
	}

	public void InitRenderState() {
		glDisable(GL_DEPTH_TEST);

		if (!IsDeactivated())
			ResetRenderState();
	}

	public void SetPresentParameters(in ShaderDeviceInfo info) {
		PresentParameters = info;
	}

	private void ResetRenderState(bool fullReset = true) {
		if (fullReset) {
			InitVertexAndPixelShaders();
		}

		int width = 0, height = 0;
		Singleton<ILauncherManager>().DisplayedSize(out width, out height);
		ShaderViewport viewport = new ShaderViewport(0, 0, width, height);
		SetViewports(new(ref viewport));
	}

	public IShaderShadow NewShaderShadow(ReadOnlySpan<char> materialName) => new ShadowStateGl46(this, (IShaderSystemInternal)ShaderManager, materialName);

	private void InitVertexAndPixelShaders() {
		// TODO; everything before this call
		ShaderManager.ResetShaderState();
	}

	public MaterialFogMode GetSceneFogMode() {
		return SceneFogMode;
	}

	MaterialFogMode SceneFogMode = MaterialFogMode.None;

	internal IShaderUtil ShaderUtil;

	public bool InFlashlightMode() {
		return ShaderUtil.InFlashlightMode();
	}


	MeshGl46? RenderMesh;
	IMaterialInternal? Material;
	uint CombobulateShadersIfChanged() {
		uint program;
		if (pipelineChanged) {
			program = ShaderCombobulator();
			if (lastShader != program) {
				lastShader = program;
				glUseProgram(program);
			}
		}

		pipelineChanged = false;
		return lastShader;
	}

	public void RenderPass() {
		if (IsDeactivated())
			return;

		CombobulateShadersIfChanged();

		if (RenderMesh != null)
			RenderMesh.RenderPass();
		else
			MeshMgr.RenderPassWithVertexAndIndexBuffers();
	}

	Dictionary<ulong, uint> shaderCombinations = [];
	private unsafe uint ShaderCombobulator() {
		// Determines the shader program used given the current shader handles.
		// If one does not exist, it is created.
		Span<nint> hashedData = [activeVertexShader.Handle, activePixelShader.Handle];
		ulong hash;
		fixed (nint* data = hashedData)
			hash = XXH64.DigestOf(data, hashedData.Length * sizeof(nint), 0);

		if (shaderCombinations.TryGetValue(hash, out var program))
			return program; // We have already linked a program for this shader combination

		// We need to create a program then
		program = glCreateProgram();
		glAttachShader(program, (uint)activeVertexShader.Handle);
		glAttachShader(program, (uint)activePixelShader.Handle);
		glLinkProgram(program);
		// Even invalid program states should be hashed... for now.
		// Maybe a time based thing for invalid programs, to try allowing for the shader developer to recover, etc...
		shaderCombinations[hash] = program;

		if (!ShaderSystem.IsValidProgram(program, out string? error)) {
			Warning("WARNING: Shader combobulation linker error.\n");
			Warning(error);
			Warning("\n");
			return 0;
		}

		return program;
	}

	private bool IsDeactivated() {
		return DeviceState != DeviceState.OK;
	}

	public void InvalidateDelayedShaderConstraints() {
		// TODO FIXME
	}

	public enum TransformType
	{
		IsIdentity = 0,
		IsCameraToWorld,
		IsGeneral
	}


	public void PushMatrix() {
		if (MatrixIsChanging()) {

		}
	}

	private bool MatrixIsChanging(TransformType type = TransformType.IsGeneral) {
		if (IsDeactivated())
			return false;

		if (type != TransformType.IsGeneral)
			return false;

		FlushBufferedPrimitivesInternal();

		return true;
	}
	public void FlushBufferedPrimitives() => FlushBufferedPrimitivesInternal();
	private void FlushBufferedPrimitivesInternal() {
		Assert(RenderMesh == null);
#if DEBUG
		//glDebugMessageInsert(GL_DEBUG_SOURCE_APPLICATION, GL_DEBUG_TYPE_MARKER, 0, GL_DEBUG_SEVERITY_LOW, "FlushBufferedPrimitivesInternal: executing...");
#endif
		MeshMgr.Flush();
#if DEBUG
		//glDebugMessageInsert(GL_DEBUG_SOURCE_APPLICATION, GL_DEBUG_TYPE_MARKER, 0, GL_DEBUG_SEVERITY_LOW, "FlushBufferedPrimitivesInternal: complete");
#endif
	}

	public void PopMatrix() {
		if (MatrixIsChanging()) {
			UpdateMatrixTransform();
		}
	}

	private void UpdateMatrixTransform() {

	}

	public void DrawMesh(IMesh imesh) {
		MeshGl46 mesh = (MeshGl46)imesh!;
		RenderMesh = mesh;
		VertexFormat vertexFormat = RenderMesh.GetVertexFormat();
		SetVertexDecl(vertexFormat, RenderMesh.HasColorMesh(), RenderMesh.HasFlexMesh(), Material!.IsUsingVertexID());
		CommitStateChanges();
		Material!.DrawMesh(VertexCompressionType.None);
		RenderMesh = null;
	}

	private void CommitStateChanges() {
		// todo
	}

	private void SetVertexDecl(VertexFormat vertexFormat, bool hasColorMesh, bool hasFleshMesh, bool usingMorph) {
		// Gl46.glVertexAttribPointer() i think we need here
	}

	public IMesh GetDynamicMesh(IMaterial material, int hwSkinBoneCount, bool buffered, IMesh? vertexOverride, IMesh? indexOverride) {
		Assert(material == null || material.IsRealTimeVersion());
		return MeshMgr.GetDynamicMesh(material, 0, hwSkinBoneCount, buffered, vertexOverride, indexOverride);
	}

	public void Bind(IMaterial? material) {
		IMaterialInternal? matInt = (IMaterialInternal?)material;

		bool materialChanged;
		if (Material != null && matInt != null && Material.InMaterialPage() && matInt.InMaterialPage()) {
			materialChanged = (Material.GetMaterialPage() != matInt.GetMaterialPage());
		}
		else {
			materialChanged = (Material != matInt) || (Material != null && Material.InMaterialPage()) || (matInt != null && matInt.InMaterialPage());
		}

		if (materialChanged) {
			FlushBufferedPrimitives();
			Material = matInt;
		}
	}

	public void SetSkinningMatrices() {
		// TODO
	}

	public void ShadeMode(ShadeMode shadeMode) {
		throw new NotImplementedException();
	}

	public bool InEditorMode() {
		return false; // todo...?
	}

	public void SetVertexShaderConstant(int var, Span<float> vec) {
		SetVertexShaderConstantInternal(var, vec);
	}

	private void SetVertexShaderConstantInternal(int var, Span<float> vec) {
		// I'm so tired of looking at this stuff
	}

	bool UsingTextureRenderTarget;

	public void SetViewports(ReadOnlySpan<ShaderViewport> viewports) {
		Assert(viewports.Length == 1);
		if (viewports.Length != 1)
			return;

		GfxViewport viewport = new();
		viewport.X = viewports[0].TopLeftX;
		viewport.Y = viewports[0].TopLeftY;
		viewport.Width = viewports[0].Width;
		viewport.Height = viewports[0].Height;
		viewport.MinZ = viewports[0].MinZ;
		viewport.MaxZ = viewports[0].MaxZ;

		if (UsingTextureRenderTarget) {
			int maxWidth = 0, maxHeight = 0;
			GetBackBufferDimensions(out maxWidth, out maxHeight);
		}
		// TODO: this has a lot more logic...
		FlushBufferedPrimitives();
		// HACK BECAUSE SOMETHING IS REALLY WRONG: We report the right viewport width/height to OpenGL, but regardless the first couple of frames it decides that we didn't. So this hack 
		// skips the first couple of loading screen frames. 
		if (frame >= 2) {
			glViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
			glDepthRangef(viewport.MinZ, viewport.MaxZ);
		}
		frame++;
	}
	int frame;

	public void GetViewports(Span<ShaderViewport> viewports) {
		throw new NotImplementedException();
	}

	public void PreInit(IShaderUtil shaderUtil, IServiceProvider services) {
		MeshMgr = (MeshMgr)services.GetRequiredService<IMeshMgr>()!;
		ShaderManager = services.GetRequiredService<IShaderSystem>();

		this.services = services;
		ShaderUtil = shaderUtil;
	}

	public ImageFormat GetBackBufferFormat() {
		// MaterialSystem is a prison of an architecture and I don't know how to reliably pass this information thru at the moment.
		// If other formats are used this will need to be changed. For now this will work fine.
		// It seems like IMaterialSystem::GetBackBufferFormat -> ShaderDevice::GetBackBufferFormat -> retrieve
		// PresentParameters.BackBufferFormat but what actually sets that I'm not sure yet
		return ImageFormat.RGBA8888;
	}
	public void GetBackBufferDimensions(out int width, out int height) {
		width = PresentParameters.DisplayMode.Width;
		height = PresentParameters.DisplayMode.Height;
	}

	ShaderDeviceInfo PresentParameters;
	bool ResetRenderStateNeeded = false;
	ulong CurrentFrame;
	nint TextureMemoryUsedLastFrame;

	public void BeginFrame() {
		if (ResetRenderStateNeeded) {
			ResetRenderState(false);
			ResetRenderStateNeeded = false;
		}

		++CurrentFrame;
		TextureMemoryUsedLastFrame = 0;
	}

	public void EndFrame() {
		ExportTextureList();
	}

	public bool IsDebugTextureListFresh(int numFramesAllowed = 1) {
		return DebugDataExportFrame <= CurrentFrame && (DebugDataExportFrame >= CurrentFrame - (nuint)numFramesAllowed);
	}

	public bool SetDebugTextureRendering(bool enable) {
		bool prev = DebugTexturesRendering;
		DebugTexturesRendering = enable;
		return prev;
	}

	public void EnableDebugTextureList(bool enable) {
		bEnableDebugTextureList = enable;
	}

	public void EnableGetAllTextures(bool enable) {
		DebugGetAllTextures = enable;
	}

	public KeyValues? GetDebugTextureList() {
		return DebugTextureList;
	}

	bool bEnableDebugTextureList;
	bool DebugGetAllTextures;
	bool DebugTexturesRendering;
	ulong DebugDataExportFrame;
	KeyValues? DebugTextureList;
	nuint TextureMemoryUsedTotal;
	nuint TextureMemoryUsedPicMip1;
	nuint TextureMemoryUsedPicMip2;
	private void ExportTextureList() {
		if (!bEnableDebugTextureList)
			return;

		DebugDataExportFrame = CurrentFrame;

		if (IsPC()) {
			if (DebugTextureList != null)
				DebugTextureList = null;

			DebugTextureList = new KeyValues("TextureList");

			TextureMemoryUsedTotal = 0;
			TextureMemoryUsedPicMip1 = 0;
			TextureMemoryUsedPicMip2 = 0;
			foreach (var texkvp in Textures) {
				var tex = texkvp.Value;
				TextureMemoryUsedTotal += tex.GetMemUsage();

				// Compute picmip memory usage
				{
					nuint numBytes = tex.GetMemUsage();

					if (tex.Levels > 1) {
						if (tex.GetWidth() > 4 || tex.GetHeight() > 4 || tex.GetDepth() > 4) {
							nuint topmipsize = (nuint)ImageLoader.GetMemRequired(tex.GetWidth(), tex.GetHeight(), tex.GetDepth(), tex.GetImageFormat(), false);
							numBytes -= topmipsize;

							TextureMemoryUsedPicMip1 += numBytes;

							if (tex.GetWidth() > 8 || tex.GetHeight() > 8 || tex.GetDepth() > 8) {
								nuint othermipsizeRatio = (nuint)(((tex.GetWidth() > 8) ? 2 : 1) * ((tex.GetHeight() > 8) ? 2 : 1) * ((tex.GetDepth() > 8) ? 2 : 1));
								nuint othermipsize = topmipsize / othermipsizeRatio;
								numBytes -= othermipsize;
							}

							TextureMemoryUsedPicMip1 += numBytes;
						}
						else {
							TextureMemoryUsedPicMip1 += numBytes;
							TextureMemoryUsedPicMip2 += numBytes;
						}
					}
					else {
						TextureMemoryUsedPicMip1 += numBytes;
						TextureMemoryUsedPicMip2 += numBytes;
					}
				}

				if (!DebugGetAllTextures && tex.LastBoundFrame != CurrentFrame)
					continue;

				if (tex.LastBoundFrame != CurrentFrame)
					tex.TimesBoundThisFrame = 0;

				KeyValues pSubKey = DebugTextureList.CreateNewKey();
				pSubKey.SetString("Name", tex.DebugName.String());
				pSubKey.SetString("TexGroup", tex.TextureGroupName.String());
				pSubKey.SetInt("Size", (int)tex.GetMemUsage());
				if (tex.GetCount() > 1)
					pSubKey.SetInt("Count", tex.GetCount());
				pSubKey.SetString("Format", ImageLoader.GetName(tex.GetImageFormat()));
				pSubKey.SetInt("Width", tex.GetWidth());
				pSubKey.SetInt("Height", tex.GetHeight());

				pSubKey.SetInt("BindsMax", tex.TimesBoundMax);
				pSubKey.SetInt("BindsFrame", tex.TimesBoundThisFrame);
			}
		}
	}

	public bool SetMode(IWindow window, in ShaderDeviceInfo info) {
		ShaderDeviceInfo actualInfo = info;
		if (!InitDevice(window, in actualInfo)) {
			return false;
		}

		if (!OnDeviceInit())
			return false;

		return true;
	}

	internal IServiceProvider services;
	internal IGraphicsContext? Device;
	internal GraphicsDriver Driver;

	public bool InitDevice(IWindow window, in ShaderDeviceInfo deviceInfo) {
		IGraphicsProvider graphics = services.GetRequiredService<IGraphicsProvider>();
		SetPresentParameters(in deviceInfo);
		Device = graphics.CreateContext(in deviceInfo, window);
		if (Device == null)
			return false;

		Driver = deviceInfo.Driver;
		Device!.MakeCurrent();
		unsafe {
			if (0 != (deviceInfo.Driver & GraphicsDriver.OpenGL))
				GL_LoadExtensions(graphics.GL_LoadExtensionsPtr());
		}

		return true;
	}

	internal unsafe void GL_LoadExtensions(delegate* unmanaged[Cdecl]<byte*, void*> loadExts) {
		Import((name) => {
			byte[] data = Encoding.UTF8.GetBytes(name);
			fixed (byte* ptr = data)
				return (nint)loadExts(ptr);
		});
	}

	public bool IsActive() => Device != null;
	public bool IsUsingGraphics() => IsActive();


	public void Present() {
		FlushBufferedPrimitives();
		bool validPresent = true;
		bool inMainThread = ThreadInMainThread();

		if (!inMainThread) {
			throw new Exception();
		}

		Device!.SwapBuffers();
	}

	bool IShaderDevice.IsDeactivated() => IsDeactivated();


	MaterialMatrixMode currentMode;
	public void MatrixMode(MaterialMatrixMode mode) {
		currentMode = mode;
	}

	public unsafe void LoadMatrix(in Matrix4x4 m4x4) {
		int szm4x4 = sizeof(Matrix4x4);
		int loc = (int)currentMode * szm4x4;
		Matrix4x4 transposed = Matrix4x4.Transpose(m4x4);
		glNamedBufferSubData(uboMatrices, loc, szm4x4, &transposed);
	}

	public void LoadIdentity() {
		LoadMatrix(Matrix4x4.Identity);
	}

	public int GetCurrentNumBones() {
		return 0;
	}

	Dictionary<uint, Dictionary<ulong, int>> locs = [];

	public unsafe int LocateShaderUniform(ReadOnlySpan<char> name) {
		if (!activeVertexShader.IsValid()) {
			Warning("WARNING: Attempted to locate uniform on an invalid vertex shader!\n");
			return -1;
		}

		if (!activePixelShader.IsValid()) {
			Warning("WARNING: Attempted to locate uniform on an invalid pixel shader!\n");
			return -1;
		}

		// If the name starts with $, go up one
		if (name.Length > 0 && name[0] == '$')
			name = name[1..];

		// Combobulate
		uint shader = CombobulateShadersIfChanged();

		// Then get shader ID -> shader uniform lookup table
		if (!locs.TryGetValue(shader, out var lookup))
			lookup = locs[shader] = [];

		// Then compute uniform name -> hash symbol
		// and look up if we've queried for this parameter yet
		ulong hash = name.Hash();
		if (lookup.TryGetValue(hash, out int loc))
			return loc;

		Span<byte> bytes = stackalloc byte[name.Length * 2];
		int byteLen = Encoding.ASCII.GetBytes(name, bytes);
		fixed (byte* uniformName = bytes)
			lookup[hash] = loc = glGetUniformLocation(shader, uniformName);

		return loc;
	}

	public nint GetCurrentProgram() => (nint)CombobulateShadersIfChanged();
	uint GetCurrentProgramInternal() => CombobulateShadersIfChanged();

	public void SetShaderUniform(int uniform, int integer) {
#if GL_DEBUG
		int i = glGetError();
#endif
		glProgramUniform1i(GetCurrentProgramInternal(), uniform, integer);
#if GL_DEBUG
		if ((i = glGetError()) != 0)
			AssertMsg(false, $"GL error {i}");
#endif
	}

	public void SetShaderUniform(int uniform, uint integer) {
		glProgramUniform1ui(GetCurrentProgramInternal(), uniform, integer);
	}

	public void SetShaderUniform(int uniform, float fl) {
		glProgramUniform1f(GetCurrentProgramInternal(), uniform, fl);
	}

	public void SetShaderUniform(int uniform, ReadOnlySpan<float> flConsts) {
		glProgramUniform1fv(GetCurrentProgramInternal(), uniform, flConsts);
	}

	readonly int[] LastBoundTextures = new int[(int)Sampler.MaxSamplers];
	int lastActiveTexture = -1;
	public void BindTexture(Sampler sampler, ShaderAPITextureHandle_t textureHandle) {
		CombobulateShadersIfChanged();
		if (textureHandle == INVALID_SHADERAPI_TEXTURE_HANDLE)
			return; // TODO: can we UNSET the sampler???

		if (LastBoundTextures[(int)sampler] != textureHandle) {
			int tex = GL_TEXTURE0 + (int)sampler;
			if (tex != lastActiveTexture) {
				glActiveTexture(tex);
				lastActiveTexture = tex;
			}
			glBindTexture(GL_TEXTURE_2D, (uint)textureHandle);
			LastBoundTextures[(int)sampler] = textureHandle;
		}
	}

	public bool CanDownloadTextures() {
		if (IsDeactivated())
			return false;

		return IsActive();
	}

	ShaderAPITextureHandle_t ModifyTextureHandle;

	public void ModifyTexture(ShaderAPITextureHandle_t textureHandle) {
		ModifyTextureHandle = textureHandle;
	}

	public struct TextureLoadInfo
	{
		public ShaderAPITextureHandle_t Handle;
		public int Width;
		public int Height;
		public int ZOffset;
		public int Level;
		public ImageFormat SrcFormat;
	}

	public void TexImageFromVTF(IVTFTexture? vtf, int vtfFrame) {
		Assert(vtf != null);
		Assert(ModifyTextureHandle != INVALID_SHADERAPI_TEXTURE_HANDLE);

		ref TextureLoadInfo info = ref (stackalloc TextureLoadInfo[1])[0];
		info.Handle = ModifyTextureHandle;
		info.Width = 0;
		info.Height = 0;
		info.ZOffset = 0;
		info.Level = 0;
		info.SrcFormat = vtf.Format();

		if (vtf.Depth() > 1) {
			throw new NotImplementedException("Multidepth textures not supported yet");
		}
		else if (vtf.IsCubeMap()) {
			throw new NotImplementedException("Cubemap textures not supported yet");
		}
		else {
			for (int i = 0; i < vtf.MipCount(); i++) {
				info.Level = i;
				LoadTextureFromVTF(in info, vtf, vtfFrame);
			}
		}
	}

	private unsafe void LoadTextureFromVTF(in TextureLoadInfo info, IVTFTexture vtf, int vtfFrame) {
		vtf.ImageFileInfo(vtfFrame, 0, info.Level, out int start, out int size);

		vtf.ComputeMipLevelDimensions(info.Level, out int w, out int h, out _);

		if (info.SrcFormat.IsCompressed()) {
			Span<byte> data = vtf.ImageData(vtfFrame, 0, info.Level);
			fixed (byte* bytes = data)
				glCompressedTextureSubImage2D((uint)info.Handle, info.Level, 0, 0, w, h, ImageLoader.GetGLImageInternalFormat(info.SrcFormat), data.Length, bytes);
			// Msg("err: " + glGetErrorName() + "\n");
		}
		else {
			Span<byte> data = vtf.ImageData(vtfFrame, 0, info.Level);
			TexSubImage2D(info.Level, 0, 0, 0, 0, vtf.Width(), vtf.Height(), info.SrcFormat, 0, data);
		}
	}



	public unsafe ShaderAPITextureHandle_t CreateTexture(
		int width,
		int height,
		int depth,
		ImageFormat imageFormat,
		ushort mipCount,
		int copies,
		CreateTextureFlags creationFlags,
		ReadOnlySpan<char> debugName,
		ReadOnlySpan<char> textureGroup) {
		ShaderAPITextureHandle_t handle = default;
		CreateTextures(new Span<int>(ref handle), 1, width, height, depth, imageFormat, mipCount, copies, creationFlags, debugName, textureGroup);
		return handle;
	}
	public unsafe void CreateTextures(
		Span<ShaderAPITextureHandle_t> textureHandles,
		int count,
		int width,
		int height,
		int depth,
		ImageFormat imageFormat,
		ushort mipCount,
		int copies,
		CreateTextureFlags creationFlags,
		ReadOnlySpan<char> debugName,
		ReadOnlySpan<char> textureGroup) {
		if (depth == 0)
			depth = 1;

		bool isCubeMap = (creationFlags & CreateTextureFlags.Cubemap) != 0;
		bool isRenderTarget = (creationFlags & CreateTextureFlags.RenderTarget) != 0;
		bool managed = (creationFlags & CreateTextureFlags.Managed) != 0;
		bool isDepthBuffer = (creationFlags & CreateTextureFlags.DepthBuffer) != 0;
		bool isDynamic = (creationFlags & CreateTextureFlags.Dynamic) != 0;
		bool isSRGB = (creationFlags & CreateTextureFlags.SRGB) != 0;

		CreateTextureHandles(textureHandles);
		for (int i = 0; i < count; i++) {
			ShaderAPITextureHandle_t handle = textureHandles[i];
			glObjectLabel(GL_TEXTURE, (uint)handle, $"ShaderAPI Texture '{debugName.SliceNullTerminatedString()}' [frame {i}]");

			ConvertDataToAcceptableGLFormat(imageFormat, null, out ImageFormat dstFormat, out _);
			glTextureStorage2D((uint)handle, mipCount, ImageLoader.GetGLImageInternalFormat(dstFormat), width, height);
			Textures[handle].Width = width;
			Textures[handle].Height = height;
			Textures[handle].Depth = depth;
			Textures[handle].Levels = mipCount;
			Textures[handle].Count = count;
			Textures[handle].Format = dstFormat;
			Textures[handle].DebugName = debugName;
			Textures[handle].TextureGroupName = textureGroup;

			ComputeStatsInfo(textureHandles[i], isCubeMap, depth > 1);
		}
	}

	InternalTextureInfo GetTexture(ShaderAPITextureHandle_t handle) => Textures[handle];

	private void ComputeStatsInfo(ShaderAPITextureHandle_t hTexture, bool isCubeMap, bool isVolumeTexture) {
		InternalTextureInfo textureData = GetTexture(hTexture);

		textureData.SizeBytes = 0;
		textureData.SizeTexels = 0;

		if (isCubeMap) {
			// todo: cubemap, info
		}
		else if (isVolumeTexture) {
			// todo
		}
		else {
			int numLevels = textureData.GetLevelCount();
			for (int i = 0; i < numLevels; ++i) {
				textureData.SizeBytes += (nuint)ImageLoader.GetMemRequired(textureData.Width >> i, textureData.Height >> i, 1, textureData.GetImageFormat(), false);
				textureData.SizeTexels += (textureData.Width >> i) * (textureData.Height >> i);
			}
		}
	}

	public ShaderAPITextureHandle_t CreateTextureHandle() {
		ShaderAPITextureHandle_t handle = 0;
		CreateTextureHandles(new Span<ShaderAPITextureHandle_t>(ref handle));
		return handle;
	}

	public class InternalTextureInfo
	{
		internal int Width;
		internal int Height;
		internal int Depth;
		internal int Levels;
		internal int Count;
		internal ImageFormat Format;
		internal UtlSymbol DebugName;
		internal UtlSymbol TextureGroupName;

		public nuint SizeBytes;
		public int SizeTexels;
		internal ulong LastBoundFrame;
		internal int TimesBoundMax;
		internal int TimesBoundThisFrame;

		internal nuint GetMemUsage() {
			return SizeBytes;
		}

		internal virtual int GetWidth() => Width;
		internal virtual int GetHeight() => Height;
		internal virtual int GetDepth() => Depth;
		internal virtual int GetLevelCount() => Levels;
		internal virtual int GetCount() => Count;
		internal virtual ImageFormat GetImageFormat() => Format;
	}

	readonly Dictionary<ShaderAPITextureHandle_t, InternalTextureInfo> Textures = [];

	public unsafe void CreateTextureHandles(Span<int> textureHandles) {
		int idxCreating = 0;
		fixed (ShaderAPITextureHandle_t* handles = textureHandles)
			glCreateTextures(GL_TEXTURE_2D, textureHandles.Length, (uint*)handles);
		for (int i = 0; i < textureHandles.Length; i++)
			Textures.Add(textureHandles[i], new());
	}

	public ShaderAPITextureHandle_t CreateDepthTexture(ImageFormat imageFormat, ushort width, ushort height, Span<char> debugName, bool texture) {
		ShaderAPITextureHandle_t handle = CreateTextureHandle();
		glObjectLabel(GL_TEXTURE, (uint)handle, $"ShaderAPI Depth Texture '{debugName}'");
		glTextureStorage2D((uint)handle, 1, GL_DEPTH24_STENCIL8, width, height);
		return handle;
	}

	public bool IsTexture(ShaderAPITextureHandle_t handle) {
		return true; // TODO
	}

	bool TextureIsAllocated(ShaderAPITextureHandle_t texture) => Textures.ContainsKey(texture);

	public unsafe void DeleteTexture(ShaderAPITextureHandle_t handle) {
		if (!TextureIsAllocated(handle))
			return;

		UnbindTexture(handle);
		Textures.Remove(handle);
		uint h = (uint)handle;
		glDeleteTextures(1, &h);
	}

	public void UnbindTexture(int handle) {
		// todo
	}

	public ImageFormat GetNearestSupportedFormat(ImageFormat fmt, bool filteringRequired = true) {
		return FindNearestSupportedFormat(fmt, false, false, filteringRequired);
	}

	public ImageFormat FindNearestSupportedFormat(ImageFormat format, bool isVertexTexture, bool isRenderTarget, bool filterableRequired) {
		return format;
	}

	public int GetCurrentDynamicVBSize() {
		return (1024 + 512) * 1024; // See if it's still needed to use smaller sizes at certain points... how would this even work, I wonder
	}

	public unsafe void TexSubImage2D(int mip, int face, int x, int y, int z, int width, int height, ImageFormat srcFormat, int srcStride, Span<byte> imageData) {
		glGetError();
		glPixelStorei(GL_UNPACK_ROW_LENGTH, srcStride / srcFormat.SizeInBytes());
		ConvertDataToAcceptableGLFormat(srcFormat, imageData, out srcFormat, out Span<byte> convertedData);
		fixed (byte* data = convertedData)
			glTextureSubImage2D((uint)ModifyTextureHandle, mip, x, y, width >> mip, height >> mip, ImageLoader.GetGLImageUploadFormat(srcFormat), GL_UNSIGNED_BYTE, data);
		glPixelStorei(GL_UNPACK_ROW_LENGTH, 0);
		var err = glGetError();
		System.Diagnostics.Debug.Assert(err == 0);
	}

	readonly ThreadLocal<byte[]> tempTransformBuffers = new ThreadLocal<byte[]>(() => new byte[1024 * 1024]);
	Span<byte> GetTempTransformBuffer(ImageFormat inFormat, ImageFormat outFormat, Span<byte> inData) {
		int desiredLength = ImageLoader.SizeInBytes(outFormat) * (inData.Length / ImageLoader.SizeInBytes(inFormat));

		if (desiredLength > tempTransformBuffers.Value!.Length)
			tempTransformBuffers.Value = new byte[MathLib.CeilPow2(desiredLength)];

		return tempTransformBuffers.Value!.AsSpan()[..desiredLength];
	}
	private void ConvertDataToAcceptableGLFormat(ImageFormat inFormat, Span<byte> inData, out ImageFormat outFormat, out Span<byte> outData) {
		switch (inFormat) {
			case ImageFormat.BGR888:
				outFormat = ImageFormat.RGB888;
				break;
			default:
				outFormat = inFormat;
				outData = inData;
				return;
		}

		switch (inFormat) {
			case ImageFormat.BGR888:
				outData = GetTempTransformBuffer(inFormat, outFormat, inData);
				for (int i = 0; i < inData.Length; i += 3) {
					outData[i + 2] = inData[i + 0];
					outData[i + 1] = inData[i + 1];
					outData[i + 0] = inData[i + 2];
				}
				return;
			default:
				outFormat = inFormat;
				outData = inData;
				return;
		}
	}

	public void ReacquireResources() {
		ReacquireResourcesInternal();
	}

	int releaseResourcesCount = 0;

	private void ReacquireResourcesInternal(bool resetState = false, bool forceReacquire = false, ReadOnlySpan<char> forceReason = default) {
		if (--releaseResourcesCount != 0) {
			Warning($"ReacquireResources has no effect, now at level {releaseResourcesCount}.\n");
			DevWarning("ReacquireResources being discarded is a bug: use IsDeactivated to check for a valid device.\n");
			Assert(false);
			if (releaseResourcesCount < 0)
				releaseResourcesCount = 0;
			return;
		}

		if (resetState) {
			ResetRenderState();
		}

		RestoreShaderObjects();
		MeshMgr.RestoreBuffers();
		ShaderUtil.RestoreShaderObjects(services);
	}

	private void RestoreShaderObjects() {

	}

	public void ReleaseResources() {
		releaseResourcesCount++;
	}

	public void SetShaderUniform(IMaterialVar textureVar) {
		int uniform = LocateShaderUniform(textureVar.GetName());
		if (uniform == -1)
			return;
		switch (textureVar.GetVarType()) {
			case MaterialVarType.Float: SetShaderUniform(uniform, textureVar.GetFloatValue()); break;
			case MaterialVarType.Int: SetShaderUniform(uniform, textureVar.GetIntValue()); break;
		}
	}

	ulong lastBoardUploadHash;
	public bool SetBoardState(in GraphicsBoardState state) {
		ulong currHash = state.Hash();
		if (currHash != lastBoardUploadHash) {
			glToggle(GL_BLEND, state.Blending);

			if (state.AlphaSeparateBlend) {
				glBlendFuncSeparate(state.SourceBlend.GLEnum(), state.DestinationBlend.GLEnum(), state.AlphaSourceBlend.GLEnum(), state.AlphaDestinationBlend.GLEnum());
				glBlendEquationSeparate(state.BlendOperation.GLEnum(), state.AlphaBlendOperation.GLEnum());
			}
			else {
				glBlendFunc(state.SourceBlend.GLEnum(), state.DestinationBlend.GLEnum());
				glBlendEquation(state.BlendOperation.GLEnum());
			}

			glColorMask(state.ColorWrite, state.ColorWrite, state.ColorWrite, state.AlphaWrite);

			glToggle(GL_DEPTH_TEST, state.DepthTest);
			glDepthMask(state.DepthWrite); // state.DepthWrite
			glDepthFunc(state.DepthFunc.GLEnum());
			// TEMPORARY
			glCullFace(GL_FRONT_AND_BACK);
#if DEBUG
			glDebugMessageInsert(GL_DEBUG_SOURCE_APPLICATION, GL_DEBUG_TYPE_MARKER, 0, GL_DEBUG_SEVERITY_LOW, "A board state write occured.");
#endif
			lastBoardUploadHash = currHash;
			return true;
		}
		return false;
	}

	public bool DoRenderTargetsNeedSeparateDepthBuffer() {
		return true;
	}

	public void EnableLinearColorSpaceFrameBuffer(bool v) {
		// I'm dealing with this later
	}

	public IShaderDevice GetShaderDevice() => this;

	public void SetRenderTargetEx(int renderTargetID, ShaderAPITextureHandle_t colorTextureHandle = -1, ShaderAPITextureHandle_t depthTextureHandle = -1) {
		FlushBufferedPrimitives();

		if (colorTextureHandle == -1 && depthTextureHandle == -1) {
			glBindFramebuffer(GL_FRAMEBUFFER, 0);
			return;
		}

		glBindFramebuffer(GL_FRAMEBUFFER, renderFBO);

		if (colorTextureHandle == -2)
			glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, 0, 0);
		else if (colorTextureHandle >= 0)
			glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, (uint)colorTextureHandle, 0);

		if (depthTextureHandle == -2)
			glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_TEXTURE_2D, 0, 0);
		else if (depthTextureHandle >= 0)
			glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_TEXTURE_2D, (uint)depthTextureHandle, 0);

		var status = glCheckFramebufferStatus(GL_FRAMEBUFFER);
		Assert(status == GL_FRAMEBUFFER_COMPLETE, "Framebuffer incomplete");
		glBindFramebuffer(GL_FRAMEBUFFER, 0);
	}

	public IMesh CreateStaticMesh(VertexFormat format, ReadOnlySpan<char> textureGroup, IMaterial? material) {
		return MeshMgr.CreateStaticMesh(format, textureGroup, material);
	}

	public int GetMaxVerticesToRender(IMaterial material) => MeshMgr.GetMaxVerticesToRender(material);
	public int GetMaxIndicesToRender(IMaterial material) => MeshMgr.GetMaxIndicesToRender(material);

	public void TexWrap(TexCoordComponent coord, TexWrapMode wrapMode) {
		int coordinate = coord switch {
			TexCoordComponent.S => GL_TEXTURE_WRAP_S,
			TexCoordComponent.T => GL_TEXTURE_WRAP_T,
			TexCoordComponent.U => GL_TEXTURE_WRAP_R,
			_ => -1
		};

		if (coordinate == -1) {
			Warning("ShaderAPIGl46.TexWrap: unknown coord\n");
			return;
		}

		switch (wrapMode) {
			case TexWrapMode.Clamp:
				glTextureParameteri((uint)ModifyTextureHandle, coordinate, GL_CLAMP_TO_EDGE);
				break;
			case TexWrapMode.Repeat:
				glTextureParameteri((uint)ModifyTextureHandle, coordinate, GL_REPEAT);
				break;
			case TexWrapMode.Border:
				glTextureParameteri((uint)ModifyTextureHandle, coordinate, GL_CLAMP_TO_BORDER);
				break;
			default:
				Warning("ShaderAPIGl46.TexWrap: unknown wrapMode\n");
				break;
		}
	}
	IMaterialSystemHardwareConfig HardwareConfig = Singleton<IMaterialSystemHardwareConfig>();
	public void TexMinFilter(TexFilterMode mode) {
		switch (mode) {
			case TexFilterMode.Nearest:
				glTextureParameteri((uint)ModifyTextureHandle, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
				break;
			case TexFilterMode.Linear:
				glTextureParameteri((uint)ModifyTextureHandle, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
				break;
			case TexFilterMode.NearestMipmapNearest:
				glTextureParameteri((uint)ModifyTextureHandle, GL_TEXTURE_MIN_FILTER, GL_NEAREST_MIPMAP_NEAREST);
				break;
			case TexFilterMode.LinearMipmapNearest:
				glTextureParameteri((uint)ModifyTextureHandle, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_NEAREST);
				break;
			case TexFilterMode.NearestMipmapLinear:
				glTextureParameteri((uint)ModifyTextureHandle, GL_TEXTURE_MIN_FILTER, GL_NEAREST_MIPMAP_LINEAR);
				break;
			case TexFilterMode.LinearMipmapLinear:
				glTextureParameteri((uint)ModifyTextureHandle, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
				break;
			case TexFilterMode.Anisotropic:
				glTextureParameterf((uint)ModifyTextureHandle, GL_TEXTURE_MAX_ANISOTROPY, HardwareConfig.MaximumAnisotropicLevel());
				glTextureParameteri((uint)ModifyTextureHandle, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
				break;
			default:
				break;
		}
	}

	public void TexMagFilter(TexFilterMode mode) {
		switch (mode) {
			case TexFilterMode.Nearest:
				glTextureParameteri((uint)ModifyTextureHandle, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
				break;
			case TexFilterMode.Linear:
				glTextureParameteri((uint)ModifyTextureHandle, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
				break;
			case TexFilterMode.NearestMipmapNearest:
				Warning("ShaderAPIGl46.TexMagFilter: TexFilterMode.NearestMipmapNearest is invalid\n");
				break;
			case TexFilterMode.LinearMipmapNearest:
				Warning("ShaderAPIGl46.TexMagFilter: TexFilterMode.LinearMipmapNearest is invalid\n");
				break;
			case TexFilterMode.NearestMipmapLinear:
				Warning("ShaderAPIGl46.TexMagFilter: TexFilterMode.NearestMipmapLinear is invalid\n");
				break;
			case TexFilterMode.LinearMipmapLinear:
				Warning("ShaderAPIGl46.TexMagFilter: TexFilterMode.LinearMipmapLinear is invalid\n");
				break;
			case TexFilterMode.Anisotropic:
				Warning("ShaderAPIGl46.TexMagFilter: TexFilterMode.Anisotropic is invalid\n");
				break;
			default:
				break;
		}
	}

	int MaxBoneLoaded;
	public unsafe void LoadBoneMatrix(int boneIndex, in Matrix3x4 matrix) {
		if (IsDeactivated())
			return;
		int szm4x4 = sizeof(Matrix4x4);
		int loc = (int)boneIndex * szm4x4;
		Matrix4x4 transposed = Matrix4x4.Transpose(matrix);
		glNamedBufferSubData(uboBones, loc, szm4x4, &transposed);
		if (boneIndex > MaxBoneLoaded)
			MaxBoneLoaded = boneIndex;

		if (boneIndex == 0) {
			MatrixMode(MaterialMatrixMode.Model);
			LoadMatrix(matrix);
		}
	}

	int numBones;
	public void SetNumBoneWeights(int numBones) {
		if (this.numBones != numBones) {
			FlushBufferedPrimitives();
			this.numBones = numBones;
		}
	}

	struct LockInfo
	{
		public bool Locked;
		public int Mip;
		public int CubeID;
		public int X;
		public int Y;
		public int W;
		public int H;
		public ImageFormat Format;
		public ShaderAPITextureHandle_t Handle;
	}
	LockInfo Lock;
	// This is a rushed implementation

	byte[] lockdata = new byte[2048 * 2048];
	Memory<byte> GetTempLockBuffer(ImageFormat format, int width, int height) {
		int desiredLength = ImageLoader.SizeInBytes(format) * (width) * (height);

		if (desiredLength > lockdata.Length)
			lockdata = new byte[MathLib.CeilPow2(desiredLength)];

		Array.Clear(lockdata);
		return lockdata.AsMemory()[..desiredLength];
	}

	public unsafe bool TexLock(int level, int cubeFaceID, int xOffset, int yOffset, int width, int height, ref PixelWriter writer) {
		if (!Textures.TryGetValue(ModifyTextureHandle, out InternalTextureInfo? info))
			return false;
		if (Lock.Locked)
			return false;

		Lock.Mip = level;
		Lock.CubeID = cubeFaceID;
		Lock.X = xOffset;
		Lock.Y = yOffset;
		Lock.W = width;
		Lock.H = height;
		Lock.Handle = ModifyTextureHandle;
		Lock.Format = info.Format;

		Memory<byte> buffer = GetTempLockBuffer(info.Format, width, height);
		fixed (byte* data = buffer.Span) {
			glGetTextureSubImage((uint)ModifyTextureHandle, 0, xOffset, yOffset, 0, width, height, 1, GL_RGBA, GL_UNSIGNED_BYTE, buffer.Span.Length, data);
		}
		writer.SetPixelMemory(info.Format, buffer.Span, width * ImageLoader.SizeInBytes(info.Format));
		return true;
	}

	public unsafe bool TexLock(int level, int cubeFaceID, int xOffset, int yOffset, int width, int height, ref PixelWriterMem writer) {
		if (!Textures.TryGetValue(ModifyTextureHandle, out InternalTextureInfo? info))
			return false;
		if (Lock.Locked)
			return false;

		Lock.Mip = level;
		Lock.CubeID = cubeFaceID;
		Lock.X = xOffset;
		Lock.Y = yOffset;
		Lock.W = width;
		Lock.H = height;
		Lock.Handle = ModifyTextureHandle;
		Lock.Format = info.Format;

		Memory<byte> buffer = GetTempLockBuffer(info.Format, width, height);
		fixed (byte* data = buffer.Span) {
			glGetTextureSubImage((uint)ModifyTextureHandle, 0, xOffset, yOffset, 0, width, height, 1, GL_RGBA, GL_UNSIGNED_BYTE, buffer.Span.Length, data);
		}
		writer.SetPixelMemory(info.Format, buffer, width * ImageLoader.SizeInBytes(info.Format));
		return true;
	}

	public unsafe void TexUnlock() {
		glPixelStorei(GL_UNPACK_ROW_LENGTH, 0);
		fixed (byte* data = lockdata)
			glTextureSubImage2D((uint)Lock.Handle, Lock.Mip, Lock.X, Lock.Y, Lock.W, Lock.H, ImageLoader.GetGLImageUploadFormat(Lock.Format), GL_UNSIGNED_BYTE, data);
		Lock = default;
	}

	public void BindStandardTexture(Sampler sampler, StandardTextureId id) {
		ShaderUtil.BindStandardTexture(sampler, id);
	}
}