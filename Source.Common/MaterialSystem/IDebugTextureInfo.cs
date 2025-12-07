using Source.Common.Formats.Keyvalues;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common.MaterialSystem;

public enum TextureMemoryType
{
	MEMORY_RESERVED_MIN = 0,
	MEMORY_BOUND_LAST_FRAME,        // sums up textures bound last frame
	MEMORY_TOTAL_LOADED,            // total texture memory used
	MEMORY_ESTIMATE_PICMIP_1,       // estimate of running with "picmip 1"
	MEMORY_ESTIMATE_PICMIP_2,       // estimate of running with "picmip 2"
	MEMORY_RESERVED_MAX
}
public interface IDebugTextureInfo
{
	// Use this to turn on the mode where it builds the debug texture list.
	// At the end of the next frame, GetDebugTextureList() will return a valid list of the textures.
	void EnableDebugTextureList(bool enable);
	
	// If this is on, then it will return all textures that exist, not just the ones that were bound in the last frame.
	// It is required to enable debug texture list to get this.
	void EnableGetAllTextures(bool enable);

	// Use this to get the results of the texture list.
	// Do NOT release the KeyValues after using them.
	// There will be a bunch of subkeys, each with these values:
	//    Name   - the texture's filename
	//    Binds  - how many times the texture was bound
	//    Format - ImageFormat of the texture
	//    Width  - Width of the texture
	//    Height - Height of the texture
	// It is required to enable debug texture list to get this.
	KeyValues? GetDebugTextureList();

	// Texture memory usage


	// This returns how much memory was used.
	// TODO: int GetTextureMemoryUsed(TextureMemoryType textureMemory);

	// Use this to determine if texture debug info was computed within last numFramesAllowed frames.
	bool IsDebugTextureListFresh(int numFramesAllowed = 1);

	// Enable debug texture rendering when texture binds should not count towards textures
	// used during a frame. Returns the old state of debug texture rendering flag to use
	// it for restoring the mode.
	bool SetDebugTextureRendering(bool enable);
}
