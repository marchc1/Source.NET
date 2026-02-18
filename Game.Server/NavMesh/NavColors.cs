using Source;

namespace Game.Server.NavMesh;

public partial class NavArea
{

	enum NavEditColor
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

	Color[] NavColors = [
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
}