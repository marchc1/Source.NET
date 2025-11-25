using System.Numerics;

namespace Source.Common;

public interface IServerNetworkable
{
	IHandleEntity? GetEntityHandle();
	ServerClass GetServerClass();
	ReadOnlySpan<char> GetClassName();
	void Release();
	int AreaNum();
}

public struct PVSInfo {
	public short HeadNode;
	public short ClusterCount;
	public ushort[]? Clusters;
	public short AreaNum;
	public short AreaNum2;
	public Vector3 Center;
}
