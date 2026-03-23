using Source.Common.Commands;

using static Game.Server.NavMesh.Nav;

namespace Game.Server.NavMesh;

static class NavCommands
{

	[ConCommand("nav_remove_jump_areas", "Removes legacy jump areas, replacing them with connections.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_remove_jump_areas() { throw new NotImplementedException(); }

	[ConCommand("nav_delete", "Deletes the currently highlighted Area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_delete() { throw new NotImplementedException(); }

	[ConCommand("nav_delete_marked", "Deletes the currently marked Area (if any).", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_delete_marked() { throw new NotImplementedException(); }

	[ConCommand("nav_toggle_selected_set", "Toggles all areas into/out of the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_toggle_selected_set() { throw new NotImplementedException(); }

	[ConCommand("nav_store_selected_set", "Stores the current selected set for later retrieval.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_store_selected_set() { throw new NotImplementedException(); }

	[ConCommand("nav_recall_selected_set", "Re-selects the stored selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_recall_selected_set() { throw new NotImplementedException(); }

	[ConCommand("nav_add_to_selected_set", "Add current area to the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_add_to_selected_set() { throw new NotImplementedException(); }

	[ConCommand("nav_remove_from_selected_set", "Remove current area from the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_remove_from_selected_set() { throw new NotImplementedException(); }

	[ConCommand("nav_toggle_in_selected_set", "Remove current area from the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_toggle_in_selected_set() { throw new NotImplementedException(); }

	[ConCommand("nav_clear_selected_set", "Clear the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_clear_selected_set() { throw new NotImplementedException(); }

	[ConCommand("nav_begin_selecting", "Start continuously adding to the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_begin_selecting() { throw new NotImplementedException(); }

	[ConCommand("nav_end_selecting", "Stop continuously adding to the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_end_selecting() { throw new NotImplementedException(); }

	[ConCommand("nav_begin_drag_selecting", "Start dragging a selection area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_begin_drag_selecting() { throw new NotImplementedException(); }

	[ConCommand("nav_end_drag_selecting", "Stop dragging a selection area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_end_drag_selecting() { throw new NotImplementedException(); }

	[ConCommand("nav_begin_drag_deselecting", "Start dragging a selection area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_begin_drag_deselecting() { throw new NotImplementedException(); }

	[ConCommand("nav_end_drag_deselecting", "Stop dragging a selection area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_end_drag_deselecting() { throw new NotImplementedException(); }

	[ConCommand("nav_raise_drag_volume_max", "Raise the top of the drag select volume.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_raise_drag_volume_max() { throw new NotImplementedException(); }

	[ConCommand("nav_lower_drag_volume_max", "Lower the top of the drag select volume.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_lower_drag_volume_max() { throw new NotImplementedException(); }

	[ConCommand("nav_raise_drag_volume_min", "Raise the bottom of the drag select volume.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_raise_drag_volume_min() { throw new NotImplementedException(); }

	[ConCommand("nav_lower_drag_volume_min", "Lower the bottom of the drag select volume.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_lower_drag_volume_min() { throw new NotImplementedException(); }

	[ConCommand("nav_toggle_selecting", "Start or stop continuously adding to the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_toggle_selecting() { throw new NotImplementedException(); }

	[ConCommand("nav_begin_deselecting", "Start continuously removing from the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_begin_deselecting() { throw new NotImplementedException(); }

	[ConCommand("nav_end_deselecting", "Stop continuously removing from the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_end_deselecting() { throw new NotImplementedException(); }

	[ConCommand("nav_toggle_deselecting", "Start or stop continuously removing from the selected set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_toggle_deselecting() { throw new NotImplementedException(); }

	[ConCommand("nav_begin_shift_xy", "Begin shifting the Selected Set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_begin_shift_xy() { throw new NotImplementedException(); }

	[ConCommand("nav_end_shift_xy", "Finish shifting the Selected Set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_end_shift_xy() { throw new NotImplementedException(); }

	[ConCommand("nav_select_invalid_areas", "Adds all invalid areas to the Selected Set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_select_invalid_areas() { throw new NotImplementedException(); }

	[ConCommand("nav_split", "To split an Area into two, align the split line using your cursor and invoke the split command.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_split() { throw new NotImplementedException(); }

	[ConCommand("nav_make_sniper_spots", "Chops the marked area into disconnected sub-areas suitable for sniper spots.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_make_sniper_spots() { throw new NotImplementedException(); }

	[ConCommand("nav_merge", "To merge two Areas into one, mark the first Area, highlight the second by pointing your cursor at it, and invoke the merge command.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_merge() { throw new NotImplementedException(); }

	[ConCommand("nav_mark", "Marks the Area or Ladder under the cursor for manipulation by subsequent editing commands.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_mark() { throw new NotImplementedException(); }

	[ConCommand("nav_unmark", "Clears the marked Area or Ladder.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_unmark() { throw new NotImplementedException(); }

	[ConCommand("nav_begin_area", "Defines a corner of a new Area or Ladder. To complete the Area or Ladder, drag the opposite corner to the desired location and issue a 'nav_end_area' command.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_begin_area() { throw new NotImplementedException(); }

	[ConCommand("nav_end_area", "Defines the second corner of a new Area or Ladder and creates it.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_end_area() { throw new NotImplementedException(); }

	[ConCommand("nav_connect", "To connect two Areas, mark the first Area, highlight the second Area, then invoke the connect command. Note that this creates a ONE-WAY connection from the first to the second Area. To make a two-way connection, also connect the second area to the first.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_connect() { throw new NotImplementedException(); }

	[ConCommand("nav_disconnect", "To disconnect two Areas, mark an Area, highlight a second Area, then invoke the disconnect command. This will remove all connections between the two Areas.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_disconnect() { throw new NotImplementedException(); }

	[ConCommand("nav_disconnect_outgoing_oneways", "For each area in the selected set, disconnect all outgoing one-way connections.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_disconnect_outgoing_oneways() { throw new NotImplementedException(); }

	[ConCommand("nav_splice", "To splice, mark an area, highlight a second area, then invoke the splice command to create a new, connected area between them.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_splice() { throw new NotImplementedException(); }

	[ConCommand("nav_crouch", "Toggles the 'must crouch in this area' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_crouch() { throw new NotImplementedException(); }

	[ConCommand("nav_precise", "Toggles the 'dont avoid obstacles' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_precise() { throw new NotImplementedException(); }

	[ConCommand("nav_jump", "Toggles the 'traverse this area by jumping' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_jump() { throw new NotImplementedException(); }

	[ConCommand("nav_no_jump", "Toggles the 'dont jump in this area' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_no_jump() { throw new NotImplementedException(); }

	[ConCommand("nav_stop", "Toggles the 'must stop when entering this area' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_stop() { throw new NotImplementedException(); }

	[ConCommand("nav_walk", "Toggles the 'traverse this area by walking' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_walk() { throw new NotImplementedException(); }

	[ConCommand("nav_run", "Toggles the 'traverse this area by running' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_run() { throw new NotImplementedException(); }

	[ConCommand("nav_avoid", "Toggles the 'avoid this area when possible' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_avoid() { throw new NotImplementedException(); }

	[ConCommand("nav_transient", "Toggles the 'area is transient and may become blocked' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_transient() { throw new NotImplementedException(); }

	[ConCommand("nav_dont_hide", "Toggles the 'area is not suitable for hiding spots' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_dont_hide() { throw new NotImplementedException(); }

	[ConCommand("nav_stand", "Toggles the 'stand while hiding' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_stand() { throw new NotImplementedException(); }

	[ConCommand("nav_no_hostages", "Toggles the 'hostages cannot use this area' flag used by the AI system.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_no_hostages() { throw new NotImplementedException(); }

	[ConCommand("nav_strip", "Strips all Hiding Spots, Approach Points, and Encounter Spots from the current Area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_strip() { throw new NotImplementedException(); }

	[ConCommand("nav_save", "Saves the current Navigation Mesh to disk.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_save() { throw new NotImplementedException(); }

	[ConCommand("nav_load", "Loads the Navigation Mesh for the current map.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_load() { throw new NotImplementedException(); }

	[ConCommand("nav_use_place", "If used without arguments, all available Places will be listed. If a Place argument is given, the current Place is set.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_use_place() { throw new NotImplementedException(); }

	[ConCommand("nav_place_replace", "Replaces all instances of the first place with the second place.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_place_replace() { throw new NotImplementedException(); }

	[ConCommand("nav_place_list", "Lists all place names used in the map.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_place_list() { throw new NotImplementedException(); }

	[ConCommand("nav_toggle_place_mode", "Toggle the editor into and out of Place mode. Place mode allows labelling of Area with Place names.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_toggle_place_mode() { throw new NotImplementedException(); }

	[ConCommand("nav_set_place_mode", "Sets the editor into or out of Place mode. Place mode allows labelling of Area with Place names.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_set_place_mode() { throw new NotImplementedException(); }

	[ConCommand("nav_place_floodfill", "Sets the Place of the Area under the cursor to the curent Place, and 'flood-fills' the Place to all adjacent Areas. Flood-filling stops when it hits an Area with the same Place, or a different Place than that of the initial Area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_place_floodfill() { throw new NotImplementedException(); }

	[ConCommand("nav_place_set", "Sets the Place of all selected areas to the current Place.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_place_set() { throw new NotImplementedException(); }

	[ConCommand("nav_place_pick", "Sets the current Place to the Place of the Area under the cursor.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_place_pick() { throw new NotImplementedException(); }

	[ConCommand("nav_toggle_place_painting", "Toggles Place Painting mode. When Place Painting, pointing at an Area will 'paint' it with the current Place.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_toggle_place_painting() { throw new NotImplementedException(); }

	[ConCommand("nav_mark_unnamed", "Mark an Area with no Place name. Useful for finding stray areas missed when Place Painting.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_mark_unnamed() { throw new NotImplementedException(); }

	[ConCommand("nav_corner_select", "Select a corner of the currently marked Area. Use multiple times to access all four corners.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_corner_select() { throw new NotImplementedException(); }

	[ConCommand("nav_warp_to_mark", "Warps the player to the marked area.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_warp_to_mark() { throw new NotImplementedException(); }

	[ConCommand("nav_ladder_flip", "Flips the selected ladder's direction.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_ladder_flip() { throw new NotImplementedException(); }

	[ConCommand("nav_generate", "Generate a Navigation Mesh for the current map and save it to disk.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_generate() { throw new NotImplementedException(); }

	[ConCommand("nav_generate_incremental", "Generate a Navigation Mesh for the current map and save it to disk.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_generate_incremental() { throw new NotImplementedException(); }

	[ConCommand("nav_analyze", "Re-analyze the current Navigation Mesh and save it to disk.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_analyze() { throw new NotImplementedException(); }

	[ConCommand("nav_analyze_scripted", "commandline hook to run a nav_analyze and then quit.", FCvar.GameDLL | FCvar.Cheat | FCvar.Hidden)]
	static void nav_analyze_scripted() { throw new NotImplementedException(); }

	[ConCommand("nav_mark_walkable", "Mark the current location as a walkable position. These positions are used as seed locations when sampling the map to generate a Navigation Mesh.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_mark_walkable() { throw new NotImplementedException(); }

	[ConCommand("nav_clear_walkable_marks", "Erase any previously placed walkable positions.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_clear_walkable_marks() { throw new NotImplementedException(); }

	[ConCommand("nav_compress_id", "Re-orders area and ladder ID's so they are continuous.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_compress_id() { throw new NotImplementedException(); }

	[ConCommand("nav_show_ladder_bounds", "Draws the bounding boxes of all func_ladders in the map.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_show_ladder_bounds() { throw new NotImplementedException(); }

	[ConCommand("nav_build_ladder", "Attempts to build a nav ladder on the climbable surface under the cursor.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_build_ladder() { throw new NotImplementedException(); }

	[ConCommand("wipe_nav_attributes", "Clear all nav attributes of selected area.", FCvar.Cheat)]
	static void ClearAllNavAttributes() { throw new NotImplementedException(); }

	[ConCommand("nav_clear_attribute", "Remove given nav attribute from all areas in the selected set.", FCvar.Cheat)]
	static void NavClearAttribute() { throw new NotImplementedException(); }

	[ConCommand("nav_mark_attribute", "Set nav attribute for all areas in the selected set.", FCvar.Cheat)]
	static void NavMarkAttribute() { throw new NotImplementedException(); }

	[ConCommand("nav_pick_area", "Marks an area (and corner) based on the surface under the cursor.", FCvar.GameDLL | FCvar.Cheat)]
	static void nav_pick_area() { throw new NotImplementedException(); }
}