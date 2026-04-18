using static Game.Server.NavMesh.Nav;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Mathematics;

using System.Numerics;
using Source.Common.Formats.BSP;

namespace Game.Server.NavMesh;

enum HalfSpaceType
{
	PlusX,
	MinusX,
	PlusY,
	MinusY,
	PlusZ,
	MinusZ
}

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

	static void StepAlongClimbableSurface(ref Vector3 pos, Vector3 increment, Vector3 probe) {
		while (CheckForClimbableSurface(pos + increment - probe, pos + increment + probe))
			pos += increment;
	}

	static bool CheckForClimbableSurface(Vector3 start, Vector3 end) {
		Util.TraceLine(start, end, Mask.NPCSolidBrushOnly, null, CollisionGroup.None, out Trace result);

		bool climbableSurface = false;
		if (result.Fraction != 1.0f) {
			climbableSurface = physprops.GetSurfaceData(result.Surface.SurfaceProps)?.Game.Climbable != 0;
			if (!climbableSurface)
				climbableSurface = (result.Contents & Contents.Ladder) != 0;
		}

		return climbableSurface;
	}

	public void CommandNavBuildLadder() {
		if (!IsEditMode(EditModeType.Normal) || !ClimbableSurface)
			return;

		MathLib.VectorVectors(-SurfaceNormal, out Vector3 right, out Vector3 up);

		LadderNormal = SurfaceNormal;

		Vector3 startPos = EditCursorPos;

		Vector3 leftEdge = startPos;
		Vector3 rightEdge = startPos;

		Vector3 probe = SurfaceNormal * -HalfHumanWidth;
		const float StepSize = 1.0f;
		StepAlongClimbableSurface(ref leftEdge, right * -StepSize, probe);
		StepAlongClimbableSurface(ref rightEdge, right * StepSize, probe);

		Vector3 topEdge = (leftEdge + rightEdge) * 0.5f;
		Vector3 bottomEdge = topEdge;
		StepAlongClimbableSurface(ref topEdge, up * StepSize, probe);
		StepAlongClimbableSurface(ref bottomEdge, up * -StepSize, probe);

		Vector3 top = (leftEdge + rightEdge) * 0.5f;
		top.Z = topEdge.Z;

		Vector3 bottom = top;
		bottom.Z = bottomEdge.Z;

		CreateLadder(topEdge, bottomEdge, leftEdge.DistTo(rightEdge), LadderNormal.AsVector2D(), 0.0f);
	}

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
							player.EmitSound("Bot.EditSwitchOn");
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
					foreach (NavArea area in SelectedSet)
						draw.Invoke(area);
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

					NavPathfind.SearchSurroundingAreas(nearest, nearest!.GetCenter(), draw.Invoke, -1, SearchFlags.IncludeIncomingConnections | SearchFlags.IncludeBlockedAreas);
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

	public void CommandNavDelete() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		if (IsSelectedSetEmpty()) {
			NavArea? markedArea = GetMarkedArea();
			NavLadder? markedLadder = GetMarkedLadder();
			FindActiveNavArea();

			if (markedArea != null) {
				player.EmitSound("EDIT_DELETE");
				NavArea.TheNavAreas.Remove(markedArea);
				OnEditDestroyNotify(markedArea);
				DestroyArea(markedArea);
			}
			else if (markedLadder != null) {
				player.EmitSound("EDIT_DELETE");
				Ladders.Remove(markedLadder);
				OnEditDestroyNotify(markedLadder);
			}
			else if (SelectedArea != null) {
				player.EmitSound("EDIT_DELETE");
				NavArea.TheNavAreas.Remove(SelectedArea);
				NavArea deadArea = SelectedArea;
				OnEditDestroyNotify(deadArea);
				DestroyArea(deadArea);
			}
			else if (SelectedLadder != null) {
				player.EmitSound("EDIT_DELETE");
				Ladders.Remove(SelectedLadder);
				NavLadder deadLadder = SelectedLadder;
				OnEditDestroyNotify(deadLadder);
			}
		}
		else {
			player.EmitSound("EDIT_DELETE");

			foreach (NavArea area in SelectedSet) {
				NavArea.TheNavAreas.Remove(area);
				OnEditDestroyNotify(area);
				DestroyArea(area);
			}

			Msg($"Deleted {SelectedSet.Count} areas\n");

			ClearSelectedSet();
		}

		StripNavigationAreas();

		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavDeleteMarked() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		NavArea? markedArea = GetMarkedArea();
		if (markedArea != null) {
			player.EmitSound("EDIT_DELETE");
			OnEditDestroyNotify(markedArea);
			NavArea.TheNavAreas.Remove(markedArea);
			DestroyArea(markedArea);
		}

		NavLadder? markedLadder = GetMarkedLadder();
		if (markedLadder != null) {
			player.EmitSound("EDIT_DELETE");
			Ladders.Remove(markedLadder);
		}

		StripNavigationAreas();

		ClearSelectedSet();

		SetMarkedArea(null);
		SetMarkedLadder(null!);
		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavFloodSelect(in TokenizedCommand args) {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting))
			return;

		FindActiveNavArea();

		NavArea? start = SelectedArea;
		if (start == null)
			start = MarkedArea;

		if (start != null) {
			player.EmitSound("EDIT_DELETE");

			SearchFlags connections = SearchFlags.IncludeBlockedAreas | SearchFlags.IncludeIncomingConnections;
			if (args.ArgC() == 2 && FStrEq("out", args[1]))
				connections = SearchFlags.IncludeBlockedAreas;
			if (args.ArgC() == 2 && FStrEq("in", args[1]))
				connections = SearchFlags.IncludeBlockedAreas | SearchFlags.IncludeIncomingConnections | SearchFlags.ExcludeOutgoingConnections;

			SelectCollector collector = new(0);
			NavPathfind.SearchSurroundingAreas(start, start.GetCenter(), collector.Invoke, -1, connections);

			Msg($"Selected {collector.Count} areas.\n");
		}

		SetMarkedArea(null);
	}

	public void CommandNavToggleSelectedSet() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting))
			return;

		player.EmitSound("EDIT_DELETE");

		List<NavArea> notInSelectedSet = [];

		foreach (NavArea area in NavArea.TheNavAreas) {
			if (!IsInSelectedSet(area))
				notInSelectedSet.Add(area);
		}

		ClearSelectedSet();

		foreach (NavArea area in notInSelectedSet)
			AddToSelectedSet(area);

		Msg($"Selected {notInSelectedSet.Count} areas.\n");

		SetMarkedArea(null);
	}

	public void CommandNavStoreSelectedSet() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting))
			return;

		player.EmitSound("EDIT_DELETE");

		StoredSelectedSet.Clear();
		foreach (NavArea area in SelectedSet) {
			StoredSelectedSet.Add((int)area.GetID());
		}
	}

	public void CommandNavRecallSelectedSet() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting))
			return;

		player.EmitSound("EDIT_DELETE");

		ClearSelectedSet();

		for (int i = 0; i < StoredSelectedSet.Count; ++i) {
			NavArea? area = GetNavAreaByID((uint)StoredSelectedSet[i]);
			if (area != null)
				AddToSelectedSet(area);
		}

		Msg($"Selected {SelectedSet.Count} areas.\n");
	}

	public void CommandNavAddToSelectedSet() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting))
			return;

		FindActiveNavArea();

		if (SelectedArea != null) {
			AddToSelectedSet(SelectedArea);
			player.EmitSound("EDIT_MARK.Enable");
		}
	}

	public void CommandNavAddToSelectedSetByID(in TokenizedCommand args) {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if ((!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting)) || args.ArgC() < 2)
			return;

		int id = args.Arg(1, 0);
		NavArea? area = GetNavAreaByID((uint)id);
		if (area != null) {
			AddToSelectedSet(area);
			player.EmitSound("EDIT_MARK.Enable");
			Msg($"Added area {id}.  ( to go there: setpos {area.GetCenter().X} {area.GetCenter().Y} {area.GetCenter().Z + 5} )\n");
		}
		else
			Msg($"No area with id {id}\n");
	}

	public void CommandNavRemoveFromSelectedSet() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting))
			return;

		FindActiveNavArea();

		if (SelectedArea != null) {
			RemoveFromSelectedSet(SelectedArea);
			player.EmitSound("EDIT_MARK.Disable");
		}
	}

	public void CommandNavToggleInSelectedSet() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting))
			return;

		FindActiveNavArea();

		if (SelectedArea != null) {
			if (IsInSelectedSet(SelectedArea))
				RemoveFromSelectedSet(SelectedArea);
			else
				AddToSelectedSet(SelectedArea);
			player.EmitSound("EDIT_MARK.Disable");
		}
	}

	public void CommandNavClearSelectedSet() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting))
			return;

		ClearSelectedSet();
		player.EmitSound("EDIT_MARK.Disable");
	}

	public void CommandNavBeginSelecting() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting))
			return;

		ContinuouslySelecting = true;
		ContinuouslyDeselecting = false;

		player.EmitSound("EDIT_BEGIN_AREA.Creating");
	}

	public void CommandNavEndSelecting() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting))
			return;

		ContinuouslySelecting = false;
		ContinuouslyDeselecting = false;

		player.EmitSound("EDIT_END_AREA.Creating");
	}

	public void CommandNavBeginDragSelecting() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting) && !IsEditMode(EditModeType.DragSelecting))
			return;

		FindActiveNavArea();

		if (IsEditMode(EditModeType.DragSelecting)) {
			ClearDragSelectionSet();
			SetEditMode(EditModeType.Normal);
			player.EmitSound("EDIT_BEGIN_AREA.NotCreating");
		}
		else {
			player.EmitSound("EDIT_BEGIN_AREA.NotCreating");

			SetEditMode(EditModeType.DragSelecting);

			Anchor = EditCursorPos;
			DragSelectionVolumeZMax = nav_drag_selection_volume_zmax_offset.GetInt();
			DragSelectionVolumeZMin = nav_drag_selection_volume_zmin_offset.GetInt();
		}

		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavEndDragSelecting() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (IsEditMode(EditModeType.DragSelecting)) {
			foreach (NavArea area in DragSelectionSet)
				AddToSelectedSet(area);
			SetEditMode(EditModeType.Normal);
		}
		else
			player.EmitSound("EDIT_END_AREA.NotCreating");

		ClearDragSelectionSet();
		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavBeginDragDeselecting() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting) && !IsEditMode(EditModeType.DragSelecting))
			return;

		FindActiveNavArea();

		if (IsEditMode(EditModeType.DragSelecting)) {
			ClearDragSelectionSet();
			SetEditMode(EditModeType.Normal);
			player.EmitSound("EDIT_BEGIN_AREA.NotCreating");
		}
		else {
			player.EmitSound("EDIT_BEGIN_AREA.NotCreating");

			SetEditMode(EditModeType.DragSelecting);
			IsDragDeselecting = true;

			Anchor = EditCursorPos;
			DragSelectionVolumeZMax = nav_drag_selection_volume_zmax_offset.GetInt();
			DragSelectionVolumeZMin = nav_drag_selection_volume_zmin_offset.GetInt();
		}

		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavEndDragDeselecting() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (IsEditMode(EditModeType.DragSelecting)) {
			foreach (NavArea area in DragSelectionSet)
				RemoveFromSelectedSet(area);
			SetEditMode(EditModeType.Normal);
		}
		else
			player.EmitSound("EDIT_END_AREA.NotCreating");

		ClearDragSelectionSet();
		IsDragDeselecting = false;
		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavRaiseDragVolumeMax() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		DragSelectionVolumeZMax += 32;
		nav_drag_selection_volume_zmax_offset.SetValue(DragSelectionVolumeZMax);
	}

	public void CommandNavLowerDragVolumeMax() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		DragSelectionVolumeZMax = Math.Max(0, DragSelectionVolumeZMax - 32);
		nav_drag_selection_volume_zmax_offset.SetValue(DragSelectionVolumeZMax);
	}

	public void CommandNavRaiseDragVolumeMin() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		DragSelectionVolumeZMin = Math.Max(0, DragSelectionVolumeZMin - 32);
		nav_drag_selection_volume_zmin_offset.SetValue(DragSelectionVolumeZMin);
	}

	public void CommandNavLowerDragVolumeMin() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		DragSelectionVolumeZMin += 32;
		nav_drag_selection_volume_zmin_offset.SetValue(DragSelectionVolumeZMin);
	}

	public void CommandNavToggleSelecting(bool playSound = true) {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting))
			return;

		ContinuouslySelecting = !ContinuouslySelecting;
		ContinuouslyDeselecting = false;

		if (playSound)
			player.EmitSound("EDIT_END_AREA.Creating");
	}

	public void CommandNavBeginDeselecting() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting))
			return;

		ContinuouslyDeselecting = true;
		ContinuouslySelecting = false;

		player.EmitSound("EDIT_BEGIN_AREA.Creating");
	}

	public void CommandNavEndDeselecting() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting))
			return;

		ContinuouslyDeselecting = false;
		ContinuouslySelecting = false;

		player.EmitSound("EDIT_END_AREA.Creating");
	}

	public void CommandNavToggleDeselecting(bool playSound = true) {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting))
			return;

		ContinuouslyDeselecting = !ContinuouslyDeselecting;
		ContinuouslySelecting = false;

		if (playSound)
			player.EmitSound("EDIT_END_AREA.Creating");
	}

	public void CommandNavSelectHalfSpace(in TokenizedCommand args) {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting))
			return;

		if (args.ArgC() != 3) {
			Warning("Error:  <+X|-X|+Y|-Y|+Z|-Z> <value>\n");
			return;
		}

		HalfSpaceType halfSpace = HalfSpaceType.PlusX;
		if (FStrEq("+x", args[1]))
			halfSpace = HalfSpaceType.PlusX;
		else if (FStrEq("-x", args[1]))
			halfSpace = HalfSpaceType.MinusX;
		else if (FStrEq("+y", args[1]))
			halfSpace = HalfSpaceType.PlusY;
		else if (FStrEq("-y", args[1]))
			halfSpace = HalfSpaceType.MinusY;
		else if (FStrEq("+z", args[1]))
			halfSpace = HalfSpaceType.PlusZ;
		else if (FStrEq("-z", args[1]))
			halfSpace = HalfSpaceType.MinusZ;

		float value = args.Arg(2, 0.0f);

		Extent extent = default;
		foreach (NavArea area in NavArea.TheNavAreas) {
			area.GetExtent(ref extent);

			switch (halfSpace) {
				case HalfSpaceType.PlusX:
					if (extent.Lo.X < value && extent.Hi.X < value)
						continue;
					break;

				case HalfSpaceType.PlusY:
					if (extent.Lo.Y < value && extent.Hi.Y < value)
						continue;
					break;

				case HalfSpaceType.PlusZ:
					if (extent.Lo.Z < value && extent.Hi.Z < value)
						continue;
					break;

				case HalfSpaceType.MinusX:
					if (extent.Lo.X > value && extent.Hi.X > value)
						continue;
					break;

				case HalfSpaceType.MinusY:
					if (extent.Lo.Y > value && extent.Hi.Y > value)
						continue;
					break;

				case HalfSpaceType.MinusZ:
					if (extent.Lo.Z > value && extent.Hi.Z > value)
						continue;
					break;
			}

			if (IsInSelectedSet(area))
				RemoveFromSelectedSet(area);
			else
				AddToSelectedSet(area);
		}

		player.EmitSound("EDIT_DELETE");
	}

	public void CommandNavBeginShiftXY() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (GetEditMode() == EditModeType.ShiftingXY) {
			SetEditMode(EditModeType.Normal);
			player.EmitSound("EDIT_END_AREA.Creating");
			return;
		}
		else {
			SetEditMode(EditModeType.ShiftingXY);
			player.EmitSound("EDIT_BEGIN_AREA.Creating");
		}

		Anchor = EditCursorPos;
	}

	public void CommandNavEndShiftXY() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		SetEditMode(EditModeType.Normal);

		Vector3 shiftAmount = EditCursorPos - Anchor;
		shiftAmount.Z = 0.0f;

		ShiftSet shift = new(shiftAmount);

		ForAllSelectedAreas(shift.Invoke);

		player.EmitSound("EDIT_END_AREA.Creating");
	}

	[ConCommand("nav_shift", "Shifts the selected areas by the specified amount", FCvar.Cheat)]
	static void nav_shift(in TokenizedCommand args) {
		throw new NotImplementedException();
	}

	public void CommandNavSelectInvalidAreas() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		ClearSelectedSet();

		Extent areaExtent = default;
		foreach (NavArea area in NavArea.TheNavAreas) {
			if (area != null) {
				area.GetExtent(ref areaExtent);
				for (float x = areaExtent.Lo.X; x + GenerationStepSize <= areaExtent.Hi.X; x += GenerationStepSize) {
					for (float y = areaExtent.Lo.Y; y + GenerationStepSize <= areaExtent.Hi.Y; y += GenerationStepSize) {
						float nw = area.GetZ(x, y);
						float ne = area.GetZ(x + GenerationStepSize, y);
						float sw = area.GetZ(x, y + GenerationStepSize);
						float se = area.GetZ(x + GenerationStepSize, y + GenerationStepSize);

						if (!IsHeightDifferenceValid(nw, ne, sw, se) ||
								!IsHeightDifferenceValid(ne, nw, sw, se) ||
								!IsHeightDifferenceValid(sw, ne, nw, se) ||
								!IsHeightDifferenceValid(se, ne, sw, nw)) {
							AddToSelectedSet(area);
						}
					}
				}
			}
		}

		Msg($"Selected {SelectedSet.Count} areas.\n");

		if (SelectedSet.Count > 0)
			player.EmitSound("EDIT_MARK.Enable");
		else
			player.EmitSound("EDIT_MARK.Disable");
	}

	public void CommandNavSelectBlockedAreas() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		ClearSelectedSet();

		foreach (NavArea area in NavArea.TheNavAreas) {
			if (area != null && area.IsBlocked(Constants.TEAM_ANY))
				AddToSelectedSet(area);
		}

		Msg($"Selected {SelectedSet.Count} areas.\n");

		if (SelectedSet.Count > 0)
			player.EmitSound("EDIT_MARK.Enable");
		else
			player.EmitSound("EDIT_MARK.Disable");
	}

	public void CommandNavSelectObstructedAreas() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		ClearSelectedSet();

		foreach (NavArea area in NavArea.TheNavAreas) {
			if (area != null && area.HasAvoidanceObstacle())
				AddToSelectedSet(area);
		}

		Msg($"Selected {SelectedSet.Count} areas.\n");

		if (SelectedSet.Count > 0)
			player.EmitSound("EDIT_MARK.Enable");
		else
			player.EmitSound("EDIT_MARK.Disable");
	}

	public void CommandNavSelectDamagingAreas() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		ClearSelectedSet();

		foreach (NavArea area in NavArea.TheNavAreas) {
			if (area != null && area.IsDamaging())
				AddToSelectedSet(area);
		}

		Msg($"Selected {SelectedSet.Count} areas.\n");

		if (SelectedSet.Count > 0)
			player.EmitSound("EDIT_MARK.Enable");
		else
			player.EmitSound("EDIT_MARK.Disable");
	}

	public void CommandNavSelectStairs() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		ClearSelectedSet();

		foreach (NavArea area in NavArea.TheNavAreas) {
			if (area != null && area.HasAttributes(NavAttributeType.Stairs))
				AddToSelectedSet(area);
		}

		Msg($"Selected {SelectedSet.Count} areas.\n");

		if (SelectedSet.Count > 0)
			player.EmitSound("EDIT_MARK.Enable");
		else
			player.EmitSound("EDIT_MARK.Disable");
	}

	public void CommandNavSelectOrphans() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal) && !IsEditMode(EditModeType.PlacePainting))
			return;

		FindActiveNavArea();

		NavArea? start = SelectedArea;
		if (start == null)
			start = MarkedArea;

		if (start != null) {
			player.EmitSound("EDIT_DELETE");

			SearchFlags connections = SearchFlags.IncludeBlockedAreas | SearchFlags.IncludeIncomingConnections;

			SelectCollector collector = new(0);
			NavPathfind.SearchSurroundingAreas(start, start.GetCenter(), collector.Invoke, -1, connections);

			CommandNavToggleSelectedSet();
		}

		SetMarkedArea(null);
	}

	public void CommandNavSplit() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		FindActiveNavArea();

		if (SelectedArea != null) {
			if (SelectedArea.SplitEdit(SplitAlongX, SplitEdge, out _, out _))
				player.EmitSound("EDIT_SPLIT.MarkedArea");
			else
				player.EmitSound("EDIT_SPLIT.NoMarkedArea");
		}

		StripNavigationAreas();

		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
	}

	static bool MakeSniperSpots(NavArea area) {
		if (area == null)
			return false;

		bool splitAlongX;
		float splitEdge;

		const float minSplitSize = 2.0f;

		float sizeX = area.GetSizeX();
		float sizeY = area.GetSizeY();

		if (sizeX > GenerationStepSize && sizeX > sizeY) {
			splitEdge = RoundToUnits(area.GetCorner(NavCornerType.NorthWest).X, GenerationStepSize);
			if (splitEdge < area.GetCorner(NavCornerType.NorthWest).X + minSplitSize)
				splitEdge += GenerationStepSize;
			splitAlongX = false;
		}
		else if (sizeY > GenerationStepSize && sizeY > sizeX) {
			splitEdge = RoundToUnits(area.GetCorner(NavCornerType.NorthWest).Y, GenerationStepSize);
			if (splitEdge < area.GetCorner(NavCornerType.NorthWest).Y + minSplitSize)
				splitEdge += GenerationStepSize;
			splitAlongX = true;
		}
		else
			return false;

		if (!area.SplitEdit(splitAlongX, splitEdge, out NavArea? first, out NavArea? second))
			return false;

		first!.Disconnect(second!);
		second!.Disconnect(first);

		MakeSniperSpots(first);
		MakeSniperSpots(second);

		return true;
	}

	public void CommandNavMakeSniperSpots() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		FindActiveNavArea();

		if (SelectedArea != null) {
			if (MakeSniperSpots(SelectedArea))
				player.EmitSound("EDIT_SPLIT.MarkedArea");
			else
				player.EmitSound("EDIT_SPLIT.NoMarkedArea");
		}
		else
			player.EmitSound("EDIT_SPLIT.NoMarkedArea");

		StripNavigationAreas();

		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavMerge() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		FindActiveNavArea();

		if (SelectedArea != null) {
			NavArea? other = MarkedArea;
			if (MarkedArea == null && SelectedSet.Count == 1)
				other = SelectedSet[0];

			if (other != null && other != SelectedArea) {
				if (SelectedArea.MergeEdit(other))
					player.EmitSound("EDIT_MERGE.Enable");
				else
					player.EmitSound("EDIT_MERGE.Disable");
			}
			else {
				Msg("To merge, mark an area, highlight a second area, then invoke the merge command");
				player.EmitSound("EDIT_MERGE.Disable");
			}
		}

		StripNavigationAreas();

		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
		ClearSelectedSet();
	}

	public void CommandNavMark(in TokenizedCommand args) {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		if (!IsSelectedSetEmpty()) {
			if (IsInSelectedSet(SelectedArea!)) {
				player.EmitSound("EDIT_MARK.Disable");
				RemoveFromSelectedSet(SelectedArea!);
			}
			else {
				player.EmitSound("EDIT_MARK.Enable");
				AddToSelectedSet(SelectedArea!);
			}
			return;
		}

		FindActiveNavArea();

		if (MarkedArea != null || MarkedLadder != null) {
			player.EmitSound("EDIT_MARK.Enable");
			Msg("Area unmarked.\n");
			SetMarkedArea(null);
		}
		else if (args.ArgC() > 1) {
			if (FStrEq(args[1], "ladder")) {
				if (args.ArgC() > 2) {
					ReadOnlySpan<char> ladderIDNameToMark = args[2];
					if (!ladderIDNameToMark.IsEmpty) {
						uint ladderIDToMark = (uint)args.Arg(2, 0);
						if (ladderIDToMark != 0) {
							NavLadder? ladder = GetLadderByID(ladderIDToMark);
							if (ladder != null) {
								player.EmitSound("EDIT_MARK.Disable");
								SetMarkedLadder(ladder);

								int connected = 0;
								connected += MarkedLadder!.TopForwardArea != null ? 1 : 0;
								connected += MarkedLadder.TopLeftArea != null ? 1 : 0;
								connected += MarkedLadder.TopRightArea != null ? 1 : 0;
								connected += MarkedLadder.TopBehindArea != null ? 1 : 0;
								connected += MarkedLadder.BottomArea != null ? 1 : 0;

								Msg($"Marked Ladder is connected to {connected} Areas\n");
							}
						}
					}
				}
			}
			else {
				ReadOnlySpan<char> areaIDNameToMark = args[1];
				if (!areaIDNameToMark.IsEmpty) {
					uint areaIDToMark = (uint)args.Arg(1, 0);
					if (areaIDToMark != 0) {
						NavArea? areaToMark = null;
						foreach (NavArea area in NavArea.TheNavAreas) {
							if (area.GetID() == areaIDToMark) {
								areaToMark = area;
								break;
							}
						}
						if (areaToMark != null) {
							player.EmitSound("EDIT_MARK.Disable");
							SetMarkedArea(areaToMark);

							int connected = 0;
							connected += GetMarkedArea()!.GetAdjacentCount(NavDirType.North);
							connected += GetMarkedArea()!.GetAdjacentCount(NavDirType.South);
							connected += GetMarkedArea()!.GetAdjacentCount(NavDirType.East);
							connected += GetMarkedArea()!.GetAdjacentCount(NavDirType.West);

							Msg($"Marked Area is connected to {connected} other Areas\n");
						}
					}
				}
			}
		}
		else if (SelectedArea != null) {
			player.EmitSound("EDIT_MARK.Disable");
			SetMarkedArea(SelectedArea);

			int connected = 0;
			connected += GetMarkedArea()!.GetAdjacentCount(NavDirType.North);
			connected += GetMarkedArea()!.GetAdjacentCount(NavDirType.South);
			connected += GetMarkedArea()!.GetAdjacentCount(NavDirType.East);
			connected += GetMarkedArea()!.GetAdjacentCount(NavDirType.West);

			Msg($"Marked Area is connected to {connected} other Areas\n");
		}
		else if (SelectedLadder != null) {
			player.EmitSound("EDIT_MARK.Disable");
			SetMarkedLadder(SelectedLadder);

			int connected = 0;
			connected += MarkedLadder!.TopForwardArea != null ? 1 : 0;
			connected += MarkedLadder.TopLeftArea != null ? 1 : 0;
			connected += MarkedLadder.TopRightArea != null ? 1 : 0;
			connected += MarkedLadder.TopBehindArea != null ? 1 : 0;
			connected += MarkedLadder.BottomArea != null ? 1 : 0;

			Msg($"Marked Ladder is connected to {connected} Areas\n");
		}

		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavUnmark() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		player.EmitSound("EDIT_MARK.Enable");
		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavBeginArea() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!(IsEditMode(EditModeType.CreatingArea) || IsEditMode(EditModeType.CreatingLadder) || IsEditMode(EditModeType.Normal))) {
			player.EmitSound("EDIT_END_AREA.NotCreating");
			return;
		}

		FindActiveNavArea();

		if (IsEditMode(EditModeType.CreatingArea)) {
			SetEditMode(EditModeType.Normal);
			player.EmitSound("EDIT_BEGIN_AREA.Creating");
		}
		else if (IsEditMode(EditModeType.CreatingLadder)) {
			SetEditMode(EditModeType.Normal);
			player.EmitSound("EDIT_BEGIN_AREA.Creating");
		}
		else if (ClimbableSurface) {
			player.EmitSound("EDIT_BEGIN_AREA.NotCreating");

			SetEditMode(EditModeType.CreatingLadder);

			LadderAnchor = EditCursorPos;
			LadderNormal = SurfaceNormal;
		}
		else {
			player.EmitSound("EDIT_BEGIN_AREA.NotCreating");

			SetEditMode(EditModeType.CreatingArea);

			Anchor = EditCursorPos;
		}

		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavEndArea() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!(IsEditMode(EditModeType.CreatingArea) || IsEditMode(EditModeType.CreatingLadder) || IsEditMode(EditModeType.Normal))) {
			player.EmitSound("EDIT_END_AREA.NotCreating");
			return;
		}

		if (IsEditMode(EditModeType.CreatingArea)) {
			SetEditMode(EditModeType.Normal);

			Vector3 endPos = EditCursorPos;
			endPos.Z = Anchor.Z;

			NavArea? nearby = GetMarkedArea();
			nearby ??= GetNearestNavArea(EditCursorPos + new Vector3(0, 0, HalfHumanHeight), false, 10000.0f, true);
			nearby ??= GetNearestNavArea(endPos + new Vector3(0, 0, HalfHumanHeight), false, 10000.0f, true);
			nearby ??= GetNearestNavArea(EditCursorPos);
			nearby ??= GetNearestNavArea(endPos);

			NavArea newArea = CreateArea();
			newArea.Build(Anchor, endPos);

			if (nearby != null)
				newArea.InheritAttributes(nearby);

			NavArea.TheNavAreas.Add(newArea);
			AddNavArea(newArea);
			player.EmitSound("EDIT_END_AREA.Creating");

			if (nav_create_place_on_ground.GetBool())
				newArea.PlaceOnGround(NavCornerType.NumCorners);

			if (GetMarkedArea() != null) {
				Extent extent = default;
				GetMarkedArea()!.GetExtent(ref extent);

				if (Anchor.X > extent.Hi.X && EditCursorPos.X > extent.Hi.X) {
					GetMarkedArea()!.ConnectTo(newArea, NavDirType.East);
					newArea.ConnectTo(GetMarkedArea()!, NavDirType.West);
				}
				else if (Anchor.X < extent.Lo.X && EditCursorPos.X < extent.Lo.X) {
					GetMarkedArea()!.ConnectTo(newArea, NavDirType.West);
					newArea.ConnectTo(GetMarkedArea()!, NavDirType.East);
				}
				else if (Anchor.Y > extent.Hi.Y && EditCursorPos.Y > extent.Hi.Y) {
					GetMarkedArea()!.ConnectTo(newArea, NavDirType.South);
					newArea.ConnectTo(GetMarkedArea()!, NavDirType.North);
				}
				else if (Anchor.Y < extent.Lo.Y && EditCursorPos.Y < extent.Lo.Y) {
					GetMarkedArea()!.ConnectTo(newArea, NavDirType.North);
					newArea.ConnectTo(GetMarkedArea()!, NavDirType.South);
				}

				SetMarkedArea(newArea);
			}

			OnEditCreateNotify(newArea);
		}
		else if (IsEditMode(EditModeType.CreatingLadder)) {
			SetEditMode(EditModeType.Normal);

			player.EmitSound("EDIT_END_AREA.Creating");

			if (ClimbableSurface && FindLadderCorners(out Vector3 corner1, out Vector3 corner2, out Vector3 corner3)) {
				Vector3 top = (LadderAnchor + corner2) * 0.5f;
				Vector3 bottom = (corner1 + corner3) * 0.5f;
				if (top.Z < bottom.Z)
					(bottom, top) = (top, bottom);

				float width = LadderAnchor.DistTo(corner2);
				Vector2 ladderDir = SurfaceNormal.AsVector2D();

				CreateLadder(top, bottom, width, ladderDir, HumanHeight);
			}
			else
				player.EmitSound("EDIT_END_AREA.NotCreating");
		}
		else
			player.EmitSound("EDIT_END_AREA.NotCreating");

		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavConnect() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		FindActiveNavArea();

		Vector3 center;
		float halfWidth;
		if (SelectedSet.Count > 1) {
			bool bValid = true;
			for (int i = 1; i < SelectedSet.Count; ++i) {
				NavArea first = SelectedSet[0];
				NavArea second = SelectedSet[i];

				NavDirType dir = second.ComputeLargestPortal(first, out center, out halfWidth);
				if (dir == NavDirType.NumDirections) {
					player.EmitSound("EDIT_CONNECT.AllDirections");
					bValid = false;
					break;
				}

				dir = first.ComputeLargestPortal(second, out center, out halfWidth);
				if (dir == NavDirType.NumDirections) {
					player.EmitSound("EDIT_CONNECT.AllDirections");
					bValid = false;
					break;
				}
			}

			if (bValid) {
				for (int i = 1; i < SelectedSet.Count; ++i) {
					NavArea first = SelectedSet[0];
					NavArea second = SelectedSet[i];

					NavDirType dir = second.ComputeLargestPortal(first, out center, out halfWidth);
					second.ConnectTo(first, dir);

					dir = first.ComputeLargestPortal(second, out center, out halfWidth);
					first.ConnectTo(second, dir);
					player.EmitSound("EDIT_CONNECT.Added");
				}
			}
		}
		else if (SelectedArea != null) {
			if (MarkedLadder != null) {
				MarkedLadder.ConnectTo(SelectedArea);
				player.EmitSound("EDIT_CONNECT.Added");
			}
			else if (MarkedArea != null) {
				NavDirType dir = GetMarkedArea()!.ComputeLargestPortal(SelectedArea, out center, out halfWidth);
				if (dir == NavDirType.NumDirections)
					player.EmitSound("EDIT_CONNECT.AllDirections");
				else {
					MarkedArea.ConnectTo(SelectedArea, dir);
					player.EmitSound("EDIT_CONNECT.Added");
				}
			}
			else {
				if (SelectedSet.Count == 1) {
					NavArea area = SelectedSet[0];
					NavDirType dir = area.ComputeLargestPortal(SelectedArea, out center, out halfWidth);
					if (dir == NavDirType.NumDirections)
						player.EmitSound("EDIT_CONNECT.AllDirections");
					else {
						area.ConnectTo(SelectedArea, dir);
						player.EmitSound("EDIT_CONNECT.Added");
					}
				}
				else {
					Msg("To connect areas, mark an area, highlight a second area, then invoke the connect command. Make sure the cursor is directly north, south, east, or west of the marked area.");
					player.EmitSound("EDIT_CONNECT.AllDirections");
				}
			}
		}
		else if (SelectedLadder != null) {
			if (MarkedArea != null) {
				MarkedArea.ConnectTo(SelectedLadder);
				player.EmitSound("EDIT_CONNECT.Added");
			}
			else {
				Msg("To connect areas, mark an area, highlight a second area, then invoke the connect command. Make sure the cursor is directly north, south, east, or west of the marked area.");
				player.EmitSound("EDIT_CONNECT.AllDirections");
			}
		}

		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
		ClearSelectedSet();
	}

	public void CommandNavDisconnect() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		FindActiveNavArea();

		if (SelectedSet.Count > 1) {
			bool bValid = true;
			for (int i = 1; i < SelectedSet.Count; ++i) {
				NavArea first = SelectedSet[0];
				NavArea second = SelectedSet[i];
				if (!first.IsConnected(second, NavDirType.NumDirections) && !second.IsConnected(first, NavDirType.NumDirections)) {
					player.EmitSound("EDIT_CONNECT.AllDirections");
					bValid = false;
					break;
				}
			}

			if (bValid) {
				for (int i = 1; i < SelectedSet.Count; ++i) {
					NavArea first = SelectedSet[0];
					NavArea second = SelectedSet[i];
					first.Disconnect(second);
					second.Disconnect(first);
				}
				player.EmitSound("EDIT_DISCONNECT.MarkedArea");
			}
		}
		else if (SelectedArea != null) {
			if (MarkedArea != null) {
				MarkedArea.Disconnect(SelectedArea);
				SelectedArea.Disconnect(MarkedArea);
				player.EmitSound("EDIT_DISCONNECT.MarkedArea");
			}
			else if (SelectedSet.Count == 1) {
				SelectedSet[0].Disconnect(SelectedArea);
				SelectedArea.Disconnect(SelectedSet[0]);
				player.EmitSound("EDIT_DISCONNECT.MarkedArea");
			}
			else {
				if (MarkedLadder != null) {
					MarkedLadder.Disconnect(SelectedArea);
					SelectedArea.Disconnect(MarkedLadder);
					player.EmitSound("EDIT_DISCONNECT.MarkedArea");
				}
				else {
					Msg("To disconnect areas, mark an area, highlight a second area, then invoke the disconnect command. This will remove all connections between the two areas.");
					player.EmitSound("EDIT_DISCONNECT.NoMarkedArea");
				}
			}
		}
		else if (SelectedLadder != null) {
			if (MarkedArea != null) {
				MarkedArea.Disconnect(SelectedLadder);
				SelectedLadder.Disconnect(MarkedArea);
				player.EmitSound("EDIT_DISCONNECT.MarkedArea");
			}
			if (SelectedSet.Count == 1) {
				SelectedSet[0].Disconnect(SelectedLadder);
				SelectedLadder.Disconnect(SelectedSet[0]);
				player.EmitSound("EDIT_DISCONNECT.MarkedArea");
			}
			else {
				Msg("To disconnect areas, mark an area, highlight a second area, then invoke the disconnect command. This will remove all connections between the two areas.");
				player.EmitSound("EDIT_DISCONNECT.NoMarkedArea");
			}
		}

		ClearSelectedSet();
		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavDisconnectOutgoingOneWays() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		if (SelectedSet.Count == 0) {
			FindActiveNavArea();

			if (SelectedArea == null)
				return;

			SelectedSet.Add(SelectedArea);
		}

		for (int i = 0; i < SelectedSet.Count; ++i) {
			NavArea area = SelectedSet[i];

			List<NavArea> adjVector = [];
			area.CollectAdjacentAreas(adjVector);

			for (int j = 0; j < adjVector.Count; ++j) {
				NavArea adj = adjVector[j];

				if (!adj.IsConnected(area, NavDirType.NumDirections))
					area.Disconnect(adj);
			}
		}
		player.EmitSound("EDIT_DISCONNECT.MarkedArea");

		ClearSelectedSet();
		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavSplice() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		FindActiveNavArea();

		if (SelectedArea != null) {
			if (GetMarkedArea() != null) {
				if (SelectedArea.SpliceEdit(GetMarkedArea()!))
					player.EmitSound("EDIT_SPLICE.MarkedArea");
				else
					player.EmitSound("EDIT_SPLICE.NoMarkedArea");
			}
			else {
				Msg("To splice, mark an area, highlight a second area, then invoke the splice command to create an area between them");
				player.EmitSound("EDIT_SPLICE.NoMarkedArea");
			}
		}

		SetMarkedArea(null);
		ClearSelectedSet();
		MarkedCorner = NavCornerType.NumCorners;
	}

	void DoToggleAttribute(NavArea area, NavAttributeType attribute) {
		area.SetAttributes(area.GetAttributes() ^ attribute);

		if (attribute == NavAttributeType.Transient) {
			if ((area.GetAttributes() & NavAttributeType.Transient) != 0)
				TransientAreas.Add(area);
			else
				TransientAreas.Remove(area);
		}
	}

	public void CommandNavToggleAttribute(NavAttributeType attribute) {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		if (IsSelectedSetEmpty()) {
			FindActiveNavArea();

			if (SelectedArea != null) {
				player.EmitSound("EDIT.ToggleAttribute");
				DoToggleAttribute(SelectedArea, attribute);
			}
		}
		else {
			player.EmitSound("EDIT.ToggleAttribute");

			foreach (NavArea area in SelectedSet)
				DoToggleAttribute(area, attribute);

			Msg($"Changed attribute in {SelectedSet.Count} areas\n");

			ClearSelectedSet();
		}

		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavTogglePlaceMode() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (IsEditMode(EditModeType.PlacePainting))
			SetEditMode(EditModeType.Normal);
		else
			SetEditMode(EditModeType.PlacePainting);

		player.EmitSound("EDIT_TOGGLE_PLACE_MODE");

		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavPlaceFloodFill() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.PlacePainting))
			return;

		FindActiveNavArea();

		if (SelectedArea != null) {
			PlaceFloodFillFunctor pff = new(SelectedArea);
			NavPathfind.SearchSurroundingAreas(SelectedArea, SelectedArea.GetCenter(), pff.Invoke);
		}

		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavPlaceSet() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.PlacePainting))
			return;

		if (!IsSelectedSetEmpty()) {
			foreach (NavArea area in SelectedSet)
				area.SetPlace(GetNavPlace());
		}
	}

	public void CommandNavPlacePick() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.PlacePainting))
			return;

		FindActiveNavArea();

		if (SelectedArea != null) {
			player.EmitSound("EDIT_PLACE_PICK");
			SetNavPlace(SelectedArea.GetPlace());
		}

		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavTogglePlacePainting() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.PlacePainting))
			return;

		FindActiveNavArea();

		if (SelectedArea != null) {
			if (IsPlacePainting) {
				IsPlacePainting = false;
				player.EmitSound("Bot.EditSwitchOff");
			}
			else {
				IsPlacePainting = true;

				player.EmitSound("Bot.EditSwitchOn");

				SelectedArea.SetPlace(GetNavPlace());
			}
		}

		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavMarkUnnamed() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		FindActiveNavArea();

		if (SelectedArea != null) {
			if (GetMarkedArea() != null) {
				player.EmitSound("EDIT_MARK_UNNAMED.Enable");
				SetMarkedArea(null);
			}
			else {
				SetMarkedArea(null);
				foreach (NavArea area in NavArea.TheNavAreas) {
					if (area.GetPlace() == 0) {
						SetMarkedArea(area);
						break;
					}
				}
				if (GetMarkedArea() == null) {
					player.EmitSound("EDIT_MARK_UNNAMED.NoMarkedArea");
				}
				else {
					player.EmitSound("EDIT_MARK_UNNAMED.MarkedArea");

					int connected = 0;
					connected += GetMarkedArea()!.GetAdjacentCount(NavDirType.North);
					connected += GetMarkedArea()!.GetAdjacentCount(NavDirType.South);
					connected += GetMarkedArea()!.GetAdjacentCount(NavDirType.East);
					connected += GetMarkedArea()!.GetAdjacentCount(NavDirType.West);

					int totalUnnamedAreas = 0;
					foreach (NavArea area in NavArea.TheNavAreas) {
						if (area.GetPlace() == 0)
							++totalUnnamedAreas;
					}

					Msg($"Marked Area is connected to {connected} other Areas - there are {totalUnnamedAreas} total unnamed areas\n");
				}
			}
		}

		MarkedCorner = NavCornerType.NumCorners;
	}

	public void CommandNavCornerSelect() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		FindActiveNavArea();

		if (SelectedArea != null) {
			if (GetMarkedArea() != null) {
				int corner = ((int)MarkedCorner + 1) % ((int)NavCornerType.NumCorners + 1);
				MarkedCorner = (NavCornerType)corner;
				player.EmitSound("EDIT_SELECT_CORNER.MarkedArea");
			}
			else
				player.EmitSound("EDIT_SELECT_CORNER.NoMarkedArea");
		}
	}

	public void CommandNavCornerRaise(in TokenizedCommand args) {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		int amount = 1;
		if (args.ArgC() > 1)
			amount = args.Arg(1, 0);

		if (IsSelectedSetEmpty()) {
			FindActiveNavArea();

			if (SelectedArea != null) {
				if (GetMarkedArea() != null) {
					GetMarkedArea()!.RaiseCorner(MarkedCorner, amount);
					player.EmitSound("EDIT_MOVE_CORNER.MarkedArea");
				}
				else
					player.EmitSound("EDIT_MOVE_CORNER.NoMarkedArea");
			}
		}
		else {
			player.EmitSound("EDIT_MOVE_CORNER.MarkedArea");

			foreach (NavArea area in SelectedSet)
				area.RaiseCorner(NavCornerType.NumCorners, amount, false);

			Msg($"Raised {SelectedSet.Count} areas\n");
		}
	}

	public void CommandNavCornerLower(in TokenizedCommand args) {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		int amount = -1;
		if (args.ArgC() > 1)
			amount = -args.Arg(1, 0);

		if (IsSelectedSetEmpty()) {
			FindActiveNavArea();

			if (SelectedArea != null) {
				if (GetMarkedArea() != null) {
					GetMarkedArea()!.RaiseCorner(MarkedCorner, amount);
					player.EmitSound("EDIT_MOVE_CORNER.MarkedArea");
				}
				else
					player.EmitSound("EDIT_MOVE_CORNER.NoMarkedArea");
			}
		}
		else {
			player.EmitSound("EDIT_MOVE_CORNER.MarkedArea");

			foreach (NavArea area in SelectedSet)
				area.RaiseCorner(NavCornerType.NumCorners, amount, false);

			Msg($"Lowered {SelectedSet.Count} areas\n");
		}
	}

	public void CommandNavCornerPlaceOnGround(in TokenizedCommand args) {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		float inset = 0.0f;
		if (args.ArgC() == 2)
			inset = args.Arg(1, 0.0f);

		if (IsSelectedSetEmpty()) {
			FindActiveNavArea();

			if (SelectedArea != null) {
				if (MarkedArea != null)
					MarkedArea.PlaceOnGround(MarkedCorner, inset);
				else
					SelectedArea.PlaceOnGround(NavCornerType.NumCorners, inset);
				player.EmitSound("EDIT_MOVE_CORNER.MarkedArea");
			}
			else
				player.EmitSound("EDIT_MOVE_CORNER.NoMarkedArea");
		}
		else {
			player.EmitSound("EDIT_MOVE_CORNER.MarkedArea");

			foreach (NavArea area in SelectedSet)
				area.PlaceOnGround(NavCornerType.NumCorners, inset);

			Msg($"Placed {SelectedSet.Count} areas on the ground\n");
		}
	}

	public void CommandNavWarpToMark() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		NavArea? targetArea = GetMarkedArea();
		if (targetArea == null && !IsSelectedSetEmpty())
			targetArea = SelectedSet[0];

		if (targetArea != null) {
			Vector3 origin = targetArea.GetCenter() + new Vector3(0, 0, 0.75f * HumanHeight);
			QAngle angles = player.GetAbsAngles();

			if ((player.IsDead() || player.IsObserver()) && player.GetObserverMode() == ObserverMode.Roaming) {
				Util.SetOrigin(player, origin);
				player.EmitSound("EDIT_WARP_TO_MARK");
			}
			else {
				player.Teleport(origin, angles, vec3_origin);
				player.EmitSound("EDIT_WARP_TO_MARK");
			}
		}
		else if (GetMarkedLadder() != null) {
			NavLadder ladder = GetMarkedLadder()!;

			QAngle angles = player.GetAbsAngles();
			Vector3 origin = (ladder.Top + ladder.Bottom) / 2;
			origin.X += ladder.GetNormal().X * GenerationStepSize;
			origin.Y += ladder.GetNormal().Y * GenerationStepSize;

			if ((player.IsDead() || player.IsObserver()) && player.GetObserverMode() == ObserverMode.Roaming) {
				Util.SetOrigin(player, origin);
				player.EmitSound("EDIT_WARP_TO_MARK");
			}
			else {
				player.Teleport(origin, angles, vec3_origin);
				player.EmitSound("EDIT_WARP_TO_MARK");
			}
		}
		else
			player.EmitSound("EDIT_WARP_TO_MARK");
	}

	public void CommandNavLadderFlip() {
		BasePlayer? player = Util.GetListenServerHost();
		if (player == null)
			return;

		if (!IsEditMode(EditModeType.Normal))
			return;

		FindActiveNavArea();

		if (SelectedLadder != null) {
			NavArea? area;

			player.EmitSound("EDIT_MOVE_CORNER.MarkedArea");
			SelectedLadder.SetDir(OppositeDirection(SelectedLadder.GetDir()));

			area = SelectedLadder.TopBehindArea;
			SelectedLadder.TopBehindArea = SelectedLadder.TopForwardArea;
			SelectedLadder.TopForwardArea = area;

			area = SelectedLadder.TopRightArea;
			SelectedLadder.TopRightArea = SelectedLadder.TopLeftArea;
			SelectedLadder.TopLeftArea = area;
		}

		SetMarkedArea(null);
		MarkedCorner = NavCornerType.NumCorners;
	}

	[ConCommand("nav_select_radius", "Adds all areas in a radius to the selection set", FCvar.Cheat)]
	static void nav_select_radius(in TokenizedCommand args) {
		throw new NotImplementedException();
	}

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

class DrawSelectedSet(Vector3 shift)
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

class SelectCollector(int count)
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

class ShiftSet(Vector3 shift)
{
	readonly List<NavLadder> Ladders = [];
	Vector3 Shift = shift;

	public bool Invoke(NavArea area) {
		area.Shift(Shift);

		List<NavLadderConnect> ladders = area.GetLadders(NavLadder.LadderDirectionType.Up);
		for (int i = 0; i < ladders.Count; ++i) {
			NavLadder ladder = ladders[i].Ladder!;
			if (!Ladders.Contains(ladder)) {
				ladder.Shift(Shift);
				Ladders.Add(ladder);
			}
		}

		ladders = area.GetLadders(NavLadder.LadderDirectionType.Down);
		for (int i = 0; i < ladders.Count; ++i) {
			NavLadder ladder = ladders[i].Ladder!;
			if (!Ladders.Contains(ladder)) {
				ladder.Shift(Shift);
				Ladders.Add(ladder);
			}
		}

		return true;
	}
}

class PlaceFloodFillFunctor(NavArea area)
{
	readonly uint InitialPlace = area.GetPlace();

	public bool Invoke(NavArea area) {
		if (area.GetPlace() != InitialPlace)
			return false;

		area.SetPlace(NavMesh.Instance!.GetNavPlace());

		return true;
	}
}

class RadiusSelect(Vector3 origin, float radius)
{
	Vector3 Origin = origin;
	float RadiusSquared = radius * radius;
	int Selected = 0;

	public bool Invoke(NavArea area) {
		if (NavMesh.Instance!.IsInSelectedSet(area))
			return true;

		area.GetClosestPointOnArea(Origin, out Vector3 close);
		if (close.DistToSqr(Origin) < RadiusSquared) {
			NavMesh.Instance.AddToSelectedSet(area);
			++Selected;
		}

		return true;
	}

	public int GetNumSelected() => Selected;
}