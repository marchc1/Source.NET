namespace Source.Common.Entity;

public class BaseHandle : IEquatable<BaseHandle>, IComparable<BaseHandle>
{
	public const int NUM_SERIAL_NUM_BITS = 16;
	public const int ENT_ENTRY_MASK = Constants.NUM_ENT_ENTRIES - 1;
	public const uint INVALID_EHANDLE_INDEX = 0xFFFFFFFF;

	public uint Index;

	public BaseHandle()
	{
		Index = INVALID_EHANDLE_INDEX;
	}

	public BaseHandle(BaseHandle other)
	{
		Index = other.Index;
	}

	public BaseHandle(uint value)
	{
		Index = value;
	}

	public BaseHandle(int entry, int serialNumber)
	{
		Init(entry, serialNumber);
	}

	public void Init(int entry, int serialNumber)
	{
		if (entry < 0 || entry >= Constants.NUM_ENT_ENTRIES)
			throw new ArgumentOutOfRangeException(nameof(entry));
		if (serialNumber < 0 || serialNumber >= (1 << NUM_SERIAL_NUM_BITS))
			throw new ArgumentOutOfRangeException(nameof(serialNumber));

		Index = (uint)(entry | (serialNumber << Constants.NUM_ENT_ENTRY_BITS));
	}

	public void Term()
	{
		Index = INVALID_EHANDLE_INDEX;
	}

	public bool IsValid() => Index != INVALID_EHANDLE_INDEX;

	public int GetEntryIndex() => (int)(Index & ENT_ENTRY_MASK);

	public int GetSerialNumber() => (int)(Index >> Constants.NUM_ENT_ENTRY_BITS);

	public int ToInt() => (int)Index;

	public bool Equals(BaseHandle? other)
	{
		return other is not null && Index == other.Index;
	}

	public override bool Equals(object? obj)
	{
		return obj is BaseHandle other && Equals(other);
	}

	public override int GetHashCode()
	{
		return Index.GetHashCode();
	}

	public int CompareTo(BaseHandle? other)
	{
		return other is null ? 1 : Index.CompareTo(other.Index);
	}

	// Operators
	public static bool operator ==(BaseHandle? a, BaseHandle? b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a is null || b is null) return false;
		return a.Index == b.Index;
	}

	public static bool operator !=(BaseHandle? a, BaseHandle? b) => !(a == b);

	public static bool operator <(BaseHandle a, BaseHandle b) => a.Index < b.Index;

	public static bool operator >(BaseHandle a, BaseHandle b) => a.Index > b.Index;

	public static bool operator ==(BaseHandle a, IHandleEntity? b) => a.Get() == b;

	public static bool operator !=(BaseHandle a, IHandleEntity? b) => !(a == b);

	public static bool operator <(BaseHandle a, IHandleEntity? b)
	{
		uint otherIndex = b != null ? b.GetRefEHandle().Index : INVALID_EHANDLE_INDEX;
		return a.Index < otherIndex;
	}

	public static bool operator >(BaseHandle a, IHandleEntity? b)
	{
		uint otherIndex = b != null ? b.GetRefEHandle().Index : INVALID_EHANDLE_INDEX;
		return a.Index > otherIndex;
	}

	public BaseHandle Set(IHandleEntity? entity)
	{
		Index = entity?.GetRefEHandle().Index ?? INVALID_EHANDLE_INDEX;
		return this;
	}

	public IHandleEntity? Get()
	{
		return null;
	}
}