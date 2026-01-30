namespace Source.Common.Physics;

public interface IPhysicsObjectPairHash {
	void AddObjectPair(object object0, object obj1);
	void RemoveObjectPair(object obj0, object obj1);
	bool IsObjectPairInHash(object obj0, object obj1);
	void RemoveAllPairsForObject(object obj0);
	bool IsObjectInHash(object obj0);

	// Used to iterate over all pairs an object is part of
	int GetPairCountForObject(object obj0);
	int GetPairListForObject(object obj0, int maxCount, Span<object> objectList);
}
