using Source.Common.Formats.BSP;

namespace Source.Common;

public abstract class DispUtilsHelper
{
	public abstract PowerInfo GetPowerInfo();
	public abstract DispNeighbor GetEdgeNeighbor(int index);
	public abstract DispCornerNeighbors GetCornerNeighbors(int index);
	public abstract DispUtilsHelper GetDispUtilsByIndex(int index);

	public int GetPower() => GetPowerInfo().GetPower();
	public int GetSideLength() => GetPowerInfo().GetSideLength();
	public VertIndex GetCornerPointIndex(int iCorner) => GetPowerInfo().GetCornerPointIndex(iCorner);

	public int VertIndexToInt(in VertIndex i) {
		Assert(i.X >= 0 && i.X < GetSideLength() && i.Y >= 0 && i.Y < GetSideLength());
		return i.Y * GetSideLength() + i.X;
	}

	public VertIndex GetEdgeMidPoint(int edge) {
		short end = (short)(GetSideLength() - 1);
		short mid = (short)GetPowerInfo().GetMidPoint();

		if (edge == BSPFileCommon.NEIGHBOREDGE_LEFT)
			return new VertIndex(0, mid);
		else if (edge == BSPFileCommon.NEIGHBOREDGE_TOP)
			return new VertIndex(mid, end);
		else if (edge == BSPFileCommon.NEIGHBOREDGE_RIGHT)
			return new VertIndex(end, mid);
		else if (edge == BSPFileCommon.NEIGHBOREDGE_BOTTOM)
			return new VertIndex(mid, 0);

		Assert(false);
		return new VertIndex(0, 0);
	}
}
