using static Game.Server.NavMesh.Nav;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Mathematics;

using System.Numerics;
using Source.Common.Formats.BSP;

namespace Game.Server.NavMesh;

public partial class NavMesh
{
	Vector3 SnapToGrid(Vector3 vec, bool snapX = true, bool snapY = true, bool forceGrid = false) {
		int scale = GetGridSize(forceGrid);
		if (scale == 0)
			return vec;

		Vector3 res = vec;

		if (snapX)
			res.X = RoundToUnits(vec.X, scale);

		if (snapY)
			res.Y = RoundToUnits(vec.Y, scale);

		return res;
	}

	public float SnapToGrid(float x, bool forceGrid = false) {
		int scale = GetGridSize();
		if (scale == 0)
			return x;

		return RoundToUnits(x, scale);
	}

	int GetGridSize(bool forceGrid = false) {
		if (Instance!.IsGenerating())
			return (int)GenerationStepSize;

		int snapVal = nav_snap_to_grid.GetInt();
		if (forceGrid && snapVal == 0)
			snapVal = 1;

		if (snapVal == 0)
			return 0;

		int scale = (int)GenerationStepSize;

		switch (snapVal) {
			case 3:
				scale = 1;
				break;
			case 2:
				scale = 5;
				break;
			case 1:
			default:
				break;
		}

		return scale;
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

	public bool FindNavAreaOrLadderAlongRay(Vector3 start, Vector3 end, out NavArea? bestArea, out NavLadder? bestLadder, NavArea? ignore = null) {
		bestArea = null;
		bestLadder = null;

		if (Grid.Count == 0)
			return false;

		Source.Common.Ray ray = new();
		ray.Init(start, end, vec3_origin, vec3_origin);

		float bestDist = 1.0f;

		for (int i = 0; i < Ladders.Count; ++i) {
			NavLadder ladder = Ladders[i];

			Vector3 left = new(0, 0, 0), right = new(0, 0, 0), up = new(0, 0, 0);
			MathLib.VectorVectors(ladder.GetNormal(), out right, out up);
			right *= ladder.Width * 0.5f;
			left = -right;

			Vector3 c1 = ladder.Top + right;
			Vector3 c2 = ladder.Top + left;
			Vector3 c3 = ladder.Bottom + right;
			Vector3 c4 = ladder.Bottom + left;
			float dist = CollisionUtils.IntersectRayWithTriangle(ray, c1, c2, c4, false);
			if (dist > 0 && dist < bestDist) {
				bestLadder = ladder;
				bestDist = dist;
			}

			dist = CollisionUtils.IntersectRayWithTriangle(ray, c1, c4, c3, false);
			if (dist > 0 && dist < bestDist) {
				bestLadder = ladder;
				bestDist = dist;
			}
		}

		Extent extent = default;
		extent.Lo = extent.Hi = start;
		extent.Encompass(end);

		int loX = WorldToGridX(extent.Lo.X);
		int loY = WorldToGridY(extent.Lo.Y);
		int hiX = WorldToGridX(extent.Hi.X);
		int hiY = WorldToGridY(extent.Hi.Y);

		for (int y = loY; y <= hiY; ++y) {
			for (int x = loX; x <= hiX; ++x) {
				List<NavArea> areaGrid = Grid[x + y * GridSizeX];

				for (int it = 0; it < areaGrid.Count(); ++it) {
					NavArea area = areaGrid[it];
					if (area == ignore)
						continue;

					Vector3 nw = area.NWCorner;
					Vector3 se = area.SECorner;
					Vector3 ne = default, sw = default;
					ne.X = se.X;
					ne.Y = nw.Y;
					ne.Z = area.NEZ;
					sw.X = nw.X;
					sw.Y = se.Y;
					sw.Z = area.SWZ;

					float dist = CollisionUtils.IntersectRayWithTriangle(ray, nw, ne, se, false);
					if (dist > 0 && dist < bestDist) {
						bestArea = area;
						bestDist = dist;
					}

					dist = CollisionUtils.IntersectRayWithTriangle(ray, se, sw, nw, false);
					if (dist > 0 && dist < bestDist) {
						bestArea = area;
						bestDist = dist;
					}
				}
			}
		}

		if (bestArea != null)
			bestLadder = null;

		return bestDist < 1.0f;
	}

	bool FindActiveNavArea() {
		SplitAlongX = false;
		SplitEdge = 0.0f;
		SelectedArea = null;
		ClimbableSurface = false;
		SelectedLadder = null;

		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return false;

		GetEditVectors(out Vector3 from, out Vector3 dir);

		float maxRange = 2000.0f;
		bool isClippingRayAtFeet = false;
		if (nav_create_area_at_feet.GetBool()) {
			if (dir.Z < 0) {
				float eyeHeight = player.GetViewOffset().Z;
				if (eyeHeight != 0.0f) {
					float rayHeight = -dir.Z * maxRange;
					maxRange = maxRange * eyeHeight / rayHeight;
					isClippingRayAtFeet = true;
				}
			}
		}

		Vector3 to = from + maxRange * dir;

		TraceFilterWalkableEntities filter = new(null, CollisionGroup.None, WalkThruFlags.Everything);
		Util.TraceLine(from, to, nav_solid_props.GetBool() ? Mask.NPCSolid : Mask.NPCSolidBrushOnly, null, ref filter, out Trace result);

		if (result.Fraction != 1.0f) {
			if (!IsEditMode(EditModeType.CreatingArea)) {
				ClimbableSurface = physprops.GetSurfaceData(result.Surface.SurfaceProps)?.Game.Climbable != 0;
				if (!ClimbableSurface)
					ClimbableSurface = (result.Contents & Contents.Ladder) != 0;
				SurfaceNormal = result.Plane.Normal;

				if (ClimbableSurface) {
					if (IsEditMode(EditModeType.CreatingLadder)) {
						if (SurfaceNormal != LadderNormal)
							ClimbableSurface = false;
					}

					if (SurfaceNormal.Z > 0.9f)
						ClimbableSurface = false;
				}
			}

			if ((ClimbableSurface && !IsEditMode(EditModeType.CreatingLadder)) || !IsEditMode(EditModeType.CreatingArea)) {
				float closestDistSqr = 200.0f * 200.0f;

				for (int i = 0; i < Ladders.Count; ++i) {
					NavLadder ladder = Ladders[i];

					Vector3 absMin = ladder.Bottom;
					Vector3 absMax = ladder.Top;

					Vector3 left = new(0, 0, 0), right = new(0, 0, 0), up = new(0, 0, 0);
					MathLib.VectorVectors(ladder.GetNormal(), out right, out up);
					right *= ladder.Width * 0.5f;
					left = -right;

					absMin.X += Math.Min(left.X, right.X);
					absMin.Y += Math.Min(left.Y, right.Y);

					absMax.X += Math.Max(left.X, right.X);
					absMax.Y += Math.Max(left.Y, right.Y);

					Extent e;
					e.Lo = absMin + new Vector3(-5, -5, -5);
					e.Hi = absMax + new Vector3(5, 5, 5);

					if (e.Contains(EditCursorPos)) {
						SelectedLadder = ladder;
						break;
					}

					if (!ClimbableSurface)
						continue;

					Vector3 p1 = (ladder.Bottom + ladder.Top) / 2;
					Vector3 p2 = EditCursorPos;
					float distSqr = p1.DistToSqr(p2);

					if (distSqr < closestDistSqr) {
						SelectedLadder = ladder;
						closestDistSqr = distSqr;
					}
				}
			}

			EditCursorPos = result.EndPos;

			if (!ClimbableSurface && SelectedLadder == null) {
				FindNavAreaOrLadderAlongRay(result.StartPos, result.EndPos + 100.0f * dir, out SelectedArea, out SelectedLadder);

				if (SelectedArea == null && SelectedLadder == null)
					SelectedArea = GetNearestNavArea(result.EndPos, false, 500.0f);
			}

			if (SelectedArea != null) {
				float yaw = player.EyeAngles().Y;
				while (yaw > 360.0f)
					yaw -= 360.0f;

				while (yaw < 0.0f)
					yaw += 360.0f;

				if (yaw < 45.0f || yaw > 315.0f || (yaw > 135.0f && yaw < 225.0f)) {
					SplitEdge = SnapToGrid(result.EndPos.Y, true);
					SplitAlongX = true;
				}
				else {
					SplitEdge = SnapToGrid(result.EndPos.X, true);
					SplitAlongX = false;
				}
			}

			if (!ClimbableSurface && !IsEditMode(EditModeType.CreatingLadder))
				EditCursorPos = SnapToGrid(EditCursorPos);

			return true;
		}
		else if (isClippingRayAtFeet) {
			EditCursorPos = SnapToGrid(result.EndPos);
		}

		if (IsEditMode(EditModeType.CreatingLadder) || IsEditMode(EditModeType.CreatingArea))
			return false;

		FindNavAreaOrLadderAlongRay(from, to, out SelectedArea, out SelectedLadder);

		return SelectedArea != null || SelectedLadder != null || isClippingRayAtFeet;
	}

	public bool FindLadderCorners(out Vector3 corner1, out Vector3 corner2, out Vector3 corner3) {
		corner1 = default;
		corner2 = default;
		corner3 = default;

		MathLib.VectorVectors(LadderNormal, out Vector3 ladderRight, out Vector3 ladderUp);
		GetEditVectors(out Vector3 from, out Vector3 dir);

		const float maxDist = 100000f;

		Source.Common.Ray ray = new();
		ray.Init(from, from + dir * maxDist);

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


	public void CommandNavBuildLadder() { }

	void OnEditModeStart() {
		ClearSelectedSet();
		ContinuouslySelecting = false;
		ContinuouslyDeselecting = false;
	}

	void OnEditModeEnd() { }

	void UpdateDragSelectionSet() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		Extent dragArea = default;
		int xmin = (int)Math.Min(Anchor.X, EditCursorPos.X);
		int xmax = (int)Math.Max(Anchor.X, EditCursorPos.X);
		int ymin = (int)Math.Min(Anchor.Y, EditCursorPos.Y);
		int ymax = (int)Math.Max(Anchor.Y, EditCursorPos.Y);

		dragArea.Lo = new Vector3(xmin, ymin, Anchor.Z);
		dragArea.Hi = new Vector3(xmax, ymax, Anchor.Z);

		ClearDragSelectionSet();

		AddToDragSet add = new(dragArea, (int)Anchor.Z - DragSelectionVolumeZMin, (int)Anchor.Z + DragSelectionVolumeZMax, IsDragDeselecting);
		ForAllAreas(add.Invoke);
	}

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

		if (nav_show_nodes.GetBool()) {
			for (NavNode? node = NavNode.GetFirst(); node != null; node = node.GetNext()) {
				if (EditCursorPos.DistToSqr(node.GetPosition()) < 150 * 150)
					node.Draw();
			}
		}

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
					AddDirectionVector(ref pos, NavDirType.North, offset);
					DebugOverlay.Text(pos, "N", false, DebugOverlay.Persist);

					pos = EditCursorPos;
					AddDirectionVector(ref pos, NavDirType.South, offset);
					DebugOverlay.Text(pos, "S", false, DebugOverlay.Persist);

					pos = EditCursorPos;
					AddDirectionVector(ref pos, NavDirType.East, offset);
					DebugOverlay.Text(pos, "E", false, DebugOverlay.Persist);

					pos = EditCursorPos;
					AddDirectionVector(ref pos, NavDirType.West, offset);
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
						NavAttributeType attributes = SelectedArea.GetAttributes();
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
						if (SelectedArea.IsBlocked(Constants.TEAM_ANY)) strcat(attrib, "BLOCKED ");
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

					NavPathfind.SearchSurroundingAreas(nearest, nearest.GetCenter(), draw.Invoke, -1, SearchFlags.IncludeIncomingConnections | SearchFlags.IncludeBlockedAreas);
				}
			}
		}
	}

	void SetMarkedLadder(NavLadder ladder) {
		MarkedArea = null;
		MarkedLadder = ladder;
		MarkedCorner = NavCornerType.NumCorners;
	}

	void SetMarkedArea(NavArea? area) {
		MarkedLadder = null;
		MarkedArea = area;
		MarkedCorner = NavCornerType.NumCorners;
	}

	public uint GetNavPlace() => NavPlace;
	public void SetNavPlace(uint place) => NavPlace = place;

	public void CommandNavDelete() { }

	public void CommandNavDeleteMarked() { }

	public void CommandNavFloodSelect(in TokenizedCommand args) { }

	public void CommandNavToggleSelectedSet() { }

	public void CommandNavStoreSelectedSet() { }

	public void CommandNavRecallSelectedSet() { }

	public void CommandNavAddToSelectedSet() { }

	public void CommandNavAddToSelectedSetByID(in TokenizedCommand args) { }

	public void CommandNavRemoveFromSelectedSet() { }

	public void CommandNavToggleInSelectedSet() { }

	public void CommandNavClearSelectedSet() { }

	public void CommandNavBeginSelecting() { }

	public void CommandNavEndSelecting() { }

	public void CommandNavBeginDragSelecting() { }

	public void CommandNavEndDragSelecting() { }

	public void CommandNavBeginDragDeselecting() { }

	public void CommandNavEndDragDeselecting() { }

	public void CommandNavRaiseDragVolumeMax() { }

	public void CommandNavLowerDragVolumeMax() { }

	public void CommandNavRaiseDragVolumeMin() { }

	public void CommandNavLowerDragVolumeMin() { }

	public void CommandNavToggleSelecting(bool playSound = true) { }

	public void CommandNavBeginDeselecting() { }

	public void CommandNavEndDeselecting() { }

	public void CommandNavToggleDeselecting(bool playSound = true) { }

	public void CommandNavSelectHalfSpace(in TokenizedCommand args) { }

	public void CommandNavBeginShiftXY() { }

	public void CommandNavEndShiftXY() { }

	public void CommandNavSelectInvalidAreas() { }

	public void CommandNavSelectBlockedAreas() { }

	public void CommandNavSelectObstructedAreas() { }

	public void CommandNavSelectDamagingAreas() { }

	public void CommandNavSelectStairs() { }

	public void CommandNavSelectOrphans() { }

	public void CommandNavSplit() { }

	public void CommandNavMakeSniperSpots() { }

	public void CommandNavMerge() { }

	public void CommandNavMark(in TokenizedCommand args) { }

	public void CommandNavUnmark() { }

	public void CommandNavBeginArea() { }

	public void CommandNavEndArea() { }

	public void CommandNavConnect() { }

	public void CommandNavDisconnect() { }

	public void CommandNavDisconnectOutgoingOneWays() { }

	public void CommandNavSplice() { }

	void DoToggleAttribute(NavArea area, NavAttributeType attribute) {
		area.SetAttributes(area.GetAttributes() ^ attribute);

		if (attribute == NavAttributeType.Transient) {
			if ((area.GetAttributes() & NavAttributeType.Transient) != 0)
				TransientAreas.Add(area);
			else
				TransientAreas.Remove(area);
		}
	}

	public void CommandNavToggleAttribute(NavAttributeType attribute) { }

	public void CommandNavTogglePlaceMode() { }

	public void CommandNavPlaceFloodFill() { }

	public void CommandNavPlaceSet() { }

	public void CommandNavPlacePick() { }

	public void CommandNavTogglePlacePainting() { }

	public void CommandNavMarkUnnamed() { }

	public void CommandNavCornerSelect() { }

	public void CommandNavCornerRaise(in TokenizedCommand args) { }

	public void CommandNavCornerLower(in TokenizedCommand args) { }

	public void CommandNavCornerPlaceOnGround(in TokenizedCommand args) { }

	public void CommandNavWarpToMark() { }

	public void CommandNavLadderFlip() { }

	public void AddToSelectedSet(NavArea area) { }

	public void RemoveFromSelectedSet(NavArea area) => SelectedSet.Remove(area);

	void AddToDragSelectionSet(NavArea area) { }

	void RemoveFromDragSelectionSet(NavArea area) => DragSelectionSet.Remove(area);

	void ClearDragSelectionSet() => DragSelectionSet.Clear();

	void ClearSelectedSet() => SelectedSet.Clear();

	bool IsSelectedSetEmpty() => SelectedSet.Count == 0;

	int GetSelectedSetSize() => SelectedSet.Count;

	public List<NavArea> GetSelectedSet() => SelectedSet;

	public bool IsInSelectedSet(NavArea area) => SelectedSet.Contains(area);

	public void OnEditCreateNotify(NavArea newArea) { }

	public void OnEditDestroyNotify(NavArea deadArea) { }

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

		return Count < nav_draw_limit.GetInt();
	}
}

class AddToDragSet(Extent area, int zMin, int zMax, bool dragDeselecting)
{
	Extent DragArea = area;
	int ZMin = zMin - 1;
	int ZMax = zMax + 1;
	bool DragDeselecting = dragDeselecting;

	public bool Invoke(NavArea area) {
		bool shouldBeInSelectedSet = DragDeselecting;

		if ((NavMesh.Instance!.IsInSelectedSet(area) == shouldBeInSelectedSet) && area.IsOverlapping(DragArea) && area.GetCenter().Z >= ZMin && area.GetCenter().Z <= ZMax)
			NavMesh.Instance.AddToSelectedSet(area);

		return true;
	}
}

public class SelectCollector(int count)
{
	public int Count = count;

	public bool Invoke(NavArea area) {
		if (NavMesh.Instance!.IsInSelectedSet(area))
			return false;

		NavMesh.Instance.AddToSelectedSet(area);
		++Count;

		return true;
	}
}