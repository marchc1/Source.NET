namespace Source.Common;

public interface IBaseHandle
{
	uint Unpack();

	bool Equals(BaseHandle otherHandle);
	bool Equals(object? obj);
	int GetEntryIndex();
	int GetHashCode();
	int GetSerialNumber();
	void Init(in BaseHandle otherHandle);
	void Init(int entry, int serial);
	void Init(uint entindex);
	void Init(ulong entindex);
	void Invalidate();
	bool IsValid();
}
