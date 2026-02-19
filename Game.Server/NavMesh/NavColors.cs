using Game.Shared;

using Source;

using System.Numerics;

namespace Game.Server.NavMesh;

public static class NavColors
{

	public enum NavEditColor
	{
		// Degenerate area colors
		NavDegenerateFirstColor = 0,
		NavDegenerateSecondColor,

		// Place painting color
		NavSamePlaceColor,
		NavDifferentPlaceColor,
		NavNoPlaceColor,

		// Normal colors
		NavSelectedColor,
		NavMarkedColor,
		NavNormalColor,
		NavCornerColor,
		NavBlockedByDoorColor,
		NavBlockedByFuncNavBlockerColor,

		// Hiding spot colors
		NavIdealSniperColor,
		NavGoodSniperColor,
		NavGoodCoverColor,
		NavExposedColor,
		NavApproachPointColor,

		// Connector colors
		NavConnectedTwoWaysColor,
		NavConnectedOneWayColor,
		NavConnectedContiguous,
		NavConnectedNonContiguous,

		// Editing colors
		NavCursorColor,
		NavSplitLineColor,
		NavCreationColor,
		NavInvalidCreationColor,
		NavGridColor,
		NavDragSelectionColor,

		// Nav attribute colors
		NavAttributeCrouchColor,
		NavAttributeJumpColor,
		NavAttributePreciseColor,
		NavAttributeNoJumpColor,
		NavAttributeStopColor,
		NavAttributeRunColor,
		NavAttributeWalkColor,
		NavAttributeAvoidColor,
		NavAttributeStairColor
	}

	static Color[] Colors = [
		// Degenerate area colors
		new(255, 255, 0),	// NavDegenerateFirstColor
		new(255, 0, 0),		// NavDegenerateSecondColor

		// Place painting color
		new(0, 255, 0),		// NavSamePlaceColor
		new(0, 0, 0),			// NavDifferentPlaceColor
		new(255, 0, 0),		// NavNoPlaceColor

		// Normal colors
		new(255, 255, 0),	// NavSelectedColor
		new(0, 255, 0),		// NavMarkedColor
		new(255, 0, 0),		// NavNormalColor
		new(0, 0, 0),			// NavCornerColor
		new(0, 0, 0),			// NavBlockedByDoorColor
		new(0, 255, 0),		// NavBlockedByFuncNavBlockerColor

		// Hiding spot colors
		new(255, 0, 0),		// NavIdealSniperColor
		new(255, 0, 0),		// NavGoodSniperColor
		new(0, 255, 0),		// NavGoodCoverColor
		new(255, 0, 0),		// NavExposedColor
		new(255, 100, 0),	// NavApproachPointColor

		// Connector colors
		new(0, 255, 0),		// NavConnectedTwoWaysColor
		new(0, 0, 0),			// NavConnectedOneWayColor
		new(0, 255, 0),		// NavConnectedContiguous
		new(255, 0, 0),		// NavConnectedNonContiguous

		// Editing colors
		new(255, 255, 0), // NavCursorColor
		new(255, 255, 0),	// NavSplitLineColor
		new(0, 255, 0),		// NavCreationColor
		new(255, 0, 0),		// NavInvalidCreationColor
		new(0, 64, 64 ),	// NavGridColor
		new(255, 255, 0),	// NavDragSelectionColor

		// Nav attribute colors
		new(0, 0, 0),			// NavAttributeCrouchColor
		new(0, 255, 0),		// NavAttributeJumpColor
		new(0, 255, 0),		// NavAttributePreciseColor
		new(255, 0, 0),		// NavAttributeNoJumpColor
		new(255, 0, 0),		// NavAttributeStopColor
		new(0, 0, 0),			// NavAttributeRunColor
		new(0, 255, 0),		// NavAttributeWalkColor
		new(255, 0, 0),		// NavAttributeAvoidColor
		new(0, 200, 0),   // NavAttributeStairColor
	];

	public static void NavDrawLine(in Vector3 from, in Vector3 to, NavEditColor navColor) {
		Vector3 offset = new Vector3(0f, 0f, 1f);
		Color color = Colors[(int)navColor];

		DebugOverlay.Line(from + offset, to + offset, color[0], color[1], color[2], false, DebugOverlay.Persist);
		DebugOverlay.Line(from + offset, to + offset, color[0] / 2, color[1] / 2, color[2] / 2, true, DebugOverlay.Persist);
	}

	public static void NavDrawTriangle(in Vector3 p1, in Vector3 p2, in Vector3 p3, NavEditColor navColor) {
		NavDrawLine(p1, p2, navColor);
		NavDrawLine(p2, p3, navColor);
		NavDrawLine(p1, p3, navColor);
	}

	public static void NavDrawFilledTriangle(in Vector3 p1, in Vector3 p2, in Vector3 p3, NavEditColor navColor, bool dark) {
		Color color = Colors[(int)navColor];

		if (dark) {
			color[0] = (byte)(color[0] / 2);
			color[1] = (byte)(color[1] / 2);
			color[2] = (byte)(color[2] / 2);
		}

		DebugOverlay.Triangle(p1, p2, p3, color[0], color[1], color[2], 255, true, DebugOverlay.Persist);
	}

	public static void NavDrawHorizontalArrow(in Vector3 from, in Vector3 to, float width, NavEditColor navColor) {
		Vector3 offset = new Vector3(0f, 0f, 1f);
		Color color = Colors[(int)navColor];

		DebugOverlay.HorzArrow(from + offset, to + offset, width, color[0], color[1], color[2], 255, false, DebugOverlay.Persist);
		DebugOverlay.HorzArrow(from + offset, to + offset, width, color[0] / 2, color[1] / 2, color[2] / 2, 255, true, DebugOverlay.Persist);
	}

	public static void NavDrawDashedLine(in Vector3 from, in Vector3 to, NavEditColor navColor) {
		Vector3 offset = new Vector3(0f, 0f, 1f);
		Color color = Colors[(int)navColor];

		const float solidLen = 7f;
		const float gapLen = 3f;

		Vector3 unit = Vector3.Normalize(to - from);
		float totalDistance = Vector3.Distance(from, to);

		float distance = 0f;

		while (distance < totalDistance) {
			Vector3 start = from + unit * distance;
			float endDistance = MathF.Min(distance + solidLen, totalDistance);
			Vector3 end = from + unit * endDistance;

			distance += solidLen + gapLen;

			DebugOverlay.Line(start + offset, end + offset, color[0], color[1], color[2], false, DebugOverlay.Persist);
			DebugOverlay.Line(start + offset, end + offset, color[0] / 2, color[1] / 2, color[2] / 2, true, DebugOverlay.Persist);
		}
	}

	public static void NavDrawVolume(in Vector3 vMin, in Vector3 vMax, float zMidline, NavEditColor navColor) {
		NavDrawLine(new Vector3(vMax.X, vMax.Y, zMidline), new Vector3(vMin.X, vMax.Y, zMidline), navColor);
		NavDrawLine(new Vector3(vMin.X, vMin.Y, zMidline), new Vector3(vMin.X, vMax.Y, zMidline), navColor);
		NavDrawLine(new Vector3(vMin.X, vMin.Y, zMidline), new Vector3(vMax.X, vMin.Y, zMidline), navColor);
		NavDrawLine(new Vector3(vMax.X, vMax.Y, zMidline), new Vector3(vMax.X, vMin.Y, zMidline), navColor);

		NavDrawLine(new Vector3(vMax.X, vMax.Y, vMin.Z), new Vector3(vMin.X, vMax.Y, vMin.Z), navColor);
		NavDrawLine(new Vector3(vMin.X, vMin.Y, vMin.Z), new Vector3(vMin.X, vMax.Y, vMin.Z), navColor);
		NavDrawLine(new Vector3(vMin.X, vMin.Y, vMin.Z), new Vector3(vMax.X, vMin.Y, vMin.Z), navColor);
		NavDrawLine(new Vector3(vMax.X, vMax.Y, vMin.Z), new Vector3(vMax.X, vMin.Y, vMin.Z), navColor);

		NavDrawLine(new Vector3(vMax.X, vMax.Y, vMax.Z), new Vector3(vMin.X, vMax.Y, vMax.Z), navColor);
		NavDrawLine(new Vector3(vMin.X, vMin.Y, vMax.Z), new Vector3(vMin.X, vMax.Y, vMax.Z), navColor);
		NavDrawLine(new Vector3(vMin.X, vMin.Y, vMax.Z), new Vector3(vMax.X, vMin.Y, vMax.Z), navColor);
		NavDrawLine(new Vector3(vMax.X, vMax.Y, vMax.Z), new Vector3(vMax.X, vMin.Y, vMax.Z), navColor);

		NavDrawLine(new Vector3(vMax.X, vMax.Y, vMin.Z), new Vector3(vMax.X, vMax.Y, vMax.Z), navColor);
		NavDrawLine(new Vector3(vMin.X, vMin.Y, vMin.Z), new Vector3(vMin.X, vMin.Y, vMax.Z), navColor);
		NavDrawLine(new Vector3(vMax.X, vMin.Y, vMin.Z), new Vector3(vMax.X, vMin.Y, vMax.Z), navColor);
		NavDrawLine(new Vector3(vMin.X, vMax.Y, vMin.Z), new Vector3(vMin.X, vMax.Y, vMax.Z), navColor);
	}
}