using Source.Common.Bitmap;
using Source.Common.Launcher;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

namespace Source.Common.ShaderAPI;

public enum CreateTextureFlags
{
	Cubemap = 0x0001,
	RenderTarget = 0x0002,
	Managed = 0x0004,
	DepthBuffer = 0x0008,
	Dynamic = 0x0010,
	AutoMipmap = 0x0020,
	VertexTexture = 0x0040,
	SysMem = 0x0200,
	UnfilterableOK = 0x1000,
	SRGB = 0x4000,
}

/// <summary>
/// A basic representation of the graphics state machine
/// </summary>
public struct GraphicsBoardState
{
	public bool Blending;
	public ShaderBlendFactor SourceBlend;
	public ShaderBlendFactor DestinationBlend;
	public ShaderBlendOp BlendOperation;

	public bool AlphaSeparateBlend;
	public ShaderBlendFactor AlphaSourceBlend;
	public ShaderBlendFactor AlphaDestinationBlend;
	public ShaderBlendOp AlphaBlendOperation;

	public bool DepthTest;
	public bool ColorWrite;
	public bool AlphaWrite;
	public bool DepthWrite;
	public ShaderDepthFunc DepthFunc;
}

public interface IMeshMgr
{

}

public interface IShaderAPI : IShaderDynamicAPI
{
	void SetViewports(ReadOnlySpan<ShaderViewport> viewports);
	void GetViewports(Span<ShaderViewport> viewports);

	void PreInit(IShaderUtil shaderUtil, IServiceProvider services);
	void DrawMesh(IMesh mesh);
	void Bind(IMaterial? material);
	void FlushBufferedPrimitives();
	bool OnDeviceInit();
	void InitRenderState();
	void ClearBuffers(bool bClearColor, bool bClearDepth, bool bClearStencil, int renderTargetWidth, int renderTargetHeight);
	void ClearColor3ub(byte r, byte g, byte b);
	void ClearColor4ub(byte r, byte g, byte b, byte a);
	void GetBackBufferDimensions(out int width, out int height);
	ImageFormat GetBackBufferFormat();
	void BeginFrame();
	void EndFrame();
	int GetCurrentDynamicVBSize();
	void TexSubImage2D(int mip, int face, int x, int y, int z, int width, int height, ImageFormat srcFormat, int srcStride, Span<byte> imageData);
	bool DoRenderTargetsNeedSeparateDepthBuffer();
	void EnableLinearColorSpaceFrameBuffer(bool v);
	void SetRenderTargetEx(int rt, ShaderAPITextureHandle_t colorTextureHandle = (ShaderAPITextureHandle_t)ShaderRenderTarget.Backbuffer, ShaderAPITextureHandle_t depthTextureHandle = (ShaderAPITextureHandle_t)ShaderRenderTarget.Depthbuffer);
	void InvalidateDelayedShaderConstraints();
	void SetSkinningMatrices();
	void ShadeMode(ShadeMode flat);
	void RenderPass();
	bool SetMode(IWindow window, in ShaderDeviceInfo info);
	int GetMaxVerticesToRender(IMaterial material);
	int GetMaxIndicesToRender(IMaterial material);
	bool IsActive();
	bool SetBoardState(in GraphicsBoardState state);
	bool CanDownloadTextures();
	void BindTexture(Sampler sampler, ShaderAPITextureHandle_t textureHandle);
	void TexImageFromVTF(IVTFTexture? vtfTexture, int i);
	void ModifyTexture(int v);
	ShaderAPITextureHandle_t CreateTexture(
		int width,
		int height,
		int depth,
		ImageFormat imageFormat,
		ushort mipCount,
		int copies,
		CreateTextureFlags creationFlags,
		ReadOnlySpan<char> debugName,
		ReadOnlySpan<char> textureGroup);
	void CreateTextures(Span<ShaderAPITextureHandle_t> textureHandles,
		int count,
		int width,
		int height,
		int depth,
		ImageFormat imageFormat,
		ushort mipCount,
		int copies,
		CreateTextureFlags creationFlags,
		ReadOnlySpan<char> debugName,
		ReadOnlySpan<char> textureGroup);
	ShaderAPITextureHandle_t CreateDepthTexture(ImageFormat imageFormat, ushort width, ushort height, Span<char> debugName, bool texture);
	bool IsTexture(ShaderAPITextureHandle_t handle);
	public void DeleteTexture(ShaderAPITextureHandle_t handle);
	ImageFormat GetNearestSupportedFormat(ImageFormat fmt, bool filteringRequired = true);
	IShaderShadow NewShaderShadow(ReadOnlySpan<char> materialName);
	IShaderDevice GetShaderDevice();
	void TexWrap(TexCoordComponent coord, TexWrapMode wrapMode);
	void TexMinFilter(TexFilterMode mode);
	void TexMagFilter(TexFilterMode mode);
	void LoadBoneMatrix(int boneIndex, in Matrix3x4 matrix);
	void SetNumBoneWeights(int numBones);
	bool TexLock(int level, int cubeFaceID, int xOffset, int yOffset, int width, int height, ref PixelWriter writer);
	bool TexLock(int level, int cubeFaceID, int xOffset, int yOffset, int width, int height, ref PixelWriterMem writer);
	void TexUnlock();
	bool ChangeVideoMode(in ShaderDeviceInfo info);
	void AddModeChangeCallBack(Action func);
}
