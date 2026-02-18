namespace Source.Common;

public struct BaseHandle : IEquatable<BaseHandle>
{
	public bool Equals(BaseHandle otherHandle) => otherHandle != null && Index == otherHandle.Index;
	public uint Index;

	public void Invalidate() => Index = (uint)Constants.INVALID_EHANDLE_INDEX;
	public BaseHandle() => Invalidate();
	public BaseHandle(uint value) => Index = value;
	public BaseHandle(in BaseHandle handle) => Index = handle.Index;
	public BaseHandle(int entry, int serial) => Init(entry, serial);

	public void Init(in BaseHandle otherHandle) => Index = otherHandle.Index;
	public void Init(uint entindex) => Index = entindex;
	public void Init(ulong entindex) => Index = (uint)entindex;
	public void Init(int entry, int serial) => Index = (uint)(entry | (serial << Constants.NUM_ENT_ENTRY_BITS));

	public int GetEntryIndex() => (int)(Index & Constants.ENT_ENTRY_MASK);
	public int GetSerialNumber() => (int)(Index >> Constants.NUM_ENT_ENTRY_BITS);

	public static bool operator ==(BaseHandle a, BaseHandle b) => a.Index == b.Index;
	public static bool operator !=(BaseHandle a, BaseHandle b) => a.Index != b.Index;
	public static bool operator <(BaseHandle a, BaseHandle b) => a.Index < b.Index;
	public static bool operator >(BaseHandle a, BaseHandle b) => a.Index > b.Index;
	public override bool Equals(object? obj) {
		return obj switch {
			BaseHandle b => Index == b.Index,
			_ => false
		};
	}
	public override int GetHashCode() => (int)Index;
	public bool IsValid() => Index != Constants.INVALID_EHANDLE_INDEX;

	public BaseHandle Set(IHandleEntity? entity) {
		if (entity != null)
			this.Index = entity.GetRefEHandle().Index;
		else
			Index = Constants.INVALID_EHANDLE_INDEX;
		return this;
	}
}
