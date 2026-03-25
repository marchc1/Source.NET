using Source.Common.Engine;
using Source.Common.Formats.BSP;

using System.Numerics;

namespace Source.Common;

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

public class CheckTransmitInfo
{
	public const int MAX_FAST_ENT_CLUSTERS = 4;
	public const int MAX_ENT_CLUSTERS = 64;
	public const int MAX_WORLD_AREAS = 8;

	public Edict? ClientEnt;

	public int PVSSize;
	public readonly byte[] PVS = new byte[PAD_NUMBER(BSPFileCommon.MAX_MAP_CLUSTERS, 8) / 8];

	public MaxEdictsBitVec TransmitEdict;
	public MaxEdictsBitVec TransmitAlways;

	public int AreasNetworked;
	public int[] Areas = new int[MAX_WORLD_AREAS];

	public int MapAreas;
	public byte[] AreaFloodNums = new byte[BSPFileCommon.MAX_MAP_AREAS];
}

public struct PVSInfo
{
	public short HeadNode;
	public short ClusterCount;
	public ushort[]? Clusters;
	public short AreaNum;
	public short AreaNum2;
	public Vector3 Center;
}
