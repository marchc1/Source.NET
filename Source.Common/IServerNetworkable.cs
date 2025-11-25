namespace Source.Common;

public interface IServerNetworkable
{
	IHandleEntity? GetEntityHandle();
	ServerClass GetServerClass();
	ReadOnlySpan<char> GetClassName();
	void Release();
	int AreaNum();
}
