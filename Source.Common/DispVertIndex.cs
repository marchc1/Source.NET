namespace Source.Common;

public struct VertIndex
{
	public short X, Y;

	public VertIndex() { }
	public VertIndex(short ix, short iy) {
		X = ix;
		Y = iy;
	}

	public void Init(short ix, short iy) {
		X = ix;
		Y = iy;
	}

	public short this[int i] {
		readonly get {
			Assert(i >= 0 && i <= 1);
			return i == 0 ? X : Y;
		}
		set {
			Assert(i >= 0 && i <= 1);
			if (i == 0) X = value;
			else Y = value;
		}
	}

	public static VertIndex operator +(VertIndex lhs, VertIndex other) => new((short)(lhs.X + other.X), (short)(lhs.Y + other.Y));
	public static VertIndex operator -(VertIndex lhs, VertIndex other) => new((short)(lhs.X - other.X), (short)(lhs.Y - other.Y));
	public static VertIndex operator <<(VertIndex lhs, int shift) => new((short)(lhs.X << shift), (short)(lhs.Y << shift));
	public static VertIndex operator >>(VertIndex lhs, int shift) => new((short)(lhs.X >> shift), (short)(lhs.Y >> shift));

	public static bool operator ==(VertIndex lhs, VertIndex other) => lhs.X == other.X && lhs.Y == other.Y;
	public static bool operator !=(VertIndex lhs, VertIndex other) => lhs.X != other.X || lhs.Y != other.Y;

	public override readonly bool Equals(object? obj) => obj is VertIndex other && this == other;
	public override readonly int GetHashCode() => HashCode.Combine(X, Y);

	public static VertIndex BuildOffsetVertIndex(in VertIndex nodeIndex, in VertIndex offset, int mul) {
		int x = nodeIndex.X + offset.X * mul;
		int y = nodeIndex.Y + offset.Y * mul;

		Assert(x >= short.MinValue && x <= short.MaxValue);
		Assert(y >= short.MinValue && y <= short.MaxValue);

		return new VertIndex((short)x, (short)y);
	}
}
