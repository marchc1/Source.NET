using Source.Common;

using System.Numerics;

namespace Source.Engine;

public interface ISpatialPartitionInternal : ISpatialPartition{
	void Init(in Vector3 worldmin, in Vector3 worldmax);
	void DrawDebugOverlays();
}
