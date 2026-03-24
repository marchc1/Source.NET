using System.IO.Pipelines;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Common;


public enum PartitionListMask
{
	EngineSolidEdicts = (1 << 0),       // every edict_t that isn't SOLID_TRIGGER or SOLID_NOT (and static props)
	EngineTriggerEdicts = (1 << 1),     // every edict_t that IS SOLID_TRIGGER
	ClientSolidEdicts = (1 << 2),
	ClientResponsiveEdicts = (1 << 3),  // these are client-side only objects that respond to being forces, etc.
	EngineNonStaticEdicts = (1 << 4),   // everything in solid & trigger except the static props, includes SOLID_NOTs
	ClientStaticProps = (1 << 5),
	EngineStaticProps = (1 << 6),
	ClientNonStaticEdicts = (1 << 7),   // everything except the static props

	AllClientEdicts = ClientNonStaticEdicts | ClientStaticProps | ClientResponsiveEdicts | ClientSolidEdicts,
	ClientGameEdicts = AllClientEdicts & ~ClientStaticProps,
	ServerGameEdicts = EngineSolidEdicts | EngineTriggerEdicts | EngineNonStaticEdicts
}

public static class PartitionConstants
{
	public const SpatialPartitionHandle_t PARTITION_INVALID_HANDLE = unchecked((SpatialPartitionHandle_t)~0);
}

public enum IterationRetval
{
	Continue,
	Stop
}

public interface IPartitionEnumerator
{
	IterationRetval EnumElement(IHandleEntity? handleEntity);
}

public interface IPartitionQueryCallback
{
	void OnPreQuery_V1();
	void OnPreQuery(SpatialPartitionListMask_t listMask);
	void OnPostQuery(SpatialPartitionListMask_t listMask);
}

public interface ISpatialPartition
{
	SpatialPartitionHandle_t CreateHandle(IHandleEntity? handleEntity);

	// A fast method of creating a handle + inserting into the tree in the right place
	SpatialPartitionHandle_t CreateHandle(IHandleEntity handleEntity, SpatialPartitionListMask_t listMask, in Vector3 mins, in Vector3 maxs);

	void DestroyHandle(SpatialPartitionHandle_t handle);

	// Adds, removes an handle from a particular spatial partition list
	// There can be multiple partition lists; each has a unique id
	void Insert(SpatialPartitionListMask_t listMask, SpatialPartitionHandle_t handle);
	void Remove(SpatialPartitionListMask_t listMask, SpatialPartitionHandle_t handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Insert(PartitionListMask listMask, SpatialPartitionHandle_t handle) => Insert((SpatialPartitionListMask_t)listMask, handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Remove(PartitionListMask listMask, SpatialPartitionHandle_t handle) => Remove((SpatialPartitionListMask_t)listMask, handle);

	// Same as calling Remove() then Insert(). For performance-sensitive areas where you want to save a call.
	void RemoveAndInsert(SpatialPartitionListMask_t removeMask, SpatialPartitionListMask_t insertMask, SpatialPartitionHandle_t handle);

	// This will remove a particular handle from all lists
	void Remove(SpatialPartitionHandle_t handle);

	// Call this when an entity moves...
	void ElementMoved(SpatialPartitionHandle_t handle, in Vector3 mins, in Vector3 maxs);

	// A fast method to insert + remove a handle from the tree...
	// This is used to suppress collision of a single model..
	SpatialTempHandle_t HideElement(SpatialPartitionHandle_t handle);
	void UnhideElement(SpatialPartitionHandle_t handle, SpatialTempHandle_t tempHandle);

	// Installs callbacks to get called right before a query occurs
	void InstallQueryCallback_V1(IPartitionQueryCallback? callback);
	void RemoveQueryCallback(IPartitionQueryCallback? callback);

	// Gets all entities in a particular volume...
	// if coarseTest == true, it'll return all elements that are in
	// spatial partitions that intersect the box
	// if coarseTest == false, it'll return only elements that truly intersect
	void EnumerateElementsInBox<IPE>(SpatialPartitionListMask_t listMask, in Vector3 mins, in Vector3 maxs, bool coarseTest, scoped ref IPE iterator)
		where IPE : struct, IPartitionEnumerator, allows ref struct;
	void EnumerateElementsInSphere<IPE>(SpatialPartitionListMask_t listMask, in Vector3 origin, float radius, bool coarseTest, scoped ref IPE iterator)
		where IPE : struct, IPartitionEnumerator, allows ref struct;
	void EnumerateElementsAlongRay<IPE>(SpatialPartitionListMask_t listMask, in Ray ray, bool coarseTest, scoped ref IPE iterator)
		where IPE : struct, IPartitionEnumerator, allows ref struct;
	void EnumerateElementsAtPoint<IPE>(SpatialPartitionListMask_t listMask, in Vector3 pt, bool coarseTest, scoped ref IPE iterator)
		where IPE : struct, IPartitionEnumerator, allows ref struct;

	// For debugging.... suppress queries on particular lists
	void SuppressLists(SpatialPartitionListMask_t nListMask, bool suppress);
	SpatialPartitionListMask_t GetSuppressedLists();

	void RenderAllObjectsInTree(TimeUnit_t time);
	void RenderObjectsInPlayerLeafs(in Vector3 playerMin, in Vector3 layerMax, TimeUnit_t time);
	void RenderLeafsForRayTraceStart(TimeUnit_t time);
	void RenderLeafsForRayTraceEnd();
	void RenderLeafsForHullTraceStart(TimeUnit_t time);
	void RenderLeafsForHullTraceEnd();
	void RenderLeafsForBoxStart(TimeUnit_t time);
	void RenderLeafsForBoxEnd();
	void RenderLeafsForSphereStart(TimeUnit_t time);
	void RenderLeafsForSphereEnd();

	void RenderObjectsInBox(in Vector3 min, in Vector3 max, TimeUnit_t time);
	void RenderObjectsInSphere(in Vector3 center, float radius, TimeUnit_t time);
	void RenderObjectsAlongRay(in Ray ray, TimeUnit_t time);

	void ReportStats(ReadOnlySpan<char> fileName);

	void InstallQueryCallback(IPartitionQueryCallback? callback);
}
