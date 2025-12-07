using Source.Common.Formats.Keyvalues;

namespace Source.Common.MaterialSystem;

public enum TextureMemoryType
{
	MemoryReservedMin = 0,
	MemoryBoundLastFrame,        // sums up textures bound last frame
	MemoryTotalLoaded,           // total texture memory used
	MemoryEstimatePicmip1,      // estimate of running with "picmip 1"
	MemoryEstimatePicmip2,      // estimate of running with "picmip 2"
	MemoryReservedMax
}

public interface IDebugTextureInfo
{
	void EnableDebugTextureList(bool bEnable);
	void EnableGetAllTextures(bool bEnable);
	KeyValues? GetDebugTextureList();
	int GetTextureMemoryUsed(TextureMemoryType eTextureMemory);
	bool IsDebugTextureListFresh(int numFramesAllowed = 1);
	bool SetDebugTextureRendering(bool bEnable);
}