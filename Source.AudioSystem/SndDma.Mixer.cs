using Source.Common;
using Source.Common.Audio;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Filesystem;

namespace Source.AudioSystem;

//------------------------------------------------------------------------------
//
// Sound Mixers
//
// Sound mixers are referenced by name from Soundscapes, and are used to provide
// custom volume control over various sound categories, called 'mix groups'
//
// see scripts/soundmixers.txt for data format
//------------------------------------------------------------------------------

public static partial class SndDma
{
	const int CMXRGROUPMAX = 64;                    // up to n mixgroups
	const int CMXRGROUPRULESMAX = CMXRGROUPMAX + 16;    // max number of group rules
	const int CMXRSOUNDMIXERSMAX = 32;                  // up to n sound mixers per project

	// mix groups - these equivalent to submixes on an audio mixer

	// list of rules for determining sound membership in mix groups.
	// All conditions which are not null are ANDed together
	const int CMXRCLASSMAX = 16;
	const int CMXRNAMEMAX = 32;

	struct ClassListElem
	{
		public string szclassname;  // name of entities' class, such as CAI_BaseNPC or CHL2_Player
	}


	struct GroupRule
	{
		public string szmixgroup;       // mix group name
		public int mixgroupid;                  // mix group unique id
		public string szdir;            // substring to search for in ch->sfx
		public int classId;                 // index of classname
		public int chantype;                    // channel type (CHAN_WEAPON, etc)
		public int soundlevel_min;              // min soundlevel
		public int soundlevel_max;              // max soundlevel

		public int priority;                    // 0..100 higher priority sound groups duck all lower pri groups if enabled
		public int is_ducked;                   // if 1, sound group is ducked by all higher priority 'causes_duck" sounds
		public int causes_ducking;              // if 1, sound group ducks other 'is_ducked' sounds of lower priority
		public float duck_target_pct;           // if sound group is ducked, target percent of original volume

		public float total_vol;                 // total volume of all sounds in this group, if group can cause ducking
		public float ducker_threshold;          // ducking is caused by this group if total_vol > ducker_threshold
												// and causes_ducking is enabled.
		public float duck_target_vol;           // target volume while ducking
		public float duck_ramp_val;             // current value of ramp - moves towards duck_target_vol
	}

	// sound mixer

	struct SoundMixer
	{
		public string szsoundmixer;                 // name of this soundmixer
		public float[] mapMixgroupidToValue;            // sparse array of mix group values for this soundmixer
	}

	static readonly int[] g_mapMixgroupidToGrouprulesid = new int[CMXRGROUPMAX];                // map mixgroupid (one per unique group name)
																								// back to 1st entry of this name in g_grouprules

	// sound mixer globals

	static readonly ClassListElem[] g_groupclasslist = new ClassListElem[CMXRCLASSMAX];
	static readonly SoundMixer[] g_soundmixers = new SoundMixer[CMXRSOUNDMIXERSMAX];  // all sound mixers
	static readonly GroupRule[] g_grouprules = new GroupRule[CMXRGROUPRULESMAX];   // all rules for determining mix group membership


	// set current soundmixer index g_isoundmixer, search for match in soundmixers
	// Only change current soundmixer if new name is different from current name.

	static int g_isoundmixer = -1;                              // index of current sound mixer
	static string g_szsoundmixer_cur = "";                      // current soundmixer name

	public static readonly ConVar snd_soundmixer = new("snd_soundmixer", "Default_Mix");      // current soundmixer name


	public static void MXR_SetCurrentSoundMixer(ReadOnlySpan<char> szsoundmixer) {
		// if soundmixer name is not different from current name, return

		if (stricmp(szsoundmixer, g_szsoundmixer_cur) == 0)
			return;

		for (int i = 0; i < g_csoundmixers; i++) {
			if (stricmp(g_soundmixers[i].szsoundmixer, szsoundmixer) == 0) {
				g_isoundmixer = i;

				// save new current sound mixer name
				g_szsoundmixer_cur = new(szsoundmixer);

				return;
			}
		}
	}

	public static readonly ConVar snd_showclassname = new("snd_showclassname", "0");      // if 1, show classname of ent making sound
																						   // if 2, show all mixgroup matches
																						   // if 3, show all mixgroup matches with current soundmixer for ent
	// get the client class name if an entity was specified
	static ReadOnlySpan<char> GetClientClassname(int soundsource) {
		IClientEntity? pClientEntity = null;
		IClientEntityList? entitylist = OptionalSingleton<IClientEntityList>();
		if (entitylist != null) {
			pClientEntity = entitylist.GetClientEntity(soundsource);
			if (pClientEntity != null) {
				ClientClass? pClientClass = pClientEntity.GetClientClass();
				// check npc sounds
				if (pClientClass != null)
					return pClientClass.GetName();
			}
		}

		return null;
	}

	// builds a cached list of rules that match the directory name on the sound
	static int MXR_GetMixGroupListFromDirName(ReadOnlySpan<char> pDirname, byte[] pList, int listMax) {
		// if we call this before the groups are parsed we'll get bad data
		Assert(g_cgrouprules > 0);
		int count = 0;
		for (int i = 0; i < listMax; i++)
			pList[i] = 255;

		pDirname = pDirname.SliceNullTerminatedString();
		for (int i = 0; i < g_cgrouprules; i++) {
			ref GroupRule prule = ref g_grouprules[i];
			if (!string.IsNullOrEmpty(prule.szdir) && pDirname.Contains(prule.szdir, StringComparison.OrdinalIgnoreCase)) {
				pList[count] = (byte)i;
				count++;
				if (count >= listMax)
					return count;
			}
		}
		return count;
	}


	// determine which mixgroups sound is in, and save those mixgroupids in sound.
	// use current soundmixer indicated with g_isoundmixer, and contents of g_rgpgrouprules.
	// Algorithm:
	//		1. all conditions in a row are AND conditions,
	//		2. all rows sharing the same groupname are OR conditions.
	// so - if a sound matches all conditions of a row, it is given that row's mixgroup id
	//		if a sound doesn't match all conditions of a row, the next row is checked.

	// returns 0, default mixgroup if no match
	static void MXR_GetMixGroupFromSoundsource(Channel pchan, int soundsource, SoundLevel soundlevel) {
		int i;
		bool fmatch;
		Span<bool> classMatch = stackalloc bool[CMXRCLASSMAX];

		// init all mixgroups for channel
		for (i = 0; i < 8; i++)
			pchan.MixGroups[i] = -1;

		Span<char> sndnameBuf = stackalloc char[MAX_OSPATH];
		strcpy(sndnameBuf, pchan.Sfx!.GetName());
		// Use forward slashes here
		FixSlashesForward(sndnameBuf);
		ReadOnlySpan<char> sndname = sndnameBuf.SliceNullTerminatedString();
		ReadOnlySpan<char> pszclassname = GetClientClassname(soundsource);

		for (i = 0; i < g_cgroupclass; i++) {
			classMatch[i] = false;
			if (!pszclassname.IsEmpty && pszclassname.Contains(g_groupclasslist[i].szclassname, StringComparison.OrdinalIgnoreCase))
				classMatch[i] = true;
		}

		if (snd_showclassname.GetInt() == 1) {
			// utility: show classname of ent making sound

			if (!pszclassname.IsEmpty)
				DevMsg($"({pszclassname}:{sndname}) \n");
		}

		// check all group rules for a match, save
		// up to 8 matches in channel mixgroup.

		int cmixgroups = 0;
		if (!pchan.Sfx.MixGroupsCached)
			pchan.Sfx.OnNameChanged(pchan.Sfx.GetName());

		// since this is a sorted list (in group rule order) we only need to test against the next matching rule
		// this avoids a search inside the loop
		int currentDirRuleIndex = 0;
		int currentDirRule = pchan.Sfx.MixGroupList[0];

		for (i = 0; i < g_cgrouprules; i++) {
			ref GroupRule prule = ref g_grouprules[i];
			fmatch = true;

			// check directory or name substring
#if DEBUG
			// check dir table is correct in CSfxTable cache
			if (!string.IsNullOrEmpty(prule.szdir) && sndname.Contains(prule.szdir, StringComparison.OrdinalIgnoreCase))
				Assert(currentDirRule == i);
			else
				Assert(currentDirRule != i);
			if (prule.classId >= 0) {
				// rule has a valid class id and table is correct
				Assert(prule.classId < g_cgroupclass);
				if (!pszclassname.IsEmpty && pszclassname.Contains(g_groupclasslist[prule.classId].szclassname, StringComparison.OrdinalIgnoreCase))
					Assert(classMatch[prule.classId] == true);
				else
					Assert(classMatch[prule.classId] == false);
			}
#endif
			// this is the next matching dir for this sound, no need to search
			// becuse the list is sorted and we visit all elements
			if (currentDirRule == i) {
				Assert(!string.IsNullOrEmpty(prule.szdir));
				currentDirRuleIndex++;
				currentDirRule = 255;
				if (currentDirRuleIndex < pchan.Sfx.MixGroupCount)
					currentDirRule = pchan.Sfx.MixGroupList[currentDirRuleIndex];
			}
			else if (!string.IsNullOrEmpty(prule.szdir))
				fmatch = false; // substring doesn't match, keep looking

			// check class name

			if (fmatch && prule.classId >= 0)
				fmatch = classMatch[prule.classId];

			// check channel type

			if (fmatch && prule.chantype >= 0) {
				if (pchan.EntChannel != prule.chantype)
					fmatch = false; // channel type doesn't match, keep looking
			}

			// check sndlvlmin/max

			if (fmatch && prule.soundlevel_min >= 0) {
				if ((int)soundlevel < prule.soundlevel_min)
					fmatch = false; // soundlevel is less than min, keep looking
			}

			if (fmatch && prule.soundlevel_max >= 0) {
				if ((int)soundlevel > prule.soundlevel_max)
					fmatch = false; // soundlevel is greater than max, keep looking
			}

			if (fmatch) {
				pchan.MixGroups[cmixgroups] = prule.mixgroupid;
				cmixgroups++;
				if (cmixgroups >= 8)
					return;     // too many matches, stop looking
			}

			if (fmatch && snd_showclassname.GetInt() >= 2) {
				// show all mixgroups for this sound
				if (cmixgroups == 1)
					DevMsg($"\n{g_szsoundmixer_cur}:{sndname}: ");
				if (!string.IsNullOrEmpty(prule.szmixgroup)) {
					//	int rgmixgroupid[8];
					//	for (int i = 0; i < 8; i++)
					//		rgmixgroupid[i] = -1;
					//	rgmixgroupid[0] = prule->mixgroupid;
					//	float vol = MXR_GetVolFromMixGroup( rgmixgroupid );
					//	DevMsg("%s(%1.2f) ", prule->szmixgroup, vol);
					DevMsg($"{prule.szmixgroup} ");
				}
			}
		}
	}

	struct DebugShowVols
	{
		public string? psz;         // group name
		public int mixgroupid;  // groupid
		public float vol;           // group volume
		public float totalvol;      // total volume of all sounds playing in this group
	}


	// display routine for MXR_DebugShowMixVolumes

	const float MXR_DEBUG_INCY = 1.0F / 40.0F;          // vertical text spacing
	const float MXR_DEBUG_GREENSTART = 0.3F;            // start position on screen of bar

	const float MXR_DEBUG_MAXVOL = 1.0F;            // max volume scale
	const float MXR_DEBUG_REDLIMIT = 1.0F;          // volume limit into yellow
	const float MXR_DEBUG_YELLOWLIMIT = 0.7F;           // volume limit into red

	const int MXR_DEBUG_VOLSCALE = 48;              // length of graph in characters
	const char MXR_DEBUG_CHAR = '-';            // bar character

	static int g_debug_mxr_displaycount = 0;

	static void MXR_DebugGraphMixVolumes(Span<DebugShowVols> groupvols, int cgroups) {
		float flXpos, flYpos, flXposBar, duration;
		int r, g, b, a;
		int rb, gb, bb, ab;
		flXpos = 0;
		flYpos = 0;
		string text;
		string bartext;

		duration = 0.01f;

		g_debug_mxr_displaycount++;

		if ((g_debug_mxr_displaycount % 10) == 0)
			return;     // only display every 10 frames


		r = 96; g = 86; b = 226; a = 255; ab = 255;

		// show volume, dsp_volume

		text = $"Game Volume: {volume.GetFloat():F2}";
		debugoverlay?.AddScreenTextOverlay(flXpos, flYpos, duration, r, g, b, a, text);
		flYpos += MXR_DEBUG_INCY;

		text = $"DSP Volume: {dsp_volume.GetFloat():F2}";
		debugoverlay?.AddScreenTextOverlay(flXpos, flYpos, duration, r, g, b, a, text);
		flYpos += MXR_DEBUG_INCY;

		for (int i = 0; i < cgroups; i++) {
			// r += 64; g += 64; b += 16;

			r = r % 255; g = g % 255; b = b % 255;

			text = $"{groupvols[i].psz}: {groupvols[i].vol * g_DuckScale:F2} ({groupvols[i].totalvol * g_DuckScale:F2})";

			debugoverlay?.AddScreenTextOverlay(flXpos, flYpos, duration, r, g, b, a, text);

			// draw volume bar graph

			float vol = (groupvols[i].totalvol * g_DuckScale) / MXR_DEBUG_MAXVOL;

			// draw first 70% green
			float vol1 = 0.0f;
			float vol2 = 0.0f;
			float vol3 = 0.0f;
			int cbars;

			vol1 = Math.Clamp(vol, 0.0f, 0.7f);
			vol2 = Math.Clamp(vol, 0.0f, 0.95f);
			vol3 = vol;

			flXposBar = flXpos + MXR_DEBUG_GREENSTART;

			if (vol1 > 0.0) {
				//flXposBar = flXpos + MXR_DEBUG_GREENSTART;

				rb = 0; gb = 255; bb = 0;       // green bar

				cbars = (int)((float)vol1 * (float)MXR_DEBUG_VOLSCALE);
				cbars = Math.Clamp(cbars, 0, MXR_DEBUG_VOLSCALE * 3 - 1);
				bartext = new string(MXR_DEBUG_CHAR, cbars);

				debugoverlay?.AddScreenTextOverlay(flXposBar, flYpos, duration, rb, gb, bb, ab, bartext);
			}


			// yellow bar
			if (vol2 > MXR_DEBUG_YELLOWLIMIT) {
				rb = 255; gb = 255; bb = 0;

				cbars = (int)((float)vol2 * (float)MXR_DEBUG_VOLSCALE);
				cbars = Math.Clamp(cbars, 0, MXR_DEBUG_VOLSCALE * 3 - 1);
				bartext = new string(MXR_DEBUG_CHAR, cbars);

				debugoverlay?.AddScreenTextOverlay(flXposBar, flYpos, duration, rb, gb, bb, ab, bartext);
			}

			// red bar
			if (vol3 > MXR_DEBUG_REDLIMIT) {
				//flXposBar = flXpos + MXR_DEBUG_REDSTART;
				rb = 255; gb = 0; bb = 0;

				cbars = (int)((float)vol3 * (float)MXR_DEBUG_VOLSCALE);
				cbars = Math.Clamp(cbars, 0, MXR_DEBUG_VOLSCALE * 3 - 1);
				bartext = new string(MXR_DEBUG_CHAR, cbars);

				debugoverlay?.AddScreenTextOverlay(flXposBar, flYpos, duration, rb, gb, bb, ab, bartext);
			}

			flYpos += MXR_DEBUG_INCY;
		}
	}

	public static readonly ConVar snd_disable_mixer_duck = new("snd_disable_mixer_duck", "0");   // if 1, soundmixer ducking is disabled

	// given mix group id, return current duck volume

	static float MXR_GetDuckVolume(int mixgroupid) {

		if (snd_disable_mixer_duck.GetInt() != 0)
			return 1.0f;

		Assert(mixgroupid < g_cgrouprules);

		int grouprulesid = g_mapMixgroupidToGrouprulesid[mixgroupid];

		// if this mixgroup is not ducked, return 1.0

		if (g_grouprules[grouprulesid].is_ducked == 0)
			return 1.0f;

		// return current duck value for this group, scaled by current fade in/out ramp

		return g_grouprules[grouprulesid].duck_ramp_val;

	}

	const float SND_DUCKER_UPDATETIME = 0.1F;       // seconds to wait between ducker updates

	static double g_mxr_ducktime = 0.0;         // time of last update to ducker

	// Get total volume currently playing in all groups,
	// process duck volumes for all groups
	// Call once per frame - updates occur at 10hz

	static void MXR_UpdateAllDuckerVolumes() {
		if (snd_disable_mixer_duck.GetInt() != 0)
			return;

		// check timer since last update, only update at 10hz

		int i;
		double dtime = soundServices.GetHostTime();

		// don't update until timer expires

		if (Math.Abs(dtime - g_mxr_ducktime) < SND_DUCKER_UPDATETIME)
			return;

		g_mxr_ducktime = dtime;

		// clear out all total volume values for groups

		for (i = 0; i < g_cgrouprules; i++)
			g_grouprules[i].total_vol = 0.0f;

		// for every channel in a mix group which can cause ducking:
		// get total volume, store total in grouprule:

		ChannelList list = new();
		int ch_idx;

		Channel pchan;
		bool b_found_ducked_channel = false;

		g_ActiveChannels.GetActiveChannels(list);

		for (i = 0; i < list.Count; i++) {
			ch_idx = list.GetChannelIndex(i);
			pchan = channels[ch_idx];

			if (pchan.LastVol > 0.0) {
				// account for all mix groups this channel belongs to...

				for (int j = 0; j < 8; j++) {
					int imixgroup = pchan.MixGroups[j];

					if (imixgroup < 0)
						continue;

					int grouprulesid = g_mapMixgroupidToGrouprulesid[imixgroup];

					if (g_grouprules[grouprulesid].causes_ducking != 0)
						g_grouprules[grouprulesid].total_vol += pchan.LastVol;

					if (g_grouprules[grouprulesid].is_ducked != 0)
						b_found_ducked_channel = true;
				}
			}
		}

		// if no channels playing which may be ducked, do nothing

		if (!b_found_ducked_channel)
			return;

		// for all groups that can be ducked:
		// see if a higher priority sound group has a volume > threshold,
		// if so, then duck this group by setting duck_target_vol to duck_target_pct.
		// if no sound group is causing ducking in this group, reset duck_target_vol to 1.0

		for (i = 0; i < g_cgrouprules; i++) {
			if (g_grouprules[i].is_ducked != 0) {
				int priority = g_grouprules[i].priority;

				float duck_volume = 1.0f;               // clear to 1.0 if no channel causing ducking

				// make sure we interact appropriately with global voice ducking...
				// if global voice ducking is active, skip sound group ducking and just set duck_volume target to 1.0

				if (g_DuckScale >= 1.0) {
					// check all sound groups for higher priority duck trigger

					for (int j = 0; j < g_cgrouprules; j++) {
						if (g_grouprules[j].priority > priority &&
							g_grouprules[j].causes_ducking != 0 &&
							g_grouprules[j].total_vol > g_grouprules[j].ducker_threshold) {
							// a higher priority group is causing this group to be ducked
							// set duck volume target to the ducked group's duck target percent
							// and break

							duck_volume = g_grouprules[i].duck_target_pct;

							// UNDONE: to prevent edge condition caused by crossing threshold, may need to have secondary
							// UNDONE: timer which allows ducking at 0.2 hz

							break;
						}
					}
				}

				g_grouprules[i].duck_target_vol = duck_volume;
			}
		}

		// update all ducker ramps if current duck value is not target
		// if ramp is greater than duck_volume, approach at 'attack rate'
		// if ramp is less than duck_volume, approach at 'decay rate'

		for (i = 0; i < g_cgrouprules; i++) {
			float target = g_grouprules[i].duck_target_vol;
			float current = g_grouprules[i].duck_ramp_val;

			if (g_grouprules[i].is_ducked != 0 && (current != target)) {

				float ramptime = target < current ? snd_duckerattacktime.GetFloat() : snd_duckerreleasetime.GetFloat();

				// delta is volume change per update (we can do this
				// since we run at an approximate fixed update rate of 10hz)

				float delta = (1.0F - g_grouprules[i].duck_target_pct);

				delta *= (SND_DUCKER_UPDATETIME / ramptime);

				if (current > target)
					delta = -delta;

				// update ramps

				current += delta;

				if (current < target && delta < 0)
					current = target;
				if (current > target && delta > 0)
					current = target;

				g_grouprules[i].duck_ramp_val = current;
			}
		}

	}

	public static readonly ConVar snd_showmixer = new("snd_showmixer", "0");  // set to 1 to show mixer every frame

	// show the current soundmixer output

	static void MXR_DebugShowMixVolumes() {
		if (snd_showmixer.GetInt() == 0)
			return;

		// for the current soundmixer:
		// make a totalvolume bucket for each mixgroup type in the soundmixer.
		// for every active channel, add its spatialized volume to
		// totalvolume bucket for that channel's selected mixgroup

		// display all mixgroup/volume/totalvolume values as horizontal bars

		Span<DebugShowVols> groupvols = new DebugShowVols[CMXRGROUPMAX];

		int i;
		int cgroups = 0;

		if (g_isoundmixer < 0) {
			DevMsg("No sound mixer selected!");
			return;
		}

		ref SoundMixer pmixer = ref g_soundmixers[g_isoundmixer];

		// for every entry in mapMixgroupidToValue which is not -1,
		// set up groupvols

		for (i = 0; i < CMXRGROUPMAX; i++) {
			if (pmixer.mapMixgroupidToValue[i] >= 0) {
				groupvols[cgroups].mixgroupid = i;
				groupvols[cgroups].psz = MXR_GetGroupnameFromId(i).ToString();
				groupvols[cgroups].totalvol = 0.0f;
				groupvols[cgroups].vol = pmixer.mapMixgroupidToValue[i];
				cgroups++;
			}
		}

		// for every active channel, get its volume and
		// the selected mixgroupid, add to groupvols totalvol

		ChannelList list = new();
		int ch_idx;
		Channel pchan;

		g_ActiveChannels.GetActiveChannels(list);

		for (i = 0; i < list.Count; i++) {
			ch_idx = list.GetChannelIndex(i);
			pchan = channels[ch_idx];
			if (pchan.LastVol > 0.0) {
				// find entry in groupvols
				for (int j = 0; j < CMXRGROUPMAX; j++) {
					if (pchan.LastMixGroupId == groupvols[j].mixgroupid) {
						groupvols[j].totalvol += pchan.LastVol;
						break;
					}
				}
			}
		}

		// groupvols is now fully initialized - just display it

		MXR_DebugGraphMixVolumes(groupvols, cgroups);
	}

#if DEBUG

	// set the named mixgroup volume to vol for the current soundmixer
	static void MXR_DebugSetMixGroupVolume(in TokenizedCommand args) {
		if (args.ArgC() != 3) {
			DevMsg("Parameters: mix group name, volume");
			return;
		}

		ReadOnlySpan<char> szgroupname = args[1];
		float vol = strtof(args[2]);

		int imixgroup = MXR_GetMixgroupFromName(szgroupname);

		if (g_isoundmixer < 0)
			return;

		ref SoundMixer pmixer = ref g_soundmixers[g_isoundmixer];

		pmixer.mapMixgroupidToValue[imixgroup] = vol;
	}

#endif //_DEBUG

	// given array of groupids (ie: the sound is in these groups),
	// return a mix volume.

	// return first mixgroup id in the provided array
	// which maps to a non -1 volume value for this
	// sound mixer

	static float MXR_GetVolFromMixGroup(int[] rgmixgroupid, out int plast_mixgroupid) {

		// if no soundmixer currently set, return 1.0 volume

		if (g_isoundmixer < 0) {
			plast_mixgroupid = 0;
			return 1.0f;
		}

		float duckgain = 1.0f;

		if (g_csoundmixers != 0) {
			ref SoundMixer pmixer = ref g_soundmixers[g_isoundmixer];

			// search mixgroupid array, return first match (non -1)

			for (int i = 0; i < 8; i++) {
				int imixgroup = rgmixgroupid[i];

				if (imixgroup < 0)
					continue;

				// save lowest duck gain value for any of the mix groups this sound is in

				float duckgain_new = MXR_GetDuckVolume(imixgroup);

				if (duckgain_new < duckgain)
					duckgain = duckgain_new;

				Assert(imixgroup < CMXRGROUPMAX);

				// return first mixgroup id in the passed in array
				// that maps to a non -1 volume value for this
				// sound mixer

				if (pmixer.mapMixgroupidToValue[imixgroup] >= 0) {
					plast_mixgroupid = imixgroup;

					// get gain due to mixer settings

					float gain = pmixer.mapMixgroupidToValue[imixgroup];

					// modify gain with ducker settings for this group

					return gain * duckgain;
				}
			}
		}

		plast_mixgroupid = 0;
		return duckgain;
	}

	// get id of mixgroup name

	public static int MXR_GetMixgroupFromName(ReadOnlySpan<char> pszgroupname) {
		// scan group rules for mapping from name to id
		pszgroupname = pszgroupname.SliceNullTerminatedString();
		if (pszgroupname.IsEmpty)
			return -1;

		for (int i = 0; i < g_cgrouprules; i++) {
			if (stricmp(g_grouprules[i].szmixgroup, pszgroupname) == 0)
				return g_grouprules[i].mixgroupid;
		}

		return -1;
	}

	// get mixgroup name from id
	public static ReadOnlySpan<char> MXR_GetGroupnameFromId(int mixgroupid) {
		// scan group rules for mapping from name to id
		if (mixgroupid < 0)
			return null;

		for (int i = 0; i < g_cgrouprules; i++) {
			if (g_grouprules[i].mixgroupid == mixgroupid)
				return g_grouprules[i].szmixgroup;
		}

		return null;
	}

	// assign a unique mixgroup id to each unique named mix group
	// within grouprules. Note: all mixgroupids in grouprules must be -1
	// when this routine starts.

	static void MXR_AssignGroupIds() {
		int cmixgroupid = 0;

		for (int i = 0; i < g_cgrouprules; i++) {
			int mixgroupid = MXR_GetMixgroupFromName(g_grouprules[i].szmixgroup);

			if (mixgroupid == -1) {
				// groupname is not yet assigned, provide a unique mixgroupid.

				g_grouprules[i].mixgroupid = cmixgroupid;

				// save reverse mapping, from mixgroupid to the first grouprules entry for this name

				g_mapMixgroupidToGrouprulesid[cmixgroupid] = i;

				cmixgroupid++;
			}
		}
	}

	static string MXR_TruncName(ReadOnlySpan<char> name) {
		name = name.SliceNullTerminatedString();
		return new(name[..Math.Min(CMXRNAMEMAX - 1, name.Length)]);
	}

	static int MXR_AddClassname(ReadOnlySpan<char> pName) {
		string szclassname = MXR_TruncName(pName);
		for (int i = 0; i < g_cgroupclass; i++) {
			if (stricmp(szclassname, g_groupclasslist[i].szclassname) == 0)
				return i;
		}
		if (g_cgroupclass >= CMXRCLASSMAX) {
			Assert(g_cgroupclass < CMXRCLASSMAX);
			return -1;
		}
		g_groupclasslist[g_cgroupclass].szclassname = szclassname;
		g_cgroupclass++;
		return g_cgroupclass - 1;
	}

	// load group rules and sound mixers from file

	static bool MXR_LoadAllSoundMixers() {
		// init soundmixer globals

		g_isoundmixer = -1;
		g_szsoundmixer_cur = "";

		g_csoundmixers = 0;                 // total number of soundmixers found
		g_cgrouprules = 0;                  // total number of group rules found

		Array.Clear(g_soundmixers);
		Array.Clear(g_grouprules);

		// load file

		// build rules

		// build array of sound mixers

		string szFile;
		ReadOnlySpan<char> pstart;
		bool bResult = false;
		string? pbuffer;

		szFile = "scripts/soundmixers.txt";

		pbuffer = null;
		IFileHandle? file = filesystem.Open(szFile, FileOpenOptions.Read, "GAME");
		if (file != null) {
			using (StreamReader reader = new(file.Stream))
				pbuffer = reader.ReadToEnd();
			file.Dispose();
		}
		if (pbuffer == null)
			Error($"MXR_LoadAllSoundMixers: unable to open '{szFile}'\n");

		Span<char> com_token = stackalloc char[1024];

		pstart = pbuffer;

		// first pass: load g_grouprules[]

		// starting at top of file,
		// scan for first '{', skipping all comment lines
		// get strings for: groupname, directory, classname, chan, sndlvl_min, sndlvl_max
		// convert chan to CHAN_ lookup
		// convert sndlvl_min, sndl_max to ints
		// store all in g_grouprules, update g_cgrouprules;
		// get next line
		// when hit '}' we're done with grouprules

		// check for first CHAR_LEFT_PAREN

		while (true) {
			pstart = SndParse.COM_Parse(pstart, com_token);

			if (com_token[0] == '\0')
				break; // eof

			if (com_token[0] != CHAR_LEFT_PAREN)
				continue;

			break;
		}

		while (true) {
			pstart = SndParse.COM_Parse(pstart, com_token);
			if (com_token[0] == CHAR_RIGHT_PAREN)
				break;

			ref GroupRule pgroup = ref g_grouprules[g_cgrouprules];

			// copy mixgroup name, directory, classname
			// if no value specified, set to 0 length string

			pgroup.szmixgroup = "";
			pgroup.szdir = "";

			if (com_token[0] != '\0')
				pgroup.szmixgroup = MXR_TruncName(com_token);

			pstart = SndParse.COM_Parse(pstart, com_token);
			if (com_token[0] != '\0')
				pgroup.szdir = MXR_TruncName(com_token);

			pgroup.classId = -1;
			pstart = SndParse.COM_Parse(pstart, com_token);
			if (com_token[0] != '\0')
				pgroup.classId = MXR_AddClassname(com_token);

			// lookup chan
			pstart = SndParse.COM_Parse(pstart, com_token);
			if (com_token[0] != '\0') {
				if (stricmp(com_token, "CHAN_STATIC") == 0)
					pgroup.chantype = (int)SoundEntityChannel.Static;
				else if (stricmp(com_token, "CHAN_WEAPON") == 0)
					pgroup.chantype = (int)SoundEntityChannel.Weapon;
				else if (stricmp(com_token, "CHAN_VOICE") == 0)
					pgroup.chantype = (int)SoundEntityChannel.Voice;
				else if (stricmp(com_token, "CHAN_VOICE2") == 0)
					pgroup.chantype = (int)SoundEntityChannel.Voice2;
				else if (stricmp(com_token, "CHAN_BODY") == 0)
					pgroup.chantype = (int)SoundEntityChannel.Body;
				else if (stricmp(com_token, "CHAN_ITEM") == 0)
					pgroup.chantype = (int)SoundEntityChannel.Item;
			}
			else
				pgroup.chantype = -1;

			// get sndlvls

			pstart = SndParse.COM_Parse(pstart, com_token);
			if (com_token[0] != '\0')
				pgroup.soundlevel_min = atoi(com_token);
			else
				pgroup.soundlevel_min = -1;

			pstart = SndParse.COM_Parse(pstart, com_token);
			if (com_token[0] != '\0')
				pgroup.soundlevel_max = atoi(com_token);
			else
				pgroup.soundlevel_max = -1;

			// get duck priority, IsDucked, Causes_ducking, duck_target_pct

			pstart = SndParse.COM_Parse(pstart, com_token);
			if (com_token[0] != '\0')
				pgroup.priority = atoi(com_token);
			else
				pgroup.priority = 50;

			pstart = SndParse.COM_Parse(pstart, com_token);
			if (com_token[0] != '\0')
				pgroup.is_ducked = atoi(com_token);
			else
				pgroup.is_ducked = 0;

			pstart = SndParse.COM_Parse(pstart, com_token);
			if (com_token[0] != '\0')
				pgroup.causes_ducking = atoi(com_token);
			else
				pgroup.causes_ducking = 0;

			pstart = SndParse.COM_Parse(pstart, com_token);
			if (com_token[0] != '\0')
				pgroup.duck_target_pct = ((float)(atoi(com_token))) / 100.0f;
			else
				pgroup.duck_target_pct = 0.5f;

			pstart = SndParse.COM_Parse(pstart, com_token);
			if (com_token[0] != '\0')
				pgroup.ducker_threshold = ((float)(atoi(com_token))) / 100.0f;
			else
				pgroup.ducker_threshold = 0.5f;

			pgroup.duck_ramp_val = 1.0f;
			pgroup.duck_target_vol = 1.0f;
			pgroup.total_vol = 0.0f;

			// set mixgroup id to -1
			pgroup.mixgroupid = -1;

			// update rule count

			g_cgrouprules++;

			if (g_cgrouprules >= CMXRGROUPRULESMAX) {
				// UNDONE: error! too many rules
				break;
			}
		}

		// now process all groupids in groups, such that
		// each mixgroup gets a unique id.

		MXR_AssignGroupIds();

		// now load g_soundmixers

		// while not at end of file...
		// scan for "<name>", if found save as new soundmixer name
		// while not '}'
		// scan for "<name>", save as groupname
		// scan for "<num>", save as mix value

		while (true) {
			pstart = SndParse.COM_Parse(pstart, com_token);

			if (com_token[0] == '\0')
				break;  // eof

			// save name in soundmixer

			ref SoundMixer pmixer = ref g_soundmixers[g_csoundmixers];

			pmixer.szsoundmixer = MXR_TruncName(com_token);

			// init all mixer values to -1.

			pmixer.mapMixgroupidToValue = new float[CMXRGROUPMAX];
			for (int j = 0; j < CMXRGROUPMAX; j++)
				pmixer.mapMixgroupidToValue[j] = -1.0f;

			// load all groupnames for this soundmixer

			while (true) {
				pstart = SndParse.COM_Parse(pstart, com_token);

				if (com_token[0] == CHAR_LEFT_PAREN)
					continue;   // skip {

				if (com_token[0] == CHAR_RIGHT_PAREN)
					break;  // finished with this sounmixer

				// lookup mixgroupid for groupname
				int mixgroupid = MXR_GetMixgroupFromName(com_token);
				float value;

				// get mix value
				pstart = SndParse.COM_Parse(pstart, com_token);
				value = strtof(com_token);

				// store value for mixgroupid
				Assert(mixgroupid <= CMXRGROUPMAX);

				if (mixgroupid >= 0)
					pmixer.mapMixgroupidToValue[mixgroupid] = value;
			}

			g_csoundmixers++;
			if (g_csoundmixers >= CMXRSOUNDMIXERSMAX) {
				// UNDONE: error! to many sound mixers
				break;
			}
		}

		bResult = true;

		return bResult;
	}

	static void MXR_ReleaseMemory() {
		// free all resources
	}
}
