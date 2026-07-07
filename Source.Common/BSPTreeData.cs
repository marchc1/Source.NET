using System.Numerics;

namespace Source.Common;

public interface ISpatialLeafEnumerator
{
	// call back with a leaf and a context
	// The context is completely user defined; it's passed into the enumeration
	// function of ISpatialQuery.
	// This gets called	by the enumeration methods with each leaf
	// that passes the test; return true to continue enumerating,
	// false to stop

	bool EnumerateLeaf(int leaf, nint context);
}

public interface ISpatialQuery
{
	// Returns the number of leaves
	int LeafCount();

	// Enumerates the leaves along a ray, box, etc.
	bool EnumerateLeavesAtPoint<T>(in Vector3 pt, ref T enumerator, nint context) where T : ISpatialLeafEnumerator;
	bool EnumerateLeavesInBox<T>(in Vector3 mins, in Vector3 maxs, ref T enumerator, nint context) where T : ISpatialLeafEnumerator;
	bool EnumerateLeavesInSphere<T>(in Vector3 center, float radius, ref T enumerator, nint context) where T : ISpatialLeafEnumerator;
	bool EnumerateLeavesAlongRay<T>(in Ray ray, ref T enumerator, nint context) where T : ISpatialLeafEnumerator;
}

public interface IBSPTreeDataEnumerator
{
	// call back with a userId and a context
	bool EnumerateElement(int userId, nint context);
}

public interface IBSPTreeData
{
	// Initializes, shuts down
	void Init(ISpatialQuery? bspTree);
	void Shutdown();

	// Adds and removes data from the leaf lists
	BSPTreeDataHandle_t Insert(int userId, in Vector3 mins, in Vector3 maxs);
	void Remove(BSPTreeDataHandle_t handle);

	// Call this when a element moves
	void ElementMoved(BSPTreeDataHandle_t handle, in Vector3 mins, in Vector3 maxs);

	// Enumerate elements in a particular leaf
	bool EnumerateElementsInLeaf<T>(int leaf, ref T enumerator, nint context)where T : IBSPTreeDataEnumerator;

	// Is the element in any leaves at all?
	bool IsElementInTree(BSPTreeDataHandle_t handle);

	// NOTE: These methods call through to the functions in the attached
	// ISpatialQuery
	// For convenience, enumerates the leaves along a ray, box, etc.
	bool EnumerateLeavesAtPoint<T>(in Vector3 pt, ref T enumerator, nint context) where T : ISpatialLeafEnumerator;
	bool EnumerateLeavesInBox<T>(in Vector3 mins, in Vector3 maxs, ref T enumerator, nint context) where T : ISpatialLeafEnumerator;
	bool EnumerateLeavesInSphere<T>(in Vector3 center, float radius, ref T enumerator, nint context) where T : ISpatialLeafEnumerator;
	bool EnumerateLeavesAlongRay<T>(in Ray ray, ref T enumerator, nint context) where T : ISpatialLeafEnumerator;
}
