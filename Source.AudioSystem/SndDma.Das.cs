using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.AudioSystem;

[InlineArray(SndDma.DAS_CWALLS)] public struct DasWallsInt { int element; }
[InlineArray(SndDma.DAS_CWALLS)] public struct DasWallsFloat { float element; }
[InlineArray(SndDma.DAS_CWALLS)] public struct DasWallsVector { Vector3 element; }
[InlineArray(6)] public struct DasWalls6Float { float element; }

// auto dsp room struct

public struct DasRoom
{
	public DasWallsInt dist;        // distance in units from player to axis aligned and diagonal walls
	public DasWallsFloat reflect;   // acoustic reflectivity per wall
	public DasWallsFloat skyhits;   // every sky hit adds 0.1
	public DasWallsVector hit;      // location of trace hit on wall - used for calculating average centers
	public DasWallsVector norm; // wall normal at hit location

	public Vector3 vplayer;             // 'frozen' location above player's head

	public Vector3 vplayer_eyes;        // 'frozen' location player's eyes

	public int width_max;               // max width
	public int length_max;              // max length
	public int height_max;              // max height

	public float refl_avg;              // running average of reflectivity of all walls
	public DasWalls6Float refl_walls;       // left,right,front,back,ceiling,floor reflectivities

	public float sky_pct;               // percent of sky hits

	public Vector3 room_mins;           // room bounds
	public Vector3 room_maxs;

	public double last_dsp_change;      // time since last dsp change

	public float diffusion;         // 0..1.0 check radius (avg of width_avg) for # of props - scale diffusion based on # found
	public short iwall;                 // cycles through walls 0..5, ensuring only one trace per frame
	public short ent_count;                 // count of entities found in radius
	public bool bskyabove;              // true if sky found above player (ie: outside)
	public bool broomready;         // true if all distances are filled in and room is ready to check
	public short lowceiling;                // if non-zero, ceiling directly above player if < 112 units
}

// dsp detection node

public struct DasNode
{
	public Vector3 vplayer;             // position

	public bool fused;                  // true if valid node
	public bool fseesplayer;            // true if node sees player on last check
	public short dsp_preset;                // preset

	public int range_min;               // min,max detection ranges
	public int range_max;

	public int dist;                    // last distance to player

	// room parameters when node was created:

	public DasRoom room;
}

public static partial class SndDma
{
	// Dsp Automatic Selection:

	//	a) enabled by setting dsp_room to DSP_AUTOMATIC.  Subsequently, dsp_automatic is the actual dsp value for dsp_room.
	//	b) disabled by setting dsp_room to anything else

	//	c) while enabled, detection nodes are placed as player moves into a new space
	//     i.	at each node, a new dsp setting is calculated and dsp_automatic is set to an appropriate preset
	//     ii.	new nodes are set when player moves out of sight of previous node
	//     iii. moving into line of sight of a detection node causes closest node to player to set dsp_automatic

	// see void DAS_CheckNewRoomDSP( ) for main entrypoint

	public static readonly ConVar das_debug = new("adsp_debug", "0", FCvar.Archive);
	// >0: draw blue dsp detection node location
	// >1: draw green room trace height detection bars
	// 3: draw yellow horizontal trace bars for room width/depth detection
	// 4: draw yellow upward traces for height detection
	// 5: draw teal box around all props around player
	// 6: draw teal box around room as detected

	public const int DAS_CWALLS = 20;               // # of wall traces to save for calculating room dimensions
	const float DAS_ROOM_TRACE_LEN = 400.0F * 12.0F;    // max size of trace to check for room dimensions

	const float DAS_AUTO_WAIT = 0.25F;                  // wait min of n seconds between dsp_room changes and update checks

	const float DAS_WIDTH_MIN = 0.4F;                       // min % change in avg width of any wall pair to cause new dsp
	const float DAS_REFL_MIN = 0.5F;                        // min % change in avg refl of any wall to cause new dsp
	const float DAS_SKYHIT_MIN = 0.8F;                      // min % change in # of sky hits per wall

	const float DAS_DIST_MIN = 4.0F * 12.0F;            // min distance between room dsp changes
	const float DAS_DIST_MAX = 40.0F * 12.0F;           // max distance to preserve room dsp changes

	const float DAS_DIST_MIN_OUTSIDE = 6.0F * 12.0F;    // min distance between room dsp changes outside
	const float DAS_DIST_MAX_OUTSIDE = 100.0F * 12.0F;  // max distance to preserve room dsp changes outside

	const int IVEC_DIAG_UP = 8;                     // start of diagonal up vectors
	const int IVEC_UP = 18;                     // up vector
	const int IVEC_DOWN = 19;                       // down vector

	const float DAS_REFLECTIVITY_NORM = 0.5F;
	const float DAS_REFLECTIVITY_SKY = 0.0F;

	const int DAS_CNODES = 40;                  // keep around last n nodes - must be same as DSP_CAUTO_PRESETS!!!

	static readonly DasNode[] g_das_nodes = new DasNode[DAS_CNODES];     // all dsp detection nodes
	static int g_pdas_last_node = -1;   // last node that saw player

	static int g_das_check_next;                    // next node to check
	static int g_das_store_next;                    // next place to store node
	static bool g_das_all_checked;                  // true if all nodes checked
	static int g_das_checked_count;             // count of nodes checked in latest pass

	static DasRoom g_das_room;                  // room detector

	public static bool g_bdas_room_init = false;
	public static bool g_bdas_init_nodes = false;
	static bool g_bdas_create_new_node = false;

	static readonly Vector3[] g_das_vec3 = new Vector3[DAS_CWALLS];  // trace vectors to walls, ceiling, floor

	static void DAS_InitNodes() {
		Array.Clear(g_das_nodes);
		g_das_check_next = 0;
		g_das_store_next = 0;
		g_das_all_checked = false;
		g_das_checked_count = 0;

		// init all rooms

		for (int i = 0; i < DAS_CNODES; i++)
			DAS_InitAutoRoom(ref g_das_nodes[i].room);

		// init trace vectors
		// set up trace vectors for max, min width
		float vl = DAS_ROOM_TRACE_LEN;
		float vlu = DAS_ROOM_TRACE_LEN * 0.52f;
		float vlu2 = DAS_ROOM_TRACE_LEN * 0.48f;    // don't use 'perfect' diagonals

		g_das_vec3[0] = new(vl, 0.0f, 0.0f);                // x left
		g_das_vec3[1] = new(-vl, 0.0f, 0.0f);               // x right

		g_das_vec3[2] = new(0.0f, vl, 0.0f);                // y front
		g_das_vec3[3] = new(0.0f, -vl, 0.0f);               // y back

		g_das_vec3[4] = new(-vlu, vlu2, 0.0f);          // diagonal front left
		g_das_vec3[5] = new(vlu, -vlu2, 0.0f);          // diagonal rear right

		g_das_vec3[6] = new(vlu, vlu2, 0.0f);               // diagonal front right
		g_das_vec3[7] = new(-vlu, -vlu2, 0.0f);         // diagonal rear left

		// set up trace vectors for max height - on x=y diagonal

		g_das_vec3[8] = new(vlu, vlu2, vlu / 2.0F);         // front right up A x,y,z/2		(IVEC_DIAG_UP)
		g_das_vec3[9] = new(vlu, vlu2, vlu);                // front right up B x,y,z
		g_das_vec3[10] = new(vlu / 2.0F, vlu2 / 2.0F, vlu); // front right up C x/2,y/2,z

		g_das_vec3[11] = new(-vlu, -vlu2, vlu / 2.0F);      // rear left up A -x,-y,z/2
		g_das_vec3[12] = new(-vlu, -vlu2, vlu);         // rear left up B -x,-y,z
		g_das_vec3[13] = new(-vlu / 2.0F, -vlu2 / 2.0F, vlu);   // rear left up C -x/2,-y/2,z

		// set up trace vectors for max height - on x axis & y axis

		g_das_vec3[14] = new(-vlu, 0, vlu);             // left up B -x,0,z
		g_das_vec3[15] = new(0, vlu / 2.0F, vlu);           // front up C -x/2,0,z

		g_das_vec3[16] = new(0, -vlu, vlu);             // rear up B x,0,z
		g_das_vec3[17] = new(vlu / 2.0F, 0, vlu);           // right up C x/2,0,z

		g_das_vec3[18] = new(0.0f, 0.0f, vl);               // up	(IVEC_UP)
		g_das_vec3[19] = new(0.0f, 0.0f, -vl);              // down (IVEC_DOWN)
	}

	static void DAS_InitAutoRoom(ref DasRoom proom) {
		proom = default;
	}

	// reset all nodes for next round of visibility checks between player & nodes

	static void DAS_ResetNodes() {
		for (int i = 0; i < DAS_CNODES; i++) {
			g_das_nodes[i].fseesplayer = false;
			g_das_nodes[i].dist = 0;
		}

		g_das_all_checked = false;
		g_das_checked_count = 0;
		g_bdas_create_new_node = false;
	}

	// utility function - return next index, wrap at max

	static int DAS_GetNextIndex(ref int pindex, int max) {
		int i = pindex;
		int j;

		j = i + 1;
		if (j >= max)
			j = 0;

		pindex = j;

		return i;
	}

	// returns true if dsp node is within range of player

	static bool DAS_NodeInRange(ref DasRoom proom, ref DasNode pnode) {
		float dist;

		dist = (proom.vplayer - pnode.vplayer).Length();

		// player can still see previous room selection point, and it's less than n feet away,
		// then flag this node as visible

		pnode.dist = (int)dist;

		return dist <= pnode.range_max;
	}

	// update next valid node - set up internal node state if it can see player
	// called once per frame
	// returns true if all nodes have been checked

	static bool DAS_CheckNextNode(ref DasRoom proom) {
		int i, j;

		if (g_das_all_checked)
			return true;

		// find next valid node

		for (j = 0; j < DAS_CNODES; j++) {
			// track number of nodes checked

			g_das_checked_count++;

			// get next node in range to check

			i = DAS_GetNextIndex(ref g_das_check_next, DAS_CNODES);

			if (g_das_nodes[i].fused && DAS_NodeInRange(ref proom, ref g_das_nodes[i])) {
				// trace to see if player can still see node,
				// if so stop checking

				if (DAS_TraceNodeToPlayer(ref proom, ref g_das_nodes[i]))
					goto checknode_exit;
			}
		}

	checknode_exit:

		// flag that all nodes have been checked

		if (g_das_checked_count >= DAS_CNODES)
			g_das_all_checked = true;

		return g_das_all_checked;
	}


	static int DAS_GetNextNodeIndex() {
		return g_das_store_next;
	}
	// store new node for room

	static void DAS_StoreNode(ref DasRoom proom, int dsp_preset) {
		// overwrite node in cyclic list

		int i = DAS_GetNextIndex(ref g_das_store_next, DAS_CNODES);

		g_das_nodes[i].dsp_preset = (short)dsp_preset;
		g_das_nodes[i].fused = true;
		g_das_nodes[i].vplayer = proom.vplayer;

		// calculate node scanning range_max based on room size

		if (!proom.bskyabove) {
			// inside range - halls & tunnels have nodes every 5*width
			g_das_nodes[i].range_max = (int)MathF.Min((int)DAS_DIST_MAX, Math.Min(proom.width_max * 5, proom.length_max));
			g_das_nodes[i].range_min = (int)DAS_DIST_MIN;
		}
		else {
			// outside range
			g_das_nodes[i].range_max = (int)DAS_DIST_MAX_OUTSIDE;
			g_das_nodes[i].range_min = (int)DAS_DIST_MIN_OUTSIDE;
		}

		g_das_nodes[i].fseesplayer = false;
		g_das_nodes[i].dist = 0;

		g_das_nodes[i].room = proom;

		// update last node visible as this node

		g_pdas_last_node = i;
	}

	// check all updated nodes,
	// return dsp_preset of largest node (by area) that can see player
	// return -1 if no preset found

	// NOTE: outside nodes can't see player if player is inside and vice versa
	// foutside is true if player is outside

	static int DAS_GetDspPreset(bool foutside) {
		int dsp_preset = -1;

		int i;
		// int dist_min = 100000;
		int area_max = 0;
		int area;

		// find node that represents room with greatest floor area, return its preset.

		for (i = 0; i < DAS_CNODES; i++) {
			if (g_das_nodes[i].fused && g_das_nodes[i].fseesplayer) {
				area = (g_das_nodes[i].room.width_max * g_das_nodes[i].room.length_max);

				if (g_das_nodes[i].room.bskyabove == foutside) {
					if (area > area_max) {
						area_max = area;
						dsp_preset = g_das_nodes[i].dsp_preset;

						// save pointer to last node that saw player

						g_pdas_last_node = i;
					}
				}
				/*

							// find nearest node, return its preset

							if (g_das_nodes[i].dist < dist_min)
							{
								if ( g_das_nodes[i].room.bskyabove == foutside )
								{
									dist_min = g_das_nodes[i].dist;
									dsp_preset = g_das_nodes[i].dsp_preset;

									// save pointer to last node that saw player

									g_pdas_last_node = &(g_das_nodes[i]);

								}
							}
				*/
			}
		}

		return dsp_preset;
	}

	// custom trace filter:
	// a) never hit player or monsters or entities
	// b) always hit world, or moveables or static props

	struct TraceFilterDAS : ITraceFilter
	{
		public bool ShouldHitEntity(IHandleEntity? pHandleEntity, Contents contentsMask) {
			IClientUnknown? pUnk = pHandleEntity as IClientUnknown;
			IClientEntity? pEntity;

			if (pUnk == null)
				return false;

			// don't hit non-collideable props

			if (soundServices.IsStaticProp(pHandleEntity!)) {

				ICollideable? pCollide = soundServices.GetStaticProp(pHandleEntity!);
				if (pCollide == null)
					return false;
			}

			// don't hit any ents

			pEntity = pUnk.GetIClientEntity();

			if (pEntity != null)
				return false;

			return true;
		}

		public TraceType GetTraceType() {
			return TraceType.EverythingFilterProps;
		}
	}

	const Mask DAS_TRACE_MASK = (Mask)(Contents.Solid | Contents.Moveable | Contents.Window);

	// returns true if clear line exists between node and player
	// if node can see player, sets up node distance and flag fseesplayer

	static bool DAS_TraceNodeToPlayer(ref DasRoom proom, ref DasNode pnode) {
		TraceFilterDAS filterP = new();
		bool fseesplayer = false;
		float dist;
		Ray ray = default;
		ray.Init(proom.vplayer, pnode.vplayer);

		g_pEngineTraceClient.TraceRay(in ray, DAS_TRACE_MASK, ref filterP, out Trace trP);
		dist = (proom.vplayer - pnode.vplayer).Length();

		// player can still see previous room selection point, and it's less than n feet away,
		// then flag this node as visible

		if (!trP.DidHit() && (dist <= DAS_DIST_MAX)) {
			fseesplayer = true;
			pnode.dist = (int)dist;
		}

		pnode.fseesplayer = fseesplayer;

		return fseesplayer;
	}

	// update room boundary maxs, mins

	static void DAS_SetRoomBounds(ref DasRoom proom, in Vector3 hit, bool bheight) {
		Vector3 maxs, mins;

		maxs = proom.room_maxs;
		mins = proom.room_mins;

		if (!bheight) {
			if (hit.X > maxs.X)
				maxs.X = hit.X;

			if (hit.X < mins.X)
				mins.X = hit.X;

			if (hit.Z > maxs.Z)
				maxs.Z = hit.Z;

			if (hit.Z < mins.Z)
				mins.Z = hit.Z;
		}

		if (bheight) {
			if (hit.Y > maxs.Y)
				maxs.Y = hit.Y;

			if (hit.Y < mins.Y)
				mins.Y = hit.Y;
		}

		proom.room_maxs = maxs;
		proom.room_mins = mins;
	}

	// when all walls are updated, calculate max length, width, height, reflectivity, sky hit%, room center
	// returns true if room parameters are in good location to place a node
	// returns false if room parameters are not in good location to place a node
	// note: false occurs if up vector doesn't hit sky, but one or more up diagonal vectors do hit sky

	static bool DAS_CalcRoomProps(ref DasRoom proom) {
		int length_max = 0;
		int width_max = 0;
		int height_max = 0;
		Span<int> dist = stackalloc int[4];
		float area1, area2;
		int height;
		int i;
		int j;
		int k;
		bool b_diaghitsky = false;

		// reject this location if up vector doesn't hit sky, but
		// one or more up diagonals do hit sky -
		// in this case, player is under a slight overhang, narrow bridge, or
		// standing just inside a window or doorway. keep looking for better node location

		for (i = IVEC_DIAG_UP; i < IVEC_UP; i++) {
			if (proom.skyhits[i] > 0.0)
				b_diaghitsky = true;
		}

		if (b_diaghitsky && !(proom.skyhits[IVEC_UP] > 0.0))
			return false;

		// get all distance pairs

		for (i = 0; i < IVEC_DIAG_UP; i += 2)
			dist[i / 2] = proom.dist[i] + proom.dist[i + 1];    // 1st pair is width

		// if areas differ by more than 25%
		// select the pair with the greater area

		// if areas do not differ by more than 25%, select the pair with the
		// longer measured distance. Filters incorrect selection due to diagonals.

		area1 = (float)(dist[0] * dist[1]);
		area2 = (float)(dist[2] * dist[3]);

		area1 = (int)area1 == 0 ? 1.0F : area1;
		area2 = (int)area2 == 0 ? 1.0F : area2;

		if (PercentDifference(area1, area2) > 0.25F) {
			// areas are more than 25% different - select pair with greater area

			j = area1 > area2 ? 0 : 2;
		}
		else {
			// select pair with longer measured distance

			int iMaxDist = 0; // index to max dist
			int dmax = 0;

			for (i = 0; i < 4; i++) {
				if (dist[i] > dmax) {
					dmax = dist[i];
					iMaxDist = i;
				}
			}

			j = iMaxDist > 1 ? 2 : 0;
		}


		// width is always the smaller of the dimensions

		width_max = Math.Min(dist[j], dist[j + 1]);
		length_max = Math.Max(dist[j], dist[j + 1]);

		// get max height

		for (i = IVEC_DIAG_UP; i < IVEC_DOWN; i++) {
			height = proom.dist[i];

			if (height > height_max)
				height_max = height;
		}

		proom.length_max = length_max;
		proom.width_max = width_max;
		proom.height_max = height_max;

		// get room max,min from chosen width, depth
		// 0..3 or 4..7

		for (i = j * 2; i < 4 + (j * 2); i++)
			DAS_SetRoomBounds(ref proom, proom.hit[i], false);

		// get room height min from down trace

		proom.room_mins.Z = proom.hit[IVEC_DOWN].Z;

		// reset room height max to player trace height

		proom.room_maxs.Z = proom.vplayer.Z;

		// draw box around room max,min

		if (das_debug.GetInt() == 6) {
			// draw box around all objects detected
			Vector3 maxs = proom.room_maxs;
			Vector3 mins = proom.room_mins;
			Vector3 orig = (maxs + mins) / 2.0f;
			Vector3 absMax = maxs - orig;
			Vector3 absMin = mins - orig;

			debugoverlay?.AddBoxOverlay(orig, absMax, absMin, vec3_angle, 255, 0, 255, 0, 60.0f);
		}
		// calculate average reflectivity

		float refl = 0.0f;

		// average reflectivity for walls

		// 0..3 or 4..7

		for (k = 0, i = j * 2; i < 4 + (j * 2); i++, k++) {
			refl += proom.reflect[i];
			proom.refl_walls[k] = proom.reflect[i];
		}

		// assume ceiling is open

		proom.refl_walls[4] = 0.0f;

		// get ceiling reflectivity, if any non zero

		for (i = IVEC_DIAG_UP; i < IVEC_DOWN; i++) {
			if (proom.reflect[i] == 0.0) {
				// if any upward trace hit sky, exit;
				// ceiling reflectivity is 0.0

				proom.refl_walls[4] = 0.0f;

				i = IVEC_DOWN;  // exit loop
			}
			else {

				// upward trace didn't hit sky, keep checking

				proom.refl_walls[4] = proom.reflect[i];
			}
		}

		// add in ceiling reflectivity, if any

		refl += proom.refl_walls[4];

		// get floor reflectivity

		refl += proom.reflect[IVEC_DOWN];
		proom.refl_walls[5] = proom.reflect[IVEC_DOWN];

		proom.refl_avg = refl / 6.0F;

		// calculate sky hit percent for this wall

		float sky_pct = 0.0f;

		// 0..3 or 4..7

		for (i = j * 2; i < 4 + (j * 2); i++)
			sky_pct += proom.skyhits[i];

		for (i = IVEC_DIAG_UP; i < IVEC_DOWN; i++) {
			if (proom.skyhits[i] > 0.0) {
				// if any upward trace hit sky, exit loop
				sky_pct += proom.skyhits[i];
				i = IVEC_DOWN;
			}
		}

		// get floor skyhit

		sky_pct += proom.skyhits[IVEC_DOWN];

		proom.sky_pct = sky_pct;

		// check for sky above
		proom.bskyabove = false;

		for (i = IVEC_DIAG_UP; i < IVEC_DOWN; i++) {
			if (proom.skyhits[i] > 0.0)
				proom.bskyabove = true;
		}

		return true;
	}

	// return true if trace hit solid
	// return false if trace hit sky or didn't hit anything

	static bool DAS_HitSolid(in Trace ptr) {
		// if hit nothing return false

		if (!ptr.DidHit())
			return false;

		// if hit sky, return false (not solid)
		if ((ptr.Surface.Flags & (ushort)Surf.Sky) != 0)
			return false;

		return true;
	}

	// returns true if trace hit sky

	static bool DAS_HitSky(in Trace ptr) {
		if (ptr.DidHit() && (ptr.Surface.Flags & (ushort)Surf.Sky) != 0)
			return true;
		if (!ptr.DidHit()) {
			float dz = ptr.EndPos.Z - ptr.StartPos.Z;
			if (dz > 200 * 12.0f)
				return true;
		}
		return false;
	}


	static bool DAS_ScanningForHeight(ref DasRoom proom) {
		return proom.iwall >= IVEC_DIAG_UP;
	}

	static bool DAS_ScanningForWidth(ref DasRoom proom) {
		return proom.iwall < IVEC_DIAG_UP;
	}

	static bool DAS_ScanningForFloor(ref DasRoom proom) {
		return proom.iwall == IVEC_DOWN;
	}

	public static readonly ConVar das_door_height = new("adsp_door_height", "112"); // standard door height hl2
	public static readonly ConVar das_wall_height = new("adsp_wall_height", "128"); // standard wall height hl2
	public static readonly ConVar das_low_ceiling = new("adsp_low_ceiling", "108"); // low ceiling height hl2


	// set origin for tracing out to walls to point above player's head
	// allows calculations over walls and floor obstacles, and above door openings

	// WARNING: the current settings are optimal for skipping floor and ceiling clutter,
	// and for detecting rooms without 'looking' through doors or windows. Don't change these cvars for hl2!

	static void DAS_SetTraceHeight(ref DasRoom proom, in Trace ptrU, in Trace ptrD) {
		// NOTE: when tracing down through player's box, endpos and startpos are reversed and
		// startsolid and allsolid are true.

		int zup = (int)MathF.Abs(ptrU.EndPos.Z - ptrU.StartPos.Z);      // height above player's head
		int zdown = (int)MathF.Abs(ptrD.EndPos.Z - ptrD.StartPos.Z);        // distance to floor from player's head
		int h;
		h = zup + zdown;

		int door_height = das_door_height.GetInt();
		int wall_height = das_wall_height.GetInt();
		int low_ceiling = das_low_ceiling.GetInt();

		if (h > low_ceiling && h <= wall_height) {
			// low ceiling - trace out just above standard door height @ 112
			if (h > door_height)
				proom.vplayer.Z = MathF.Min(ptrD.EndPos.Z, ptrD.StartPos.Z) + door_height + 1;
			else
				proom.vplayer.Z = MathF.Min(ptrD.EndPos.Z, ptrD.StartPos.Z) + h - 1;
		}
		else if (h > wall_height) {
			// tall ceiling - trace out over standard walls @ 128

			proom.vplayer.Z = MathF.Min(ptrD.EndPos.Z, ptrD.StartPos.Z) + wall_height + 1;
		}
		else {
			// very low ceiling, trace out from just below ceiling
			proom.vplayer.Z = MathF.Min(ptrD.EndPos.Z, ptrD.StartPos.Z) + h - 1;
			proom.lowceiling = (short)h;
		}

		Assert(proom.vplayer.Z <= ptrU.EndPos.Z);

		if (das_debug.GetInt() > 1) {
			// draw line to height, and between floor and ceiling

			debugoverlay?.AddLineOverlay(ptrD.EndPos, ptrU.EndPos, 0, 255, 0, false, 20);

			Vector3 mins;
			Vector3 maxs;
			mins = new(-1, -1, -2.0f);
			maxs = new(1, 1, 0);

			debugoverlay?.AddBoxOverlay(proom.vplayer, mins, maxs, vec3_angle, 255, 0, 0, 0, 20);

			debugoverlay?.AddBoxOverlay(ptrU.EndPos, mins, maxs, vec3_angle, 0, 255, 0, 0, 20);
			debugoverlay?.AddBoxOverlay(ptrD.EndPos, mins, maxs, vec3_angle, 0, 255, 0, 0, 20);

		}
	}

	// prepare room struct for new round of checks:
	// clear out struct,
	// init trace height origin by finding space above player's head
	// returns true if player is in valid position to begin checks from

	static bool DAS_StartTraceChecks(ref DasRoom proom) {
		// starting new check: store player position, init maxs, mins

		proom.vplayer_eyes = soundServices.MainViewOrigin();
		proom.vplayer = soundServices.MainViewOrigin();

		proom.height_max = 0;
		proom.width_max = 0;
		proom.length_max = 0;
		proom.room_maxs = new(0.0f, 0.0f, 0.0f);
		proom.room_mins = new(10000.0f, 10000.0f, 10000.0f);

		proom.lowceiling = 0;

		// find point between player's head and ceiling - trace out to walls from here

		TraceFilterDAS filterU = new(), filterD = new();

		Vector3 v_dir = g_das_vec3[IVEC_DOWN];  // down - find floor

		Vector3 endpoint = proom.vplayer + v_dir;

		Ray ray = default;
		ray.Init(proom.vplayer, endpoint);

		g_pEngineTraceClient.TraceRay(in ray, DAS_TRACE_MASK, ref filterD, out Trace trD);

		// if player jumping or in air, don't continue

		if (trD.DidHit() && MathF.Abs(trD.EndPos.Z - trD.StartPos.Z) > 72)
			return false;

		v_dir = g_das_vec3[IVEC_UP];            // up - find ceiling

		endpoint = proom.vplayer + v_dir;

		ray.Init(proom.vplayer, endpoint);

		g_pEngineTraceClient.TraceRay(in ray, DAS_TRACE_MASK, ref filterU, out Trace trU);

		// if down trace hits floor, set trace height, otherwise default is player eye location

		if (DAS_HitSolid(in trD))
			DAS_SetTraceHeight(ref proom, in trU, in trD);

		return true;
	}

	static void DAS_DebugDrawTrace(in Trace ptr, int r, int g, int b, float duration, int imax) {

		// das_debug == 3: draw horizontal trace bars for room width/depth detection
		// das_debug == 4: draw upward traces for height detection

		if (das_debug.GetInt() != imax)
			return;

		debugoverlay?.AddLineOverlay(ptr.StartPos, ptr.EndPos, r, g, b, false, duration);

		Vector3 mins;
		Vector3 maxs;
		mins = new(-1, -1, -2.0f);
		maxs = new(1, 1, 0);

		debugoverlay?.AddBoxOverlay(ptr.EndPos, mins, maxs, vec3_angle, r, g, b, 0, duration);

	}

	// wall surface data

	struct DasSurfData
	{
		public float dist;              // distance to player
		public float reflectivity;      // acoustic reflectivity of material on surface
		public Vector3 hit;             // trace hit location
		public Vector3 norm;            // wall normal at hit location
	}

	// trace hit wall surface, get info about surface and store in surfdata struct
	// if scanning for height, bounce a second trace off of ceiling and get dist to floor

	static void DAS_GetSurfaceData(ref DasRoom proom, in Trace ptr, ref DasSurfData psurfdata) {

		float dist;             // distance to player
		float reflectivity;     // acoustic reflectivity of material on surface
		Vector3 hit;                // trace hit location
		Vector3 norm;           // wall normal at hit location
		SurfaceData_ptr? psurf;

		psurf = physprop?.GetSurfaceData(ptr.Surface.SurfaceProps);

		reflectivity = psurf != null ? psurf.Audio.Reflectivity : DAS_REFLECTIVITY_NORM;

		// keep wall hit location and normal, to calc room bounds and center

		norm = ptr.Plane.Normal;

		// get length to hit location

		dist = (ptr.EndPos - ptr.StartPos).Length();

		// if started tracing from within player box, startpos & endpos may be flipped

		if (ptr.EndPos.Z >= ptr.StartPos.Z)
			hit = ptr.EndPos;
		else
			hit = ptr.StartPos;

		// if checking for max height by bouncing several vectors off of ceiling:
		// ignore returned normal from 1st bounce, just search straight down from trace hit location

		if (DAS_ScanningForHeight(ref proom) && !DAS_ScanningForFloor(ref proom)) {
			TraceFilterDAS filter2 = new();

			norm = new(0.0f, 0.0f, -1.0f);

			Vector3 endpoint = hit + (norm * DAS_ROOM_TRACE_LEN);

			Ray ray = default;
			ray.Init(hit, endpoint);

			g_pEngineTraceClient.TraceRay(in ray, DAS_TRACE_MASK, ref filter2, out Trace tr2);

			//DAS_DebugDrawTrace( &tr2, 255, 255, 0, 10, 1);

			if (tr2.DidHit()) {
				// get distance between surfaces

				dist = (tr2.EndPos - tr2.StartPos).Length();
			}
		}

		// set up surface struct and return

		psurfdata.dist = dist;
		psurfdata.hit = hit;
		psurfdata.norm = norm;
		psurfdata.reflectivity = reflectivity;

	}


	// algorithm for detecting approximate size of space around player. Handles player in corner & non-axis aligned rooms.
	// also handles player on catwalk or player under small bridge/overhang.
	// The goal is to only change the dsp room description if the the player moves into
	// a space which is SIGNIFICANTLY different from the previously set dsp space.

	// save player position. find a point above player's head and trace out from here.

	// from player position, get max width and max length:

	// from player position,
	// a) trace x,-x, y,-y axes
	// b) trace xy, -xy, x-y, -x-y diagonals
	// c) select largest room size detected from max width, max length


	// from player position, get height
	// a) trace out along front-up (or left-up, back-up, right-up), save hit locations
	// b) trace down -z from hit locations
	// c) save max height

	// when max width, max length, max height all updated, get new player position

	// get average room size & wall materials:
	// update averages with one traceline per frame only
	// returns true if room is fully updated and ready to check

	static bool DAS_UpdateRoomSize(ref DasRoom proom) {
		Vector3 endpoint;
		Vector3 startpoint;
		Vector3 v_dir;
		int iwall;
		bool bskyhit = false;
		DasSurfData surfdata;

		// do nothing if room already fully checked

		if (proom.broomready)
			return true;

		// cycle through all walls, floor, ceiling
		// get wall index

		iwall = proom.iwall;

		// get height above player and init proom for new round of checks

		if (iwall == 0) {
			if (!DAS_StartTraceChecks(ref proom))
				return false;       // bad location to check room - player is jumping etc.
		}

		// get trace vector

		v_dir = g_das_vec3[iwall];

		// trace out from trace origin, in axis-aligned direction or along diagonals

		// if looking for max height, trace from top of player's eyes

		if (DAS_ScanningForHeight(ref proom)) {
			startpoint = proom.vplayer_eyes;
			endpoint = proom.vplayer_eyes + v_dir;
		}
		else {
			startpoint = proom.vplayer;
			endpoint = proom.vplayer + v_dir;
		}

		// try less expensive world-only trace first (no props, no ents - just try to hit walls)

		TraceFilterWorldOnly filter = new();

		Ray ray = default;
		ray.Init(startpoint, endpoint);

		g_pEngineTraceClient.TraceRay(in ray, (Mask)Contents.Solid, ref filter, out Trace tr);

		// if didn't hit world, or we hit sky when looking horizontally,
		// retrace, this time including props

		if (!DAS_HitSolid(in tr) && DAS_ScanningForWidth(ref proom)) {
			TraceFilterDAS filterDas = new();

			ray.Init(startpoint, endpoint);
			g_pEngineTraceClient.TraceRay(in ray, DAS_TRACE_MASK, ref filterDas, out tr);
		}

		if (das_debug.GetInt() > 2) {
			// draw trace lines

			if (DAS_HitSolid(in tr))
				DAS_DebugDrawTrace(in tr, 0, 255, 255, 10, (DAS_ScanningForHeight(ref proom) ? 1 : 0) + 3);
			else
				DAS_DebugDrawTrace(in tr, 255, 0, 0, 10, (DAS_ScanningForHeight(ref proom) ? 1 : 0) + 3);   // red lines if sky hit or no hit
		}

		// init surface data with defaults, in case we didn't hit world

		surfdata.dist = DAS_ROOM_TRACE_LEN;
		surfdata.reflectivity = DAS_REFLECTIVITY_SKY;   // assume sky or open area
		surfdata.hit = endpoint;                // trace hit location
		surfdata.norm = -v_dir;

		// check for sky hits

		if (DAS_HitSky(in tr)) {
			bskyhit = true;

			if (DAS_ScanningForWidth(ref proom))
				// ignore horizontal sky hits for distance calculations
				surfdata.dist = 1.0f;
			else
				surfdata.dist = surfdata.dist; // debug
		}

		// get length of trace if it hit world

		// if hit solid and not sky (tr.DidHit() && !bskyhit)
		// get surface information

		if (DAS_HitSolid(in tr))
			DAS_GetSurfaceData(ref proom, in tr, ref surfdata);

		// store surface data

		proom.dist[iwall] = (int)surfdata.dist;
		proom.reflect[iwall] = Math.Clamp(surfdata.reflectivity, 0.0f, 1.0f);
		proom.skyhits[iwall] = bskyhit ? 0.1F : 0.0F;
		proom.hit[iwall] = surfdata.hit;
		proom.norm[iwall] = surfdata.norm;

		// update wall counter

		proom.iwall++;

		if (proom.iwall == DAS_CWALLS) {
			bool b_good_node_location;

			// calculate room mins, maxs, reflectivity etc

			b_good_node_location = DAS_CalcRoomProps(ref proom);

			// reset wall counter

			proom.iwall = 0;
			proom.broomready = b_good_node_location;    // room ready to check if good node location

			return b_good_node_location;
		}

		return false;           // room not yet fully updated
	}

	// determine # of solid ents/props within detected room boundaries
	// and set diffusion based on count of ents and spatial volume of ents

	static void DAS_SetDiffusion(ref DasRoom proom) {
		// BRJ 7/12/05
		// This was commented out because the y component of proom->room_mins, proom->room_maxs was never
		// being computed, causing a bogus box to be sent to the partition system. The results of
		// this computation (namely the diffusion + ent_count fields of das_room_t) were never being used.
		// Therefore, we'll avoid the enumeration altogether

		proom.diffusion = 0.0f;
		proom.ent_count = 0;
	}

	// debug routine to display current room params

	static void DAS_DisplayRoomDEBUG(ref DasRoom proom, bool fnew, float preset) {
		float dx, dy, dz;
		float count;

		if (das_debug.GetInt() == 0)
			return;

		dx = proom.length_max / 12.0F;
		dy = proom.width_max / 12.0F;
		dz = proom.height_max / 12.0F;

		float refl = proom.refl_avg;

		count = (float)(proom.ent_count);
		float fsky = (proom.bskyabove ? 1.0F : 0.0F);

		if (fnew)
			DevMsg($"NEW DSP NODE: size:({dx:F0},{dy:F0}) height:({dz:F0}) dif {proom.diffusion:F4} : refl {refl:F4} : cobj: {count:F0} : sky {fsky:F0} \n");

		if (!fnew && preset < 0.0)
			return;

		if (preset >= 0.0) {
			DevMsg($"DSP PRESET: {preset:F0} size:({dx:F0},{dy:F0}) height:({dz:F0}) dif {proom.diffusion:F4} : refl {refl:F4} : cobj: {count:F0} : sky {fsky:F0} \n");
			return;
		}

		// draw box around new node location

		Vector3 mins;
		Vector3 maxs;
		mins = new(-8, -8, -16);
		maxs = new(8, 8, 0);

		debugoverlay?.AddBoxOverlay(proom.vplayer, mins, maxs, vec3_angle, 0, 0, 255, 0, 1000.0f);

		// draw red box around node origin

		mins = new(-0.5f, -0.5f, -1.0f);
		maxs = new(0.5f, 0.5f, 0);

		debugoverlay?.AddBoxOverlay(proom.vplayer, mins, maxs, vec3_angle, 255, 0, 0, 0, 1000.0f);

		debugoverlay?.AddTextOverlay(proom.vplayer, 0, 10, "DSP NODE");
	}

	// check newly calculated room parameters against current stored params.
	// if different, return true.
	// NOTE: only call when all proom params have been calculated.
	// return false if this is not a good location for creating a new node

	static bool DAS_CheckNewRoom(ref DasRoom proom) {
		bool bnewroom;
		float dw, dw2, dr, ds, dh;
		int cchanged = 0;
		Vector2 v2d;
		Vector3 v3d;
		float dist;

		// player can't see previous node, determine if this is a good place to lay down
		// a new node.  Get room at last seen node for comparison

		// no previous room node saw player, go create new room node

		if (g_pdas_last_node < 0) {
			bnewroom = true;
			goto check_ret;
		}

		ref DasRoom proom_prev = ref g_das_nodes[g_pdas_last_node].room;

		// if player not at least n feet from last node, return false

		v3d = proom.vplayer - proom_prev.vplayer;
		v2d = new(v3d.X, v3d.Y);

		dist = v2d.Length();

		if (dist <= DAS_DIST_MIN)
			return false;

		// see if room size has changed significantly since last node

		bnewroom = true;

		dw = 0.0f;
		dw2 = 0.0f;
		dh = 0.0f;
		dr = 0.0f;

		if (proom_prev.width_max != 0)
			dw = (float)proom.width_max / (float)proom_prev.width_max;  // max width delta

		if (proom_prev.length_max != 0)
			dw2 = (float)proom.length_max / (float)proom_prev.length_max;   // max length delta

		if (proom_prev.height_max != 0)
			dh = (float)proom.height_max / (float)proom_prev.height_max;    // max height delta

		if (proom_prev.refl_avg != 0.0)
			dr = proom.refl_avg / proom_prev.refl_avg;                  // reflectivity delta

		ds = MathF.Abs(proom.sky_pct - proom_prev.sky_pct);                 // sky hits delta

		if (dw > 1.0F) dw = 1.0F / dw;
		if (dw2 > 1.0F) dw = 1.0F / dw2;
		if (dh > 1.0F) dh = 1.0F / dh;
		if (dr > 1.0F) dr = 1.0F / dr;

		if ((1.0F - dw) >= DAS_WIDTH_MIN)
			cchanged++;

		if ((1.0F - dw2) >= DAS_WIDTH_MIN)
			cchanged++;

		//	if ( (1.0 - dh) >= DAS_WIDTH_MIN )	// don't change room based on height change
		//		cchanged++;

		// new room only if at least 1 changed

		if (cchanged >= 1)
			goto check_ret;

		//	if ( (1.0 - dr) >= DAS_REFL_MIN )	// don't change room based on reflectivity change
		//		goto check_ret;

		//	if (ds >= DAS_SKYHIT_MIN )
		//		goto check_ret;

		// new room if sky above changes state

		if (proom.bskyabove != proom_prev.bskyabove)
			goto check_ret;

		// room didn't change significantly, return false

		bnewroom = false;

	check_ret:

		if (bnewroom) {
			// if low ceiling detected < 112 units, and max height is > low ceiling height by 20%, discard - no change
			// this detects player in doorway, under pipe or narrow bridge

			if (proom.lowceiling != 0 && (proom.lowceiling < proom.height_max)) {
				float h = (float)(proom.lowceiling) / (float)proom.height_max;

				if (h < 0.8)
					return false;
			}

			DAS_SetDiffusion(ref proom);
		}

		DAS_DisplayRoomDEBUG(ref proom, bnewroom, -1.0f);

		return bnewroom;
	}

	// select new dsp_room based on size, wall materials
	// (or modulate params for current dsp)
	// returns new preset # for dsp_automatic

	static int DAS_GetRoomDSP(ref DasRoom proom, int inode) {

		// preset constructor
		// call dsp module with params, get dsp preset back

		bool bskyabove = proom.bskyabove;
		int width = proom.width_max;
		int length = proom.length_max;
		int height = proom.height_max;
		float fdiffusion = proom.diffusion;
		float freflectivity = proom.refl_avg;
		Span<float> surf_refl = stackalloc float[6];

		// fill array of surface reflectivities - for left,right,front,back,ceiling,floor

		for (int i = 0; i < 6; i++)
			surf_refl[i] = proom.refl_walls[i];

		return DSP_ConstructPreset(bskyabove, width, length, height, fdiffusion, freflectivity, surf_refl, inode, DAS_CNODES);

	}


	// main entry point: call once per frame to update dsp_automatic
	// for automatic room detection.  dsp_room must be set to DSP_AUTOMATIC to enable.
	// NOTE: this routine accumulates traceline information over several frames - it
	// never traces more than 3 times per call, and normally just once per call.

	static void DAS_CheckNewRoomDSP() {
		ref DasRoom proom = ref g_das_room;
		int dsp_preset;
		bool bRoom_ready = false;

		// if listener has not been updated, do nothing

		if ((listener_origin == vec3_origin) &&
			(listener_forward == vec3_origin) &&
			(listener_right == vec3_origin) &&
			(listener_up == vec3_origin))
			return;

		if (!SND_IsInGame())
			return;

		// make sure we init nodes & vectors first time this is called

		if (!g_bdas_init_nodes) {
			g_bdas_init_nodes = true;
			DAS_InitNodes();
		}

		if (!DSP_CheckDspAutoEnabled()) {
			// make sure room params are reinitialized each time autoroom is selected

			g_bdas_room_init = false;
			return;
		}

		if (!g_bdas_room_init) {
			g_bdas_room_init = true;

			DAS_InitAutoRoom(ref proom);
		}

		// get time

		double dtime = soundServices.GetHostTime();

		// compare to previous time - don't check for new room until timer expires
		// ie: wait at least DAS_AUTO_WAIT seconds between preset changes

		if (Math.Abs(dtime - proom.last_dsp_change) < DAS_AUTO_WAIT)
			return;

		// first, update room size parameters, see if room is ready to check - if room is updated, return true right away

		// 3 traces per frame while accumulating room size info

		for (int i = 0; i < 3; i++)
			bRoom_ready = DAS_UpdateRoomSize(ref proom);

		if (!bRoom_ready)
			return;


		if (!g_bdas_create_new_node) {
			// next, check all nodes for line of sight to player - if all checked, return true right away

			if (!DAS_CheckNextNode(ref proom)) {
				// check all nodes first

				return;
			}

			// find out if any previously stored nodes can see player,
			// if so, get closest node's dsp preset

			dsp_preset = DAS_GetDspPreset(proom.bskyabove);

			if (dsp_preset != -1) {
				// an existing node can see player - just set preset and return

				if (dsp_preset != dsp_room_GetInt()) {
					// changed preset, so update timestamp

					proom.last_dsp_change = soundServices.GetHostTime();

					if (g_pdas_last_node >= 0)
						DAS_DisplayRoomDEBUG(ref g_das_nodes[g_pdas_last_node].room, false, (float)dsp_preset);
				}

				DSP_SetDspAuto(dsp_preset);

				goto check_new_room_exit;
			}
		}

		g_bdas_create_new_node = true;

		// no nodes can see player, need to try to create a new one

		// check for 'new' room around player

		if (DAS_CheckNewRoom(ref proom)) {
			// new room found - update dsp_automatic

			dsp_preset = DAS_GetRoomDSP(ref proom, DAS_GetNextNodeIndex());

			DSP_SetDspAuto(dsp_preset);

			// changed preset, so update timestamp

			proom.last_dsp_change = soundServices.GetHostTime();

			// save room as new node

			DAS_StoreNode(ref proom, dsp_preset);

			goto check_new_room_exit;
		}

	check_new_room_exit:

		// reset new node creation flag - start checking for visible nodes again

		g_bdas_create_new_node = false;

		// reset room checking flag - start checking room around player again

		proom.broomready = false;

		// reset node checking flag - start checking nodes around player again

		DAS_ResetNodes();

		return;
	}
}
