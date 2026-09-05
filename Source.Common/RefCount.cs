namespace Source.Common;

public interface IRefCounted
{
	int AddRef();
	int Release();
}
