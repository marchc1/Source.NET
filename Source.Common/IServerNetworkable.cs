using Source.Common.Engine;
using Source.Common.Formats.BSP;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Common;

public class CheckTransmitInfo {
	public const int MAX_FAST_ENT_CLUSTERS = 4;
	public const int MAX_ENT_CLUSTERS	= 64;
	public const int MAX_WORLD_AREAS		= 8;

	[InlineArray(MAX_FAST_ENT_CLUSTERS)] public struct InlineArrayMaxFastEntClusters<T> { T? first; }
	[InlineArray((((BSPFileCommon.MAX_MAP_CLUSTERS + (8 - 1)) / 8) * 8) / 8)] public struct InlineArrayMaxMapClustersPadded<T> { T? first; }
	[InlineArray(MAX_ENT_CLUSTERS)] public struct InlineArrayMaxEntClusters<T> { T? first; }
	[InlineArray(MAX_WORLD_AREAS)] public struct InlineArrayMaxWorldAreas<T> { T? first; }
	[InlineArray(BSPFileCommon.MAX_MAP_AREAS)] public struct InlineArrayMaxMapAreas<T> { T? first; }

	public Edict? ClientEnt;

	public InlineArrayMaxMapClustersPadded<byte> PVS;
	public int PVSSize;

	public MaxEdictsBitVec TransmitEdict;  // THESE ARE POINTERS IN C++: FIGURE THIS OUT!!!
	public MaxEdictsBitVec TransmitAlways; // THESE ARE POINTERS IN C++: FIGURE THIS OUT!!!

	public int AreasNetworked;
	public InlineArrayMaxWorldAreas<int> Areas;

	public InlineArrayMaxMapAreas<byte> AreaFloodNums;
	public int MapAreas;
}


public struct PVSInfo
{
	public short HeadNode;

	public short ClusterCount;

	public ushort[]? Clusters;

	public short AreaNum;
	public short AreaNum2;

	public Vector3 Center;

	private CheckTransmitInfo.InlineArrayMaxFastEntClusters<ushort> ClustersInline;
}

public interface IServerNetworkable
{
	IHandleEntity? GetEntityHandle();
	ServerClass GetServerClass();
	Edict? GetEdict();
	ReadOnlySpan<char> GetClassName();
	void Release();
	int AreaNum();
	object? GetBaseEntity();
}
