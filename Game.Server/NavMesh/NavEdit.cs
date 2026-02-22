using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Server.NavMesh;

public partial class NavMesh
{
	static readonly ConVar nav_show_area_info = new("0.5", FCvar.Cheat, "Duration in seconds to show nav area ID and attributes while editing");
	static readonly ConVar nav_snap_to_grid = new("0", FCvar.Cheat, "Snap to the nav generation grid when creating new nav areas");
	static readonly ConVar nav_create_place_on_ground = new("0", FCvar.Cheat, "If true, nav areas will be placed flush with the ground when created by hand.");
	public static readonly ConVar nav_draw_limit = new("500", FCvar.Cheat, "The maximum number of areas to draw in edit mode");
	static readonly ConVar nav_solid_props = new("0", FCvar.Cheat, "Make props solid to nav generation/editing");
	static readonly ConVar nav_create_area_at_feet = new("0", FCvar.Cheat, "Anchor nav_begin_area Z to editing player's feet");
	static readonly ConVar nav_drag_selection_volume_zmax_offset = new("32", FCvar.Replicated, "The offset of the nav drag volume top from center");
	static readonly ConVar nav_drag_selection_volume_zmin_offset = new("32", FCvar.Replicated, "The offset of the nav drag volume bottom from center");
	static readonly ConVar nav_show_compass = new("0", FCvar.Cheat);

	Vector3 SnapToGrid(Vector3 vec, bool snapX, bool snapY, bool forceGrid) {
		throw new NotImplementedException();
	}

	float SnapToGrid(float x, bool forceGrid) {
		throw new NotImplementedException();
	}

	public void GetEditVectors(out Vector3 pos, out Vector3 forward) {
		pos = default;
		forward = default;

		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		MathLib.AngleVectors(player.EyeAngles() /*+ player.GetPunchAngle()*/, out forward); // todo punch angles
		pos = player.EyePosition();
	}

	void SetEditMode(EditModeType mode) {
		MarkedLadder = null;
		MarkedArea = null;
		MarkedCorner = NavCornerType.NumCorners;

		EditMode = mode;

		ContinuouslySelecting = false;
		ContinuouslyDeselecting = false;
		IsDragDeselecting = false;
	}

	bool FindNavAreaOrLadderAlongRay(Vector3 start, Vector3 end, NavArea bestArea, NavLadder bestLadder, NavArea ignore) {
		throw new NotImplementedException();
	}

	bool FindActiveNavArea() {
		throw new NotImplementedException();
	}

	public bool FindLadderCorners(out Vector3 corner1, out Vector3 corner2, out Vector3 corner3) {
		corner1 = default;
		corner2 = default;
		corner3 = default;

		MathLib.VectorVectors(LadderNormal, out Vector3 ladderRight, out Vector3 ladderUp);
		GetEditVectors(out Vector3 from, out Vector3 dir);

		const float maxDist = 100000f;

		Source.Common.Ray ray = new();//from, from + dir * maxDist

		corner1 = LadderAnchor + ladderUp * maxDist + ladderRight * maxDist;
		corner2 = LadderAnchor + ladderUp * maxDist - ladderRight * maxDist;
		corner3 = LadderAnchor - ladderUp * maxDist - ladderRight * maxDist;

		float dist = CollisionUtils.IntersectRayWithTriangle(ray, corner1, corner2, corner3, false);

		if (dist < 0f) {
			corner2 = LadderAnchor - ladderUp * maxDist + ladderRight * maxDist;
			dist = CollisionUtils.IntersectRayWithTriangle(ray, corner1, corner2, corner3, false);
		}

		corner3 = EditCursorPos;

		if (dist > 0f && dist < maxDist) {
			corner3 = from + dir * (dist * maxDist);

			float vertDistance = corner3.Z - LadderAnchor.Z;
			float val = vertDistance / ladderUp.Z;

			corner1 = LadderAnchor + ladderUp * val;
			corner2 = corner3 - ladderUp * val;

			return true;
		}

		return false;
	}


	void CommandNavBuildLadder() { }

	void OnEditModeStart() {
		ClearSelectedSet();
		ContinuouslySelecting = false;
		ContinuouslyDeselecting = false;
	}

	void OnEditModeEnd() { }

	void UpdateDragSelectionSet() { }

	static Color DragSelectionSetAddColor = new(100, 255, 100, 96);
	static Color DragSelectionSetDeleteColor = new(255, 100, 100, 96);

	void DrawEditMode() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (IsGenerating())
			return;

		// host_thread_mode

		const float maxRange = 1000.0f;

		GetEditVectors(out Vector3 from, out Vector3 dir);

		Vector3 to = from + maxRange * dir;

		if (FindActiveNavArea() || MarkedArea != null || MarkedLadder != null || !IsSelectedSetEmpty() || IsEditMode(EditModeType.CreatingArea) || IsEditMode(EditModeType.CreatingLadder)) {
			const float cursorSize = 10.0f;

			if (ClimbableSurface)
				DebugOverlay.Cross3D(EditCursorPos, cursorSize, 0, 255, 0, true, DebugOverlay.Persist);
			else {
				NavColors.NavDrawLine(EditCursorPos + new Vector3(0, 0, cursorSize), EditCursorPos, NavColors.NavEditColor.NavCursorColor);
				NavColors.NavDrawLine(EditCursorPos + new Vector3(cursorSize, 0, 0), EditCursorPos + new Vector3(-cursorSize, 0, 0), NavColors.NavEditColor.NavCursorColor);
				NavColors.NavDrawLine(EditCursorPos + new Vector3(0, cursorSize, 0), EditCursorPos + new Vector3(0, -cursorSize, 0), NavColors.NavEditColor.NavCursorColor);

				if (nav_show_compass.GetBool()) {
					const float offset = cursorSize * 1.5f;
					Vector3 pos;

					pos = EditCursorPos;
					Nav.AddDirectionVector(ref pos, NavDirType.North, offset);
					DebugOverlay.Text(pos, "N", false, DebugOverlay.Persist);

					pos = EditCursorPos;
					Nav.AddDirectionVector(ref pos, NavDirType.South, offset);
					DebugOverlay.Text(pos, "S", false, DebugOverlay.Persist);

					pos = EditCursorPos;
					Nav.AddDirectionVector(ref pos, NavDirType.East, offset);
					DebugOverlay.Text(pos, "E", false, DebugOverlay.Persist);

					pos = EditCursorPos;
					Nav.AddDirectionVector(ref pos, NavDirType.West, offset);
					DebugOverlay.Text(pos, "W", false, DebugOverlay.Persist);
				}
			}

			if (IsEditMode(EditModeType.CreatingArea)) {
				float z = Anchor.Z + 2.0f;
				NavColors.NavDrawLine(new Vector3(EditCursorPos.X, EditCursorPos.Y, z), new Vector3(Anchor.X, EditCursorPos.Y, z), NavColors.NavEditColor.NavCreationColor);
				NavColors.NavDrawLine(new Vector3(Anchor.X, Anchor.Y, z), new Vector3(Anchor.X, EditCursorPos.Y, z), NavColors.NavEditColor.NavCreationColor);
				NavColors.NavDrawLine(new Vector3(Anchor.X, Anchor.Y, z), new Vector3(EditCursorPos.X, Anchor.Y, z), NavColors.NavEditColor.NavCreationColor);
				NavColors.NavDrawLine(new Vector3(EditCursorPos.X, EditCursorPos.Y, z), new Vector3(EditCursorPos.X, Anchor.Y, z), NavColors.NavEditColor.NavCreationColor);
			}
			else if (IsEditMode(EditModeType.DragSelecting)) {
				float z1 = Anchor.Z + DragSelectionVolumeZMax;
				float z2 = Anchor.Z - DragSelectionVolumeZMin;

				Vector3 vMin = new(Anchor.X, Anchor.Y, z1);
				Vector3 vMax = new(EditCursorPos.X, EditCursorPos.Y, z2);
				NavColors.NavDrawVolume(vMin, vMax, Anchor.Z, NavColors.NavEditColor.NavDragSelectionColor);

				UpdateDragSelectionSet();

				Color dragSelectionColor = IsDragDeselecting ? DragSelectionSetDeleteColor : DragSelectionSetAddColor;

				foreach (NavArea area in DragSelectionSet)
					area.DrawDragSelectionSet(dragSelectionColor);
			}
			else if (IsEditMode(EditModeType.CreatingLadder)) {
				if (FindLadderCorners(out Vector3 corner1, out Vector3 corner2, out Vector3 corner3)) {
					NavColors.NavEditColor color = NavColors.NavEditColor.NavCreationColor;
					if (!ClimbableSurface) {
						color = NavColors.NavEditColor.NavInvalidCreationColor;
					}

					NavColors.NavDrawLine(LadderAnchor, corner1, color);
					NavColors.NavDrawLine(corner1, corner3, color);
					NavColors.NavDrawLine(corner3, corner2, color);
					NavColors.NavDrawLine(corner2, LadderAnchor, color);
				}
			}

			if (SelectedLadder != null) {
				LastSelectedArea = null;

				if (SelectedLadder != LastSelectedLadder || nav_show_area_info.GetBool()) {
					LastSelectedLadder = SelectedLadder;

					Span<char> buffer = stackalloc char[80];

					BaseEntity? ladderEntity = SelectedLadder.GetLadderEntity();
					if (ladderEntity != null)
						sprintf(buffer, "Ladder #%d (Team %d)\n").D(SelectedLadder.GetID()).D(/*ladderEntity.GetTeamNumber()*/0);
					else
						sprintf(buffer, "Ladder #%d\n").D(SelectedLadder.GetID());
					DebugOverlay.ScreenText(0.5f, 0.53f, buffer, 255, 255, 0, 128, DebugOverlay.Persist);
				}

				SelectedLadder.DrawLadder();
				SelectedLadder.DrawConnectedAreas();
			}

			if (MarkedLadder != null && !IsEditMode(EditModeType.PlacePainting))
				MarkedLadder.DrawLadder();

			if (MarkedArea != null && !IsEditMode(EditModeType.PlacePainting))
				MarkedArea.Draw();

			if (SelectedArea != null) {
				LastSelectedLadder = null;

				if (SelectedArea != LastSelectedArea) {
					ShowAreaInfoTimer.Start(nav_show_area_info.GetFloat());
					LastSelectedArea = SelectedArea;
				}

				if (ShowAreaInfoTimer.HasStarted() && !ShowAreaInfoTimer.IsElapsed()) {
					Span<char> buffer = stackalloc char[80];
					Span<char> attrib = stackalloc char[80];
					Span<char> locName = stackalloc char[80];

					if (SelectedArea.GetPlace() != 0) {
						ReadOnlySpan<char> name = Instance!.PlaceToName(SelectedArea.GetPlace());
						if (!name.IsEmpty)
							strcpy(locName, name);
						else
							strcpy(locName, "ERROR");
					}
					else
						locName[0] = '\0';

					if (IsEditMode(EditModeType.PlacePainting))
						attrib[0] = '\0';
					else {
						attrib[0] = '\0';
						NavAttributeType attributes = (NavAttributeType)SelectedArea.GetAttributes();
						if ((attributes & NavAttributeType.Crouch) != 0) strcat(attrib, "CROUCH ");
						if ((attributes & NavAttributeType.Jump) != 0) strcat(attrib, "JUMP ");
						if ((attributes & NavAttributeType.Precice) != 0) strcat(attrib, "PRECISE ");
						if ((attributes & NavAttributeType.NoJump) != 0) strcat(attrib, "NO_JUMP ");
						if ((attributes & NavAttributeType.Stop) != 0) strcat(attrib, "STOP ");
						if ((attributes & NavAttributeType.Run) != 0) strcat(attrib, "RUN ");
						if ((attributes & NavAttributeType.Walk) != 0) strcat(attrib, "WALK ");
						if ((attributes & NavAttributeType.Avoid) != 0) strcat(attrib, "AVOID ");
						if ((attributes & NavAttributeType.Transient) != 0) strcat(attrib, "TRANSIENT ");
						if ((attributes & NavAttributeType.DontHide) != 0) strcat(attrib, "DONT_HIDE ");
						if ((attributes & NavAttributeType.Stand) != 0) strcat(attrib, "STAND ");
						if ((attributes & NavAttributeType.NoHostages) != 0) strcat(attrib, "NO HOSTAGES ");
						if ((attributes & NavAttributeType.Stairs) != 0) strcat(attrib, "STAIRS ");
						if ((attributes & NavAttributeType.ObstacleTop) != 0) strcat(attrib, "OBSTACLE ");
						if ((attributes & NavAttributeType.Cliff) != 0) strcat(attrib, "CLIFF ");
						if (SelectedArea.IsBlocked(-2 /*TEAM_ANY*/)) strcat(attrib, "BLOCKED ");
						if (SelectedArea.HasAvoidanceObstacle()) strcat(attrib, "OBSTRUCTED ");
						if (SelectedArea.IsDamaging()) strcat(attrib, "DAMAGING ");
						if (SelectedArea.IsUnderwater) strcat(attrib, "UNDERWATER ");

						int connected = 0;
						connected += SelectedArea.GetAdjacentCount(NavDirType.North);
						connected += SelectedArea.GetAdjacentCount(NavDirType.South);
						connected += SelectedArea.GetAdjacentCount(NavDirType.East);
						connected += SelectedArea.GetAdjacentCount(NavDirType.West);
						strcat(attrib, connected + " Connections ");
					}

					sprintf(buffer, "Area #%d %s %s\n").D(SelectedArea.GetID()).S(locName).S(attrib);
					DebugOverlay.ScreenText(0.5f, 0.53f, buffer, 255, 255, 0, 128, DebugOverlay.Persist);

					if (IsPlacePainting) {
						if (SelectedArea.GetPlace() != Instance!.GetNavPlace()) {
							SelectedArea.SetPlace(Instance!.GetNavPlace());
							// player.EmitSound("Bot.EditSwitchOn");
						}
					}
				}


				if (ContinuouslySelecting)
					AddToSelectedSet(SelectedArea);
				else if (ContinuouslyDeselecting)
					RemoveFromSelectedSet(SelectedArea);

				if (IsEditMode(EditModeType.PlacePainting))
					SelectedArea.DrawConnectedAreas();
				else {
					Extent extent = default;
					SelectedArea.GetExtent(ref extent);

					float yaw = player.EyeAngles().Y;
					while (yaw > 360.0f)
						yaw -= 360.0f;

					while (yaw < 0.0f)
						yaw += 360.0f;

					if (SplitAlongX) {
						from.X = extent.Lo.X;
						from.Y = SplitEdge;
						from.Z = SelectedArea.GetZ(from);

						to.X = extent.Hi.X;
						to.Y = SplitEdge;
						to.Z = SelectedArea.GetZ(to);
					}
					else {
						from.X = SplitEdge;
						from.Y = extent.Lo.Y;
						from.Z = SelectedArea.GetZ(from);

						to.X = SplitEdge;
						to.Y = extent.Hi.Y;
						to.Z = SelectedArea.GetZ(to);
					}

					NavColors.NavDrawLine(from, to, NavColors.NavEditColor.NavSplitLineColor);

					SelectedArea.DrawConnectedAreas();
				}
			}

			if (!IsSelectedSetEmpty()) {
				Vector3 shift = new(0, 0, 0);

				if (IsEditMode(EditModeType.ShiftingXY)) {
					shift = EditCursorPos - Anchor;
					shift.Z = 0.0f;
				}

				DrawSelectedSet draw = new(shift);

				if (SelectedSet.Count < nav_draw_limit.GetInt()) {
					foreach (NavArea area in SelectedSet) {
						draw.Invoke(area);
					}
				}
				else {
					NavArea? nearest = null;
					float nearRange = 9999999999.9f;

					foreach (var area in SelectedSet) {
						float range = (player.GetAbsOrigin() - area.GetCenter()).LengthSqr();
						if (range < nearRange) {
							nearRange = range;
							nearest = area;
						}
					}

					// SearchSurroundingAreas(nearest, nearest.GetCenter(), draw, -1, INCLUDE_INCOMING_CONNECTIONS | INCLUDE_BLOCKED_AREAS);
				}
			}
		}
	}

	void SetMarkedLadder(NavLadder ladder) {
		MarkedArea = null;
		MarkedLadder = ladder;
		MarkedCorner = NavCornerType.NumCorners;
	}

	void SetMarkedArea(NavArea area) {
		MarkedLadder = null;
		MarkedArea = area;
		MarkedCorner = NavCornerType.NumCorners;
	}

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

	public void AddToSelectedSet(NavArea area) { }

	void RemoveFromSelectedSet(NavArea area) => SelectedSet.Remove(area);

	void AddToDragSelectionSet(NavArea area) { }

	void RemoveFromDragSelectionSet(NavArea area) => DragSelectionSet.Remove(area);

	void ClearDragSelectionSet() => DragSelectionSet.Clear();

	void ClearSelectedSet() => SelectedSet.Clear();

	bool IsSelectedSetEmpty() => SelectedSet.Count == 0;

	int GetSelecteSetSize() => SelectedSet.Count;

	List<NavArea> GetSelectedSet() => SelectedSet;

	public bool IsInSelectedSet(NavArea area) => SelectedSet.Contains(area);

	void OnEditCreateNotify(NavArea newArea) { }

	void OnEditDestroyNotify(NavArea deadArea) { }

	void OnEditDestroyNotify(NavLadder deadLadder) {
		throw new NotImplementedException();
	}
}

public class DrawSelectedSet(Vector3 shift)
{
	public int Count = 0;
	public Vector3 Shift = shift;

	public bool Invoke(NavArea area) {
		if (NavMesh.Instance!.IsInSelectedSet(area)) {
			area.DrawSelectedSet(Shift);
			Count++;
		}

		return Count < NavMesh.nav_draw_limit.GetInt();
	}
}
