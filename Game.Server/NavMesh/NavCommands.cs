using Source.Common.Commands;

using static Game.Server.NavMesh.Nav;

namespace Game.Server.NavMesh;

static class NavCommands
{
	static NavMesh TheNavMesh => NavMesh.Instance!;

	[ConCommand("nav_remove_jump_areas", "Removes legacy jump areas, replacing them with connections.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_remove_jump_areas() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavRemoveJumpAreas();
	}

	[ConCommand("nav_delete", "Deletes the currently highlighted Area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_delete() {
		if (!Util.IsCommandIssuedByServerAdmin() || !nav_edit.GetBool())
			return;

		TheNavMesh.CommandNavDelete();
	}

	[ConCommand("nav_delete_marked", "Deletes the currently marked Area (if any).", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_delete_marked() {
		if (!Util.IsCommandIssuedByServerAdmin() || !nav_edit.GetBool())
			return;

		TheNavMesh.CommandNavDeleteMarked();
	}

	[ConCommand("nav_flood_select", "Selects the current Area and all Areas connected to it, recursively. To clear a selection, use this command again.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_flood_select() { throw new NotImplementedException(); }

	[ConCommand("nav_toggle_selected_set", "Toggles all areas into/out of the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_toggle_selected_set() {
		if (!Util.IsCommandIssuedByServerAdmin() || !nav_edit.GetBool())
			return;

		TheNavMesh.CommandNavToggleSelectedSet();
	}

	[ConCommand("nav_store_selected_set", "Stores the current selected set for later retrieval.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_store_selected_set() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavStoreSelectedSet();
	}

	[ConCommand("nav_recall_selected_set", "Re-selects the stored selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_recall_selected_set() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavRecallSelectedSet();
	}

	[ConCommand("nav_add_to_selected_set", "Add current area to the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_add_to_selected_set() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavAddToSelectedSet();
	}

	[ConCommand("nav_add_to_selected_set_by_id", "Add specified area id to the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_add_to_selected_set_by_id(in TokenizedCommand args) {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavAddToSelectedSetByID(args);
	}

	[ConCommand("nav_remove_from_selected_set", "Remove current area from the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_remove_from_selected_set() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavRemoveFromSelectedSet();
	}

	[ConCommand("nav_toggle_in_selected_set", "Remove current area from the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_toggle_in_selected_set() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavToggleInSelectedSet();
	}

	[ConCommand("nav_clear_selected_set", "Clear the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_clear_selected_set() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavClearSelectedSet();
	}

	[ConCommand("nav_dump_selected_set_positions", "Write the (x,y,z) coordinates of the centers of all selected nav areas to a file.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_dump_selected_set_positions() { throw new NotImplementedException(); }

	[ConCommand("nav_show_dumped_positions", "Show the (x,y,z) coordinate positions of the given dump file.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_show_dumped_positions() { throw new NotImplementedException(); }

	[ConCommand("nav_select_larger_than", "Select nav areas where both dimensions are larger than the given size.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_select_larger_than(in TokenizedCommand args) {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		if (args.ArgC() <= 1)
			return;

		float minSize = args.Arg(1, 0);

		int selectedCount = 0;

		for (int i = 0; i < NavArea.TheNavAreas.Count; ++i) {
			NavArea area = NavArea.TheNavAreas[i];
			if (area.GetSizeX() > minSize && area.GetSizeY() > minSize) {
				TheNavMesh.AddToSelectedSet(area);
				++selectedCount;
			}
		}

		DevMsg($"Selected {selectedCount} areas with dimensions larger than {minSize:3.2f} units.\n");
	}

	[ConCommand("nav_begin_selecting", "Start continuously adding to the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_begin_selecting() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavBeginSelecting();
	}

	[ConCommand("nav_end_selecting", "Stop continuously adding to the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_end_selecting() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavEndSelecting();
	}

	[ConCommand("nav_begin_drag_selecting", "Start dragging a selection area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_begin_drag_selecting() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavBeginDragSelecting();
	}

	[ConCommand("nav_end_drag_selecting", "Stop dragging a selection area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_end_drag_selecting() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavEndDragSelecting();
	}

	[ConCommand("nav_begin_drag_deselecting", "Start dragging a selection area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_begin_drag_deselecting() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavBeginDragDeselecting();
	}

	[ConCommand("nav_end_drag_deselecting", "Stop dragging a selection area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_end_drag_deselecting() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavEndDragDeselecting();
	}

	[ConCommand("nav_raise_drag_volume_max", "Raise the top of the drag select volume.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_raise_drag_volume_max() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavRaiseDragVolumeMax();
	}

	[ConCommand("nav_lower_drag_volume_max", "Lower the top of the drag select volume.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_lower_drag_volume_max() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavLowerDragVolumeMax();
	}

	[ConCommand("nav_raise_drag_volume_min", "Raise the bottom of the drag select volume.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_raise_drag_volume_min() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavRaiseDragVolumeMin();
	}

	[ConCommand("nav_lower_drag_volume_min", "Lower the bottom of the drag select volume.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_lower_drag_volume_min() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavLowerDragVolumeMin();
	}

	[ConCommand("nav_toggle_selecting", "Start or stop continuously adding to the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_toggle_selecting() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavToggleSelecting();
	}

	[ConCommand("nav_begin_deselecting", "Start continuously removing from the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_begin_deselecting() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavBeginDeselecting();
	}

	[ConCommand("nav_end_deselecting", "Stop continuously removing from the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_end_deselecting() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavEndDeselecting();
	}

	[ConCommand("nav_toggle_deselecting", "Start or stop continuously removing from the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_toggle_deselecting() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavToggleDeselecting();
	}

	[ConCommand("nav_select_half_space", "Selects any areas that intersect the given half-space.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_select_half_space(in TokenizedCommand args) {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavSelectHalfSpace(args);
	}

	[ConCommand("nav_begin_shift_xy", "Begin shifting the Selected Set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_begin_shift_xy() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavBeginShiftXY();
	}

	[ConCommand("nav_end_shift_xy", "Finish shifting the Selected Set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_end_shift_xy() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavEndShiftXY();
	}

	[ConCommand("nav_select_invalid_areas", "Adds all invalid areas to the Selected Set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_select_invalid_areas() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavSelectInvalidAreas();
	}

	[ConCommand("nav_select_blocked_areas", "Adds all blocked areas to the selected set", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_select_blocked_areas() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavSelectBlockedAreas();
	}

	[ConCommand("nav_select_obstructed_areas", "Adds all obstructed areas to the selected set", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_select_obstructed_areas() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavSelectObstructedAreas();
	}

	[ConCommand("nav_select_stairs", "Adds all stairway areas to the selected set", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_select_stairs() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavSelectStairs();
	}

	[ConCommand("nav_select_orphans", "Adds all orphan areas to the selected set (highlight a valid area first).", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_select_orphans() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavSelectOrphans();
	}

	[ConCommand("nav_split", "To split an Area into two, align the split line using your cursor and invoke the split command.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_split() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavSplit();
	}

	[ConCommand("nav_make_sniper_spots", "Chops the marked area into disconnected sub-areas suitable for sniper spots.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_make_sniper_spots() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavMakeSniperSpots();
	}

	[ConCommand("nav_merge", "To merge two Areas into one, mark the first Area, highlight the second by pointing your cursor at it, and invoke the merge command.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_merge() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavMerge();
	}

	[ConCommand("nav_mark", "Marks the Area or Ladder under the cursor for manipulation by subsequent editing commands.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_mark(in TokenizedCommand args) {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavMark(args);
	}

	[ConCommand("nav_unmark", "Clears the marked Area or Ladder.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_unmark() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavUnmark();
	}

	[ConCommand("nav_begin_area", "Defines a corner of a new Area or Ladder. To complete the Area or Ladder, drag the opposite corner to the desired location and issue a 'nav_end_area' command.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_begin_area() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavBeginArea();
	}

	[ConCommand("nav_end_area", "Defines the second corner of a new Area or Ladder and creates it.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_end_area() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavEndArea();
	}

	[ConCommand("nav_connect", "To connect two Areas, mark the first Area, highlight the second Area, then invoke the connect command. Note that this creates a ONE-WAY connection from the first to the second Area. To make a two-way connection, also connect the second area to the first.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_connect() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavConnect();
	}

	[ConCommand("nav_disconnect", "To disconnect two Areas, mark an Area, highlight a second Area, then invoke the disconnect command. This will remove all connections between the two Areas.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_disconnect() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavDisconnect();
	}

	[ConCommand("nav_disconnect_outgoing_oneways", "For each area in the selected set, disconnect all outgoing one-way connections.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_disconnect_outgoing_oneways() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavDisconnectOutgoingOneWays();
	}

	[ConCommand("nav_splice", "To splice, mark an area, highlight a second area, then invoke the splice command to create a new, connected area between them.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_splice() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavSplice();
	}

	[ConCommand("nav_crouch", "Toggles the 'must crouch in this area' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_crouch() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavToggleAttribute(NavAttributeType.Crouch);
	}

	[ConCommand("nav_precise", "Toggles the 'dont avoid obstacles' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_precise() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavToggleAttribute(NavAttributeType.Precice);
	}

	[ConCommand("nav_jump", "Toggles the 'traverse this area by jumping' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_jump() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavToggleAttribute(NavAttributeType.Jump);
	}

	[ConCommand("nav_no_jump", "Toggles the 'dont jump in this area' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_no_jump() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavToggleAttribute(NavAttributeType.NoJump);
	}

	[ConCommand("nav_stop", "Toggles the 'must stop when entering this area' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_stop() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavToggleAttribute(NavAttributeType.Stop);
	}

	[ConCommand("nav_walk", "Toggles the 'traverse this area by walking' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_walk() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavToggleAttribute(NavAttributeType.Walk);
	}

	[ConCommand("nav_run", "Toggles the 'traverse this area by running' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_run() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavToggleAttribute(NavAttributeType.Run);
	}

	[ConCommand("nav_avoid", "Toggles the 'avoid this area when possible' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_avoid() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavToggleAttribute(NavAttributeType.Avoid);
	}

	[ConCommand("nav_transient", "Toggles the 'area is transient and may become blocked' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_transient() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavToggleAttribute(NavAttributeType.Transient);
	}

	[ConCommand("nav_dont_hide", "Toggles the 'area is not suitable for hiding spots' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_dont_hide() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavToggleAttribute(NavAttributeType.DontHide);
	}

	[ConCommand("nav_stand", "Toggles the 'stand while hiding' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_stand() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavToggleAttribute(NavAttributeType.Stand);
	}

	[ConCommand("nav_no_hostages", "Toggles the 'hostages cannot use this area' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_no_hostages() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavToggleAttribute(NavAttributeType.NoHostages);
	}

	[ConCommand("nav_strip", "Strips all Hiding Spots, Approach Points, and Encounter Spots from the current Area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_strip() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.StripNavigationAreas();
	}

	[ConCommand("nav_save", "Saves the current Navigation Mesh to disk.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_save() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		if (TheNavMesh.Save())
			Msg($"Navigation map '{TheNavMesh.GetFilename()}' saved.\n");
		else
			Msg($"ERROR: Unable to save navigation map '{TheNavMesh.GetFilename()}'.\n");
	}

	[ConCommand("nav_load", "Loads the Navigation Mesh for the current map.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_load() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		if (TheNavMesh.Load() != NavErrorType.Ok)
			Msg($"ERROR: Navigation Mesh load failed.\n");
	}

	// todo: autocomplete
	[ConCommand("nav_use_place", "If used without arguments, all available Places will be listed. If a Place argument is given, the current Place is set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_use_place() { throw new NotImplementedException(); }

	[ConCommand("nav_place_replace", "Replaces all instances of the first place with the second place.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_place_replace() { throw new NotImplementedException(); }

	[ConCommand("nav_place_list", "Lists all place names used in the map.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_place_list() { throw new NotImplementedException(); }

	[ConCommand("nav_toggle_place_mode", "Toggle the editor into and out of Place mode. Place mode allows labelling of Area with Place names.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_toggle_place_mode() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavTogglePlaceMode();
	}

	[ConCommand("nav_set_place_mode", "Sets the editor into or out of Place mode. Place mode allows labelling of Area with Place names.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_set_place_mode() { throw new NotImplementedException(); }

	[ConCommand("nav_place_floodfill", "Sets the Place of the Area under the cursor to the curent Place, and 'flood-fills' the Place to all adjacent Areas. Flood-filling stops when it hits an Area with the same Place, or a different Place than that of the initial Area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_place_floodfill() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavPlaceFloodFill();
	}

	[ConCommand("nav_place_set", "Sets the Place of all selected areas to the current Place.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_place_set() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavPlaceSet();
	}

	[ConCommand("nav_place_pick", "Sets the current Place to the Place of the Area under the cursor.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_place_pick() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavPlacePick();
	}

	[ConCommand("nav_toggle_place_painting", "Toggles Place Painting mode. When Place Painting, pointing at an Area will 'paint' it with the current Place.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_toggle_place_painting() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavTogglePlacePainting();
	}

	[ConCommand("nav_mark_unnamed", "Mark an Area with no Place name. Useful for finding stray areas missed when Place Painting.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_mark_unnamed() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavMarkUnnamed();
	}

	[ConCommand("nav_corner_select", "Select a corner of the currently marked Area. Use multiple times to access all four corners.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_corner_select() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavCornerSelect();
	}

	[ConCommand("nav_corner_raise", "Raise the selected corner of the currently marked Area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_corner_raise(in TokenizedCommand args) {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavCornerRaise(args);
	}

	[ConCommand("nav_corner_lower", "Lower the selected corner of the currently marked Area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_corner_lower(in TokenizedCommand args) {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavCornerLower(args);
	}

	[ConCommand("nav_corner_place_on_ground", "Places the selected corner of the currently marked Area on the ground.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_corner_place_on_ground(in TokenizedCommand args) {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavCornerPlaceOnGround(args);
	}

	[ConCommand("nav_warp_to_mark", "Warps the player to the marked area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_warp_to_mark() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavWarpToMark();
	}

	[ConCommand("nav_ladder_flip", "Flips the selected ladder's direction.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_ladder_flip() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavLadderFlip();
	}

	[ConCommand("nav_generate", "Generate a Navigation Mesh for the current map and save it to disk.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_generate() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.BeginGeneration();
	}

	[ConCommand("nav_generate_incremental", "Generate a Navigation Mesh for the current map and save it to disk.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_generate_incremental() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.BeginGeneration(true);
	}

	[ConCommand("nav_analyze", "Re-analyze the current Navigation Mesh and save it to disk.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_analyze() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		if (nav_edit.GetBool())
			TheNavMesh.BeginAnalysis();
	}

	[ConCommand("nav_analyze_scripted", "commandline hook to run a nav_analyze and then quit.", FCvar.GameDLL | FCvar.Cheat | FCvar.Hidden)]
	static void nav_analyze_scripted() { throw new NotImplementedException(); }

	[ConCommand("nav_mark_walkable", "Mark the current location as a walkable position. These positions are used as seed locations when sampling the map to generate a Navigation Mesh.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_mark_walkable() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavMarkWalkable();
	}

	[ConCommand("nav_clear_walkable_marks", "Erase any previously placed walkable positions.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_clear_walkable_marks() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.ClearWalkableSeeds();
	}

	[ConCommand("nav_compress_id", "Re-orders area and ladder ID's so they are continuous.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_compress_id() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		NavArea.CompressIDs();
		NavLadder.CompressIDs();
	}

	[ConCommand("nav_show_ladder_bounds", "Draws the bounding boxes of all func_ladders in the map.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_show_ladder_bounds() { throw new NotImplementedException(); }

	[ConCommand("nav_build_ladder", "Attempts to build a nav ladder on the climbable surface under the cursor.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_build_ladder() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		TheNavMesh.CommandNavBuildLadder();
	}

	[ConCommand("wipe_nav_attributes", "Clear all nav attributes of selected area.", FCvar.Cheat)]
	static void ClearAllNavAttributes() {
		throw new NotImplementedException("NavAttributeClearer/NavAttributeToggler todo");
	}

	[ConCommand("nav_clear_attribute", "Remove given nav attribute from all areas in the selected set.", FCvar.Cheat)]
	static void NavClearAttribute() { throw new NotImplementedException(); }

	[ConCommand("nav_mark_attribute", "Set nav attribute for all areas in the selected set.", FCvar.Cheat)]
	static void NavMarkAttribute() { throw new NotImplementedException(); }
}