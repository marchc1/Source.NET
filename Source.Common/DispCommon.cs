using Source.Common.Formats.BSP;

namespace Source.Common;

public abstract class DispUtilsHelper
{
	public static readonly int[] g_EdgeDims =
	[
		0,
		1,
		0,
		1
	];

	public static readonly int[] g_EdgeSideLenMul =
	[
		0,
		1,
		1,
		0
	];

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

	public static short GetEdgeIndexFromPoint(in VertIndex index, int maxPower) {
		int sideLengthMinus1 = 1 << maxPower;

		if (index.X == 0)
			return BSPFileCommon.NEIGHBOREDGE_LEFT;
		else if (index.Y == sideLengthMinus1)
			return BSPFileCommon.NEIGHBOREDGE_TOP;
		else if (index.X == sideLengthMinus1)
			return BSPFileCommon.NEIGHBOREDGE_RIGHT;
		else if (index.Y == 0)
			return BSPFileCommon.NEIGHBOREDGE_BOTTOM;
		else
			return -1;
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

	static void RotateVertIncrement(NeighborOrientation neighbor, in VertIndex inv, ref VertIndex outv) {
		if (neighbor == NeighborOrientation.ORIENTATION_CCW_0) {
			outv = inv;
		}
		else if (neighbor == NeighborOrientation.ORIENTATION_CCW_90) {
			outv.X = inv.Y;
			outv.Y = (short)-inv.X;
		}
		else if (neighbor == NeighborOrientation.ORIENTATION_CCW_180) {
			outv.X = (short)-inv.X;
			outv.Y = (short)-inv.Y;
		}
		else {
			outv.X = (short)-inv.Y;
			outv.Y = inv.X;
		}
	}

	public static void SetupSpan(int power, int edge, NeighborSpan span, ref VertIndex start, ref VertIndex end) {
		int freeDim = g_EdgeDims[edge] ^ 1;
		PowerInfo powerInfo = PowerInfo.GetPowerInfo(power);

		start = powerInfo.GetCornerPointIndex(edge);
		end = powerInfo.GetCornerPointIndex((edge + 1) & 3);

		if (edge == BSPFileCommon.NEIGHBOREDGE_RIGHT || edge == BSPFileCommon.NEIGHBOREDGE_BOTTOM) {
			if (span == NeighborSpan.CORNER_TO_MIDPOINT)
				start[freeDim] = (short)powerInfo.GetMidPoint();
			else if (span == NeighborSpan.MIDPOINT_TO_CORNER)
				end[freeDim] = (short)powerInfo.GetMidPoint();
		}
		else {
			if (span == NeighborSpan.CORNER_TO_MIDPOINT)
				end[freeDim] = (short)powerInfo.GetMidPoint();
			else if (span == NeighborSpan.MIDPOINT_TO_CORNER)
				start[freeDim] = (short)powerInfo.GetMidPoint();
		}
	}

	public static DispUtilsHelper TransformIntoSubNeighbor(DispUtilsHelper disp, int edge, int sub, in VertIndex nodeIndex, ref VertIndex outv) {
		DispSubNeighbor subNeighbor = disp.GetEdgeNeighbor(edge).SubNeighbors[sub];

		VertIndex srcStart = default, srcEnd = default;
		SetupSpan(disp.GetPower(), edge, subNeighbor.GetSpan(), ref srcStart, ref srcEnd);

		DispUtilsHelper neighbor = disp.GetDispUtilsByIndex(subNeighbor.GetNeighborIndex());
		int nbEdge = (edge + 2 + (int)subNeighbor.GetNeighborOrientation()) & 3;

		VertIndex destStart = default, destEnd = default;
		SetupSpan(neighbor.GetPower(), nbEdge, subNeighbor.GetNeighborSpan(), ref destEnd, ref destStart);

		int freeDim = g_EdgeDims[edge] ^ 1;
		int fixedPercent = ((nodeIndex[freeDim] - srcStart[freeDim]) * (1 << 16)) / (srcEnd[freeDim] - srcStart[freeDim]);
		Assert(fixedPercent >= 0 && fixedPercent <= (1 << 16));

		int nbDim = g_EdgeDims[nbEdge];
		outv[nbDim] = destStart[nbDim];
		outv[nbDim ^ 1] = (short)(destStart[nbDim ^ 1] + ((destEnd[nbDim ^ 1] - destStart[nbDim ^ 1]) * fixedPercent) / (1 << 16));

		Assert(outv.X >= 0 && outv.X < neighbor.GetSideLength());
		Assert(outv.Y >= 0 && outv.Y < neighbor.GetSideLength());

		return neighbor;
	}

	public static DispUtilsHelper? SetupEdgeIncrements(DispUtilsHelper disp, int edge, int sub, ref VertIndex myIndex, ref VertIndex myInc, ref VertIndex nbIndex, ref VertIndex nbInc, ref int myEnd, ref int freeDim) {
		int edgeDim = g_EdgeDims[edge];
		freeDim = edgeDim ^ 1;

		DispNeighbor side = disp.GetEdgeNeighbor(edge);
		DispSubNeighbor subNeighbor = side.SubNeighbors[sub];
		if (!subNeighbor.IsValid())
			return null;

		DispUtilsHelper neighbor = disp.GetDispUtilsByIndex(subNeighbor.GetNeighborIndex());

		ref ShiftInfo shiftInfo = ref g_ShiftInfos[(int)subNeighbor.GetSpan(), (int)subNeighbor.GetNeighborSpan()];
		Assert(shiftInfo.Valid);

		VertIndex tempInc = default;

		PowerInfo powerInfo = disp.GetPowerInfo();
		myIndex[edgeDim] = (short)(g_EdgeSideLenMul[edge] * powerInfo.SideLengthM1);
		myIndex[freeDim] = (short)(powerInfo.MidPoint * sub);
		TransformIntoSubNeighbor(disp, edge, sub, myIndex, ref nbIndex);

		int myPower = disp.GetPowerInfo().GetPower();
		int nbPower = neighbor.GetPowerInfo().GetPower() + shiftInfo.PowerShiftAdd;

		myInc[edgeDim] = tempInc[edgeDim] = 0;
		if (nbPower > myPower) {
			myInc[freeDim] = 1;
			tempInc[freeDim] = (short)(1 << (nbPower - myPower));
		}
		else {
			myInc[freeDim] = (short)(1 << (myPower - nbPower));
			tempInc[freeDim] = 1;
		}
		RotateVertIncrement(subNeighbor.GetNeighborOrientation(), tempInc, ref nbInc);

		if (subNeighbor.GetSpan() == NeighborSpan.CORNER_TO_MIDPOINT)
			myEnd = disp.GetPowerInfo().GetSideLength() >> 1;
		else
			myEnd = disp.GetPowerInfo().GetSideLength() - 1;

		return neighbor;
	}

	public static readonly ShiftInfo[,] g_ShiftInfos =
	{
		{
			new ShiftInfo(0,  0, true),
			new ShiftInfo(0, -1, true),
			new ShiftInfo(2, -1, true)
		},
		{
			new ShiftInfo(0,  1, true),
			new ShiftInfo(0,  0, false),
			new ShiftInfo(0,  0, false)
		},
		{
			new ShiftInfo(-1, 1, true),
			new ShiftInfo(0,  0, false),
			new ShiftInfo(0,  0, false)
		}
	};
}

public struct ShiftInfo(int midPointScale, int powerShiftAdd, bool valid)
{
	public int MidPointScale = midPointScale;
	public int PowerShiftAdd = powerShiftAdd;
	public bool Valid = valid;
}

public class DispSubEdgeIterator
{
	DispUtilsHelper? Neighbor;

	VertIndex Index;
	VertIndex Inc;

	VertIndex NBIndex;
	VertIndex NBInc;

	int End;
	int FreeDim;

	public DispSubEdgeIterator() {
		Neighbor = null;
		Index.X = Inc.X = NBIndex.X = NBInc.X = 0;
		FreeDim = End = 0;
	}

	public void Start(DispUtilsHelper disp, int edge, int sub, bool touchCorners = false) {
		Neighbor = DispUtilsHelper.SetupEdgeIncrements(disp, edge, sub, ref Index, ref Inc, ref NBIndex, ref NBInc, ref End, ref FreeDim);
		if (Neighbor != null) {
			if (touchCorners) {
				Index -= Inc;
				NBIndex -= NBInc;

				End += Inc[FreeDim];
			}
		}
		else {
			Index.X = Inc.X = 0;
			FreeDim = End = 0;
		}
	}

	public bool Next() {
		Index += Inc;
		NBIndex += NBInc;

		return Index[FreeDim] < End;
	}

	public ref VertIndex GetVertIndex() => ref Index;
	public ref VertIndex GetNBVertIndex() => ref NBIndex;
	public DispUtilsHelper? GetNeighbor() => Neighbor;

	public bool IsLastVert() => (Index[FreeDim] + Inc[FreeDim]) >= End;
}
