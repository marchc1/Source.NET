using Source.Common;
using Source.Common.Engine;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Engine;

public interface IStaticPropMgrEngine
{
	bool Init();
	void Shutdown();

	// Call at the beginning of the level, will unserialize all static
	// props and add them to the main collision tree
	void LevelInit();

	// Call this when there's a client, *after* LevelInit, and after the world entity is loaded
	void LevelInitClient();

	// Call this when there's a client, *before* LevelShutdown
	void LevelShutdownClient();

	// Call at the end of the level, cleans up the static props
	void LevelShutdown();

	// Call this to recompute static prop lighting when necessary
	void RecomputeStaticLighting();

	// Check if a static prop is in a particular PVS.
	bool IsPropInPVS(IHandleEntity? handleEntity, ReadOnlySpan<byte> vis);

	// returns a collideable interface to static props
	ICollideable? GetStaticProp(IHandleEntity? handleEntity);

	// returns the lightcache handle associated with a static prop
	LightCacheHandle_t GetLightCacheHandleForStaticProp(IHandleEntity? handleEntity);

	// Is a base handle a static prop?
	bool IsStaticProp(IHandleEntity? handleEntity);
	bool IsStaticProp(BaseHandle handle);

	// Returns the static prop index (useful for networking)
	int GetStaticPropIndex(IHandleEntity? handleEntity);

	bool PropHasBakedLightingDisabled(IHandleEntity? handleEntity);
}
