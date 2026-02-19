using Source.Common.Commands;

using System.Numerics;

namespace Game.Server.NavMesh;

public partial class NavMesh
{
	Vector3 SnapToGrid(Vector3 vec, bool snapX, bool snapY, bool forceGrid) {
		throw new NotImplementedException();
	}

	float SnapToGrid(float x, bool forceGrid) {
		throw new NotImplementedException();
	}

	void GetEditVectors(Vector3 pos, Vector3 forward) { }

	// void SetEditMode(EditModeType mode) {
	// 	throw new NotImplementedException();
	// }

	bool FindNavAreaOrLadderAlongRay(Vector3 start, Vector3 end, NavArea bestArea, NavLadder bestLadder, NavArea ignore) {
		throw new NotImplementedException();
	}

	bool FindActiveNavArea() {
		throw new NotImplementedException();
	}

	bool FindLadderCorners(Vector3 corner1, Vector3 corner2, Vector3 corner3) {
		throw new NotImplementedException();
	}

	void CommandNavBuildLadder() { }

	void OnEditModeStart() { }

	void OnEditModeEnd() { }

	void UpdateDragSelectionSet() { }

	void DrawEditMode() { }

	void SetMarkedLadder(NavLadder ladder) { }

	void SetMarkedArea(NavArea area) { }

	public uint GetNavPlace() => NavPlace;
	public void SetNavPlace(uint place) => NavPlace = place;

	void CommandNavDelete() { }

	void CommandNavDeleteMarked() { }

	void CommandNavFloodSelect(in TokenizedCommand args) { }

	void CommandNavToggleSelectedSet() { }

	void CommandNavStoreSelectedSet() { }

	void CommandNavRecallSelectedSet() { }

	void CommandNavAddToSelectedSet() { }

	void CommandNavAddToSelectedSetByID(in TokenizedCommand args) { }

	void CommandNavRemoveFromSelectedSet() { }

	void CommandNavToggleInSelectedSet() { }

	void CommandNavClearSelectedSet() { }

	void CommandNavBeginSelecting() { }

	void CommandNavEndSelecting() { }

	void CommandNavBeginDragSelecting() { }

	void CommandNavEndDragSelecting() { }

	void CommandNavBeginDragDeselecting() { }

	void CommandNavEndDragDeselecting() { }

	void CommandNavRaiseDragVolumeMax() { }

	void CommandNavLowerDragVolumeMax() { }

	void CommandNavRaiseDragVolumeMin() { }

	void CommandNavLowerDragVolumeMin() { }

	void CommandNavToggleSelecting(bool playSound) { }

	void CommandNavBeginDeselecting() { }

	void CommandNavEndDeselecting() { }

	void CommandNavToggleDeselecting(bool playSound) { }

	void CommandNavSelectHalfSpace(in TokenizedCommand args) { }

	void CommandNavBeginShiftXY() { }

	void CommandNavEndShiftXY() { }

	void CommandNavSelectInvalidAreas() { }

	void CommandNavSelectBlockedAreas() { }

	void CommandNavSelectObstructedAreas() { }

	void CommandNavSelectDamagingAreas() { }

	void CommandNavSelectStairs() { }

	void CommandNavSelectOrphans() { }

	void CommandNavSplit() { }

	void CommandNavMakeSniperSpots() { }

	void CommandNavMerge() { }

	void CommandNavMark(in TokenizedCommand args) { }

	void CommandNavUnmark() { }

	void CommandNavBeginArea() { }

	void CommandNavEndArea() { }

	void CommandNavConnect() { }

	void CommandNavDisconnect() { }

	void CommandNavDisconnectOutgoingOneWays() { }

	void CommandNavSplice() { }

	void DoToggleAttribute(NavArea area, NavAttributeType attribute) { }

	void CommandNavToggleAttribute(NavAttributeType attribute) { }

	void CommandNavTogglePlaceMode() { }

	void CommandNavPlaceFloodFill() { }

	void CommandNavPlaceSet() { }

	void CommandNavPlacePick() { }

	void CommandNavTogglePlacePainting() { }

	void CommandNavMarkUnnamed() { }

	void CommandNavCornerSelect() { }

	void CommandNavCornerRaise(in TokenizedCommand args) { }

	void CommandNavCornerLower(in TokenizedCommand args) { }

	void CommandNavCornerPlaceOnGround(in TokenizedCommand args) { }

	void CommandNavWarpToMark() { }

	void CommandNavLadderFlip() { }

	void AddToSelectedSet(NavArea area) { }

	void RemoveFromSelectedSet(NavArea area) { }

	void AddToDragSelectionSet(NavArea area) { }

	void RemoveFromDragSelectionSet(NavArea area) { }

	void ClearDragSelectionSet() { }

	void ClearSelectedSet() { }

	bool IsSelectedSetEmpty() {
		throw new NotImplementedException();
	}

	int GetSelecteSetSize() {
		throw new NotImplementedException();
	}

	// NavAreaVector GetSelectedSet() {
	// 	throw new NotImplementedException();
	// }

	bool IsInSelectedSet(NavArea area) {
		throw new NotImplementedException();
	}

	void OnEditCreateNotify(NavArea newArea) { }

	void OnEditDestroyNotify(NavArea deadArea) { }

	void OnEditDestroyNotify(NavLadder deadLadder) {
		throw new NotImplementedException();
	}
}