global using static Source.AudioSystem.Vox;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Source.AudioSystem;

public readonly struct BytePtr : IEquatable<BytePtr>
{
	public readonly byte[]? Array;
	public readonly int Offset;

	public BytePtr(byte[]? array, int offset = 0) {
		Array = array;
		Offset = offset;
	}

	public static readonly BytePtr Null = default;

	public bool IsNull => Array == null;

	public ref byte this[int index] => ref Array![Offset + index];

	public static BytePtr operator +(BytePtr p, int n) => new(p.Array, p.Offset + n);
	public static BytePtr operator -(BytePtr p, int n) => new(p.Array, p.Offset - n);
	public static int operator -(BytePtr a, BytePtr b) => a.Offset - b.Offset;
	public static BytePtr operator ++(BytePtr p) => new(p.Array, p.Offset + 1);
	public static BytePtr operator --(BytePtr p) => new(p.Array, p.Offset - 1);
	public static bool operator <(BytePtr a, BytePtr b) => a.Offset < b.Offset;
	public static bool operator >(BytePtr a, BytePtr b) => a.Offset > b.Offset;
	public static bool operator <=(BytePtr a, BytePtr b) => a.Offset <= b.Offset;
	public static bool operator >=(BytePtr a, BytePtr b) => a.Offset >= b.Offset;
	public static bool operator ==(BytePtr a, BytePtr b) => a.Array == b.Array && a.Offset == b.Offset;
	public static bool operator !=(BytePtr a, BytePtr b) => !(a == b);

	public bool Equals(BytePtr other) => this == other;
	public override bool Equals(object? obj) => obj is BytePtr other && this == other;
	public override int GetHashCode() => HashCode.Combine(Array, Offset);
}

public struct Sentence_t
{
	public BytePtr pName;
	public float length;
	public bool closecaption;
	public bool isPrecached;
	public string? caption;
}

//===============================================================================
// VOX. Algorithms to load and play spoken text sentences from a file:
//
// In ambient sounds or entity sounds, precache the
// name of the sentence instead of the wave name, ie: !C1A2S4
//
// During sound system init, the 'sentences.txt' is read.
// This file has the format:
//
//		C1A2S4 agrunt/vox/You will be exterminated, surrender NOW.
//      C1A2s5 hgrunt/vox/Radio check, over.
//		...
//
//		There must be at least one space between the sentence name and the sentence.
//		Sentences may be separated by one or more lines
//		There may be tabs or spaces preceding the sentence name
//		The sentence must end in a /n or /r
//		Lines beginning with // are ignored as comments
//
//		Period or comma will insert a pause in the wave unless
//		the period or comma is the last character in the string.
//
//		If first 2 chars of a word are upper case, word volume increased by 25%
//
//		If last char of a word is a number from 0 to 9
//		then word will be pitch-shifted up by 0 to 9, where 0 is a small shift
//		and 9 is a very high pitch shift.
//
// We alloc heap space to contain this data, and track total
// sentences read.  A pointer to each sentence is maintained in g_Sentences.
//
// When sound is played back in S_StartDynamicSound or s_startstaticsound, we detect the !name
// format and lookup the actual sentence in the sentences array
//
// To play, we parse each word in the sentence, chain the words, and play the sentence
// each word's data is loaded directy from disk and freed right after playback.
//===============================================================================
public static class Vox
{
	public const int CVOXWORDMAX = 32;
	public const int CVOXZEROSCANMAX = 255;         // scan up to this many samples for next zero crossing

	// This is the initial capacity for sentences, the array will grow if necessary
	const int MAX_EXPECTED_SENTENCES = 900;

	public static readonly List<Sentence_t> g_Sentences = new(MAX_EXPECTED_SENTENCES);

	// Module Locals
	static readonly BytePtr[] rgpparseword = new BytePtr[CVOXWORDMAX];   // array of pointers to parsed words
	static readonly BytePtr voxperiod = AllocString("_period");               // vocal pause
	static readonly BytePtr voxcomma = AllocString("_comma");             // vocal pause

	const int CVOXMAPNAMESMAX = 24;
	static readonly BytePtr[] g_rgmapnames = new BytePtr[CVOXMAPNAMESMAX];
	static int g_cmapnames = 0;

	static BytePtr AllocString(string s) {
		BytePtr p = new(new byte[s.Length + 1]);
		for (int i = 0; i < s.Length; i++)
			p[i] = (byte)s[i];
		return p;
	}

	static BytePtr AllocBuffer(int size) => new(new byte[size]);

	static void VOX_Reload() {
		VOX_Shutdown();
		VOX_Init();
	}
	[ConCommand("vox_reload", "Reload sentences.txt file", FCvar.Cheat)]
	static void vox_reload() => VOX_Reload();

	static byte[] g_GroupLRU = [];
	static BytePtr g_SentenceFile;

	struct SentenceGroup
	{
		public short count;

		public short lru;
		public string groupname;
		public readonly string GroupName() => groupname;
		public void SetGroupName(string pName) => groupname = pName;
	}

	static readonly List<SentenceGroup> g_SentenceGroups = [];

	class WordBuf
	{
		public string word = "";

		public void Set(BytePtr w) {
			if (w.IsNull) {
				word = "";
				return;
			}
			Set(Str(w));
		}

		public void Set(string? w) {
			if (w == null) {
				word = "";
				return;
			}
			word = w.Length > 255 ? w[..255] : w;
			while (word.Length >= 1 && word[^1] == ' ')
				word = word[..^1];
		}
	}

	class CCPair
	{
		public WordBuf token = new();
		public WordBuf value = new();

		public WordBuf fullpath = new();
	}


	static string Str(BytePtr p) {
		if (p.IsNull)
			return "";
		int len = strlen(p);
		return Encoding.Latin1.GetString(p.Array!, p.Offset, len);
	}

	static int strlen(BytePtr p) {
		int len = 0;
		while (p[len] != 0)
			len++;
		return len;
	}

	static BytePtr strstr(BytePtr haystack, string needle) {
		int n = needle.Length;
		for (BytePtr h = haystack; h[0] != 0; h++) {
			int i = 0;
			while (i < n && h[i] != 0 && h[i] == (byte)needle[i])
				i++;
			if (i == n)
				return h;
		}
		return n == 0 ? haystack : BytePtr.Null;
	}

	static int strnicmp(BytePtr a, string b, int n) {
		for (int i = 0; i < n; i++) {
			int ca = char.ToLowerInvariant((char)a[i]);
			int cb = i < b.Length ? char.ToLowerInvariant(b[i]) : 0;
			if (ca != cb)
				return ca - cb;
			if (ca == 0)
				return 0;
		}
		return 0;
	}

	static int stricmp(BytePtr a, string b) {
		int i = 0;
		while (true) {
			int ca = char.ToLowerInvariant((char)a[i]);
			int cb = i < b.Length ? char.ToLowerInvariant(b[i]) : 0;
			if (ca != cb)
				return ca - cb;
			if (ca == 0)
				return 0;
			i++;
		}
	}

	static void strncpy(BytePtr dest, BytePtr src, int maxlen) {
		int i = 0;
		for (; i < maxlen - 1 && src[i] != 0; i++)
			dest[i] = src[i];
		dest[i] = 0;
	}

	static void strncpy(BytePtr dest, string src, int maxlen) {
		int i = 0;
		for (; i < maxlen - 1 && i < src.Length; i++)
			dest[i] = (byte)src[i];
		dest[i] = 0;
	}

	static bool isdigit(byte c) => c >= '0' && c <= '9';

	// This module depends on these engine calls:
	// DevMsg
	// S_FreeChannel
	// S_LoadSound
	// S_FindName
	// It also depends on vstdlib/RandomInt (all other random calls go through g_pSoundServices)

	public static void VOX_Init() {
		VOX_InitAllEntnames();

		if (!g_SentenceFile.IsNull)
			g_SentenceFile = BytePtr.Null;
		g_GroupLRU = [];
		g_Sentences.Clear();
		g_Sentences.EnsureCapacity(MAX_EXPECTED_SENTENCES);

		VOX_ListClear();

		VOX_ReadSentenceFile("scripts/sentences.txt");
		VOX_LookupMapnames();
	}


	public static void VOX_Shutdown() {
		g_Sentences.Clear();
		VOX_ListClear();
		g_SentenceGroups.Clear();
		g_cmapnames = 0;
	}

	//-----------------------------------------------------------------------------
	// Purpose: This is kind of like strchr(), but we get the actual pointer to the
	//			end of the string when it fails rather than NULL.  This is useful
	//			for parsing buffers containing multiple strings
	// Input  : *string -
	//			scan -
	// Output : char
	//-----------------------------------------------------------------------------
	static BytePtr ScanForwardUntil(BytePtr str, byte scan) {
		while (str[0] != 0) {
			if (str[0] == scan)
				return str;

			str++;
		}
		return str;
	}

	static bool IN_CHARACTERSET(string set, byte c) => c != 0 && set.Contains((char)c);

	// parse a null terminated string of text into component words, with
	// pointers to each word stored in rgpparseword
	// note: this code actually alters the passed in string!

	public static BytePtr[]? VOX_ParseString(BytePtr psz) {
		int i;
		int fdone = 0;
		BytePtr pszscan = psz;
		byte c;
		const string nextWord = " ,.({";
		const string skip = "., ";

		Array.Clear(rgpparseword);

		if (psz.IsNull)
			return null;

		i = 0;
		rgpparseword[i++] = psz;

		while (fdone == 0 && i < CVOXWORDMAX) {
			// scan up to next word
			c = pszscan[0];
			while (c != 0 && !IN_CHARACTERSET(nextWord, c))
				c = (++pszscan)[0];

			// if '(' then scan for matching ')'
			if (c == '(' || c == '{') {
				if (c == '(')
					pszscan = ScanForwardUntil(pszscan, (byte)')');
				else if (c == '{')
					pszscan = ScanForwardUntil(pszscan, (byte)'}');

				c = (++pszscan)[0];
				if (c == 0)
					fdone = 1;
			}

			if (fdone != 0 || c == 0)
				fdone = 1;
			else {
				// if . or , insert pause into rgpparseword,
				// unless this is the last character
				if ((c == '.' || c == ',') && (pszscan + 1)[0] != '\n' && (pszscan + 1)[0] != '\r'
						&& (pszscan + 1)[0] != 0) {
					if (c == '.')
						rgpparseword[i++] = voxperiod;
					else
						rgpparseword[i++] = voxcomma;

					if (i >= CVOXWORDMAX)
						break;
				}

				// null terminate substring
				pszscan[0] = 0;
				pszscan++;

				// skip whitespace
				c = pszscan[0];
				while (c != 0 && IN_CHARACTERSET(skip, c))
					c = (++pszscan)[0];

				if (c == 0)
					fdone = 1;
				else
					rgpparseword[i++] = pszscan;
			}
		}
		return rgpparseword;
	}

	// backwards scan psz for last '/'
	// return substring in szpath null terminated
	// if '/' not found, return 'vox/'

	static BytePtr VOX_GetDirectory(BytePtr szpath, int maxpath, BytePtr psz) {
		byte c;
		int cb = 0;
		BytePtr pszscan = psz + strlen(psz) - 1;

		// scan backwards until first '/' or start of string
		c = pszscan[0];
		while (pszscan > psz && c != '/') {
			c = (--pszscan)[0];
			cb++;
		}

		if (c != '/') {
			// didn't find '/', return default directory
			strncpy(szpath, "vox/", maxpath);
			return psz;
		}

		cb = strlen(psz) - cb;

		cb = Math.Clamp(cb, 0, maxpath - 1);

		// FIXME:  Is this safe?
		psz.Array.AsSpan(psz.Offset, cb).CopyTo(szpath.Array.AsSpan(szpath.Offset, maxpath));
		szpath[cb] = 0;
		return pszscan + 1;
	}

	// get channel volume scale if word

	public static float VOX_GetChanVol(Channel ch) {
		if (ch.Mixer == null)
			return 1.0f;

		return ch.Mixer.GetVolumeScale();
		/*

			if ( scale == 1.0 )
				return;

			ch->rightvol = (int) (ch->rightvol * scale);
			ch->leftvol = (int) (ch->leftvol * scale);

			if ( g_AudioDevice->Should3DMix() )
			{
				ch->rrightvol = (int) (ch->rrightvol * scale);
				ch->rleftvol = (int) (ch->rleftvol * scale);
				ch->centervol = (int) (ch->centervol * scale);
			}
			else
			{
				ch->rrightvol = 0;
				ch->rleftvol = 0;
				ch->centervol = 0;
			}
		*/
	}

	static VoxWord voxwordDefault;

	//===============================================================================
	//  Get any pitch, volume, start, end params into voxword
	//  and null out trailing format characters
	//  Format:
	//		someword(v100 p110 s10 e20)
	//
	//		v is volume, 0% to n%
	//		p is pitch shift up 0% to n%
	//		s is start wave offset %
	//		e is end wave offset %
	//		t is timecompression %
	//
	//	pass fFirst == 1 if this is the first string in sentence
	//  returns 1 if valid string, 0 if parameter block only.
	//
	//  If a ( xxx ) parameter block does not directly follow a word,
	//  then that 'default' parameter block will be used as the default value
	//  for all following words.  Default parameter values are reset
	//  by another 'default' parameter block.  Default parameter values
	//  for a single word are overridden for that word if it has a parameter block.
	//
	//===============================================================================

	public static int VOX_ParseWordParams(BytePtr psz, ref VoxWord pvoxword, bool fFirst) {
		BytePtr pszsave = psz;
		byte c;
		byte ct;
		BytePtr sznum = AllocBuffer(8);
		int i;
		const string commandSet = "vpset)";
		const string delimitSet = "()";

		// init to defaults if this is the first word in string.
		if (fFirst) {
			voxwordDefault.Pitch = -1;
			voxwordDefault.Volume = 100;
			voxwordDefault.Start = 0;
			voxwordDefault.End = 100;
			voxwordDefault.KeepCached = 0;
			voxwordDefault.TimeCompress = 0;
		}

		pvoxword = voxwordDefault;

		// look at next to last char to see if we have a
		// valid format:

		c = (psz + strlen(psz) - 1)[0];

		if (c != ')')
			return 1;       // no formatting, return

		// scan forward to first '('
		c = psz[0];
		while (!IN_CHARACTERSET(delimitSet, c))
			c = (++psz)[0];

		if (c == ')')
			return 0;       // bogus formatting

		// null terminate

		psz[0] = 0;
		ct = (++psz)[0];

		while (true) {
			// scan until we hit a character in the commandSet

			while (ct != 0 && !IN_CHARACTERSET(commandSet, ct))
				ct = (++psz)[0];

			if (ct == ')')
				break;

			sznum.Array.AsSpan(sznum.Offset, 8).Clear();
			i = 0;

			c = (++psz)[0];

			if (!isdigit(c))
				break;

			// read number
			while (isdigit(c) && i < 8 - 1) {
				sznum[i++] = c;
				c = (++psz)[0];
			}

			// get value of number
			i = int.Parse(Str(sznum));

			switch ((char)ct) {
				case 'v': pvoxword.Volume = i; break;
				case 'p': pvoxword.Pitch = i; break;
				case 's': pvoxword.Start = i; break;
				case 'e': pvoxword.End = i; break;
				case 't': pvoxword.TimeCompress = i; break;
			}

			ct = c;
		}

		// if the string has zero length, this was an isolated
		// parameter block.  Set default voxword to these
		// values

		if (pszsave[0] == 0) {
			voxwordDefault = pvoxword;
			return 0;
		}
		else
			return 1;
	}

	const int CVOXSAVEDWORDSIZE = 32;

	// saved entity name/number based on type of entity & id

	const int CVOXGLOBMAX = 4;      // max number of rnd and seqential globals

	struct VoxEntName
	{
		// type is defined by last character of group name.
		// for instance, V_MYNAME_S has type 'S', which is used for soldiers
		// V_MYNUM_M has type 'P' which is used for metrocops

		public int type;

		public int soundsource;             // the enity emitting the sentence
		public BytePtr pszname;                 // a custom name for the entity (this is a word name)
		public BytePtr psznum;                  // a custom number for the entity (this is a word name)
		public VoxGlobalPtrs pszglobal;         // 1 global word, shared by this type of entity, picked randomly, expires after 5min
		public VoxGlobalPtrs pszglobalseq;      // 1 global word, shared by this type of entity, picked in sequence, expires after 5 min
		public bool fdied;                      // true if ent died (don't clear, we need its name)
		public VoxGlobalInts iseq;              // sequence index, for global sequential lookups
		public VoxGlobalFloats timestamp;       // latest update to this ent global timestamp
		public VoxGlobalFloats timestampseq;    // latest update to this ent global sequential timestamp
		public float timedied;                  // timestamp of death

	}

	[InlineArray(CVOXGLOBMAX)]
	struct VoxGlobalPtrs
	{
		BytePtr element;
	}

	[InlineArray(CVOXGLOBMAX)]
	struct VoxGlobalInts
	{
		int element;
	}

	[InlineArray(CVOXGLOBMAX)]
	struct VoxGlobalFloats
	{
		float element;
	}

	const int CENTNAMESMAX = 64;

	static readonly VoxEntName[] g_entnames = new VoxEntName[CENTNAMESMAX];

	static int g_entnamelastsaved = 0;

	// init all

	static void VOX_InitAllEntnames() {
		g_entnamelastsaved = 0;
		Array.Clear(g_entnames);
		Array.Clear(g_rgmapnames);
		g_cmapnames = 0;
	}

	// get new index

	static int VOX_GetNextEntnameIndex() {
		g_entnamelastsaved++;

		if (g_entnamelastsaved >= CENTNAMESMAX)
			g_entnamelastsaved = 0;

		return g_entnamelastsaved;
	}

	// get index of this ent, or get a new index. if fallocnew is true,
	// get a new slot if none found.
	// NOTE: this routine always sets fdied to false - fdied is later
	// set to true by the caller if in IDIED routine. This
	// ensures that if an ent is reused, it won't be marked as fdied.

	static int VOX_LookupEntIndex(int type, int soundsource, bool fallocnew) {
		int i;

		for (i = 0; i < CENTNAMESMAX; i++) {
			if ((g_entnames[i].type == type) && (g_entnames[i].soundsource == soundsource)) {
				g_entnames[i].fdied = false;
				return i;
			}
		}

		if (!fallocnew)
			return -1;

		// new index slot - init

		int inew = VOX_GetNextEntnameIndex();

		g_entnames[inew].type = type;
		g_entnames[inew].soundsource = soundsource;
		g_entnames[inew].timedied = 0;
		g_entnames[inew].fdied = false;
		g_entnames[inew].pszname = BytePtr.Null;
		g_entnames[inew].psznum = BytePtr.Null;

		for (i = 0; i < CVOXGLOBMAX; i++) {
			g_entnames[inew].pszglobal[i] = BytePtr.Null;
			g_entnames[inew].timestamp[i] = 0;
			g_entnames[inew].iseq[i] = 0;
			g_entnames[inew].timestampseq[i] = 0;
			g_entnames[inew].pszglobalseq[i] = BytePtr.Null;
		}

		return inew;
	}

	// lookup random first word from this named group,
	// return static, null terminated string

	static BytePtr VOX_LookupRndVirtual(BytePtr pGroupName) {
		// get group index

		int isentenceg = VOX_GroupIndexFromName(Str(pGroupName));

		if (isentenceg < 0)
			return BytePtr.Null;

		// get pointer to sentence name within group, using lru

		int isentence = VOX_GroupPick(isentenceg, out string szsentencename, 32 - 1);

		if (isentence < 0)
			return BytePtr.Null;

		// get pointer to sentence data

		BytePtr psz = VOX_LookupString(szsentencename[0] == '!' ? szsentencename[1..] : szsentencename, default);

		// strip trailing whitespace

		if (psz.IsNull)
			return BytePtr.Null;

		BytePtr pend = strstr(psz, " ");
		if (!pend.IsNull)
			pend[0] = 0;

		// return pointer to first (and only) word

		return psz;
	}

	// given groupname, get pointer to first word of n'th sentence in group

	static BytePtr VOX_LookupSentenceByIndex(string pGroupname, int ipick, Span<int> pipicknext) {
		// get group index

		int isentenceg = VOX_GroupIndexFromName(pGroupname);

		if (isentenceg < 0)
			return BytePtr.Null;

		// get pointer to sentence name within group, using lru

		int isentence = VOX_GroupPickSequential(isentenceg, out string szsentencename, 32 - 1, ipick, true);

		if (isentence < 0)
			return BytePtr.Null;

		// get pointer to sentence data

		BytePtr psz = VOX_LookupString(szsentencename[0] == '!' ? szsentencename[1..] : szsentencename, default);

		// strip trailing whitespace

		BytePtr pend = strstr(psz, " ");
		if (!pend.IsNull)
			pend[0] = 0;

		if (!pipicknext.IsEmpty)
			pipicknext[0] = isentence;

		// return pointer to first (and only) word
		return psz;
	}

	// lookup first word from this named group, group entry 'ipick',
	// return static, null terminated string

	static BytePtr VOX_LookupNumber(BytePtr pGroupName, int ipick) {
		// construct group name from V_NUMBERS + TYPE

		int glen = strlen(pGroupName);

		// insert type character
		string sznumbers = "V_NUMBERS" + (char)pGroupName[glen - 1];

		return VOX_LookupSentenceByIndex(sznumbers, ipick, default);
	}

	// lookup ent & type, return static, null terminated string
	// if no saved string, create one.
	// UNDONE: init ent/type/string array, wrap when saving

	static BytePtr VOX_LookupMyVirtual(int iname, BytePtr pGroupName, byte chtype, int soundsource) {
		BytePtr psz = BytePtr.Null;

		// get existing ent index, or index to new slot

		int ient = VOX_LookupEntIndex((int)chtype, soundsource, true);

		if (iname == 1) {
			// lookup saved name

			psz = g_entnames[ient].pszname;
		}
		else {
			// lookup saved number

			psz = g_entnames[ient].psznum;
		}

		// if none found for this ent - pick one and save it

		if (psz.IsNull) {
			// get new string
			psz = VOX_LookupRndVirtual(pGroupName);

			// save pointer to new string in g_entnames
			if (iname == 1)
				g_entnames[ient].pszname = psz;
			else
				g_entnames[ient].psznum = psz;
		}

		return psz;
	}

	// get range or heading from ent to player,
	// store range in from 1 to 3 words as ppszNew...ppszNew2
	// store count of words in pcnew
	// if fsimple is true, return numeric sequence based on ten digit max

	static void VOX_LookupRangeHeadingOrGrid(int irhg, BytePtr pGroupName, Channel pChannel, int soundsource, ref BytePtr ppszNew, ref BytePtr ppszNew1, ref BytePtr ppszNew2, ref int pcnew, bool fsimple) {
		Vector3 SL;             // sound -> listener vector
		BytePtr phundreds = BytePtr.Null;
		BytePtr ptens = BytePtr.Null;
		BytePtr pones = BytePtr.Null;
		int cnew = 0;
		float dist;
		int dmeters = 0;
		int hundreds, tens, ones;

		SL = listener_origin - pChannel.Origin;

		if (irhg == 0) {
			// get range
			dist = SL.Length();

			dmeters = (int)(dist * 2.54F / 100.0F); // convert inches to meters

			dmeters = Math.Clamp(dmeters, 0, 900);
		}
		else if (irhg == 1) {
			// get heading
			QAngle source_angles;

			MathLib.VectorAngles(SL, out source_angles);

			dmeters = (int)source_angles[YAW];
		}
		else if (irhg == 2) {
			// get gridx
			dmeters = (int)(((16384 + listener_origin.X) * 2.54F / 100.0) / 10) % 20;
		}
		else if (irhg == 3) {
			// get gridy
			dmeters = (int)(((16384 + listener_origin.Y) * 2.54F / 100.0) / 10) % 20;
		}

		dmeters = Math.Clamp(dmeters, 0, 999);

		// get hundreds, tens, ones

		hundreds = dmeters / 100;
		tens = (dmeters - hundreds * 100) / 10;
		ones = (dmeters - hundreds * 100 - tens * 10);


		if (fsimple) {
			// just return simple ten digit lookups for ones, tens, hundreds

			pones = VOX_LookupNumber(pGroupName, ones);
			cnew++;

			if (tens != 0 || hundreds != 0) {
				ptens = VOX_LookupNumber(pGroupName, tens);
				cnew++;
			}

			if (hundreds != 0) {
				phundreds = VOX_LookupNumber(pGroupName, hundreds);
				cnew++;
			}

			goto LookupNumExit;
		}

		// get pointer to string from groupname and number

		// 100,200,300,400,500,600,700,800,900
		if (hundreds != 0 && tens == 0 && ones == 0) {
			if (hundreds <= 3) {
				phundreds = VOX_LookupNumber(pGroupName, 27 + hundreds);
				cnew++;
			}
			else {
				phundreds = VOX_LookupNumber(pGroupName, hundreds);
				ptens = VOX_LookupNumber(pGroupName, 0);
				pones = VOX_LookupNumber(pGroupName, 0);
				cnew++;
				cnew++;

			}
			goto LookupNumExit;
		}


		if (hundreds != 0) {
			// 101..999
			if (hundreds <= 3 && tens == 0 && ones != 0)
				phundreds = VOX_LookupNumber(pGroupName, 27 + hundreds);
			else
				phundreds = VOX_LookupNumber(pGroupName, hundreds);

			cnew++;

			// 101..109 to 901..909
			if (tens == 0 && ones != 0) {
				pones = VOX_LookupNumber(pGroupName, ones);
				cnew++;
				if (hundreds > 3) {
					ptens = VOX_LookupNumber(pGroupName, 0);
					cnew++;
				}
				goto LookupNumExit;
			}
		}

		// 1..19
		if (tens <= 1 && (tens != 0 || ones != 0)) {
			pones = VOX_LookupNumber(pGroupName, ones + tens * 10);
			cnew++;
			tens = 0;
			goto LookupNumExit;
		}

		// 20..99
		if (tens > 1) {
			if (ones != 0) {
				pones = VOX_LookupNumber(pGroupName, ones);
				cnew++;
			}

			ptens = VOX_LookupNumber(pGroupName, 18 + tens);
			cnew++;
		}


	LookupNumExit:
		// return values

		pcnew = cnew;

		// return
		switch (cnew) {
			default:
				ppszNew = BytePtr.Null;
				return;
			case 1: // 1..19,20,30,40,50,60,70,80,90,100,200,300
				ppszNew = !pones.IsNull ? pones : (!ptens.IsNull ? ptens : (!phundreds.IsNull ? phundreds : BytePtr.Null));
				return;
			case 2:
				if (!ptens.IsNull && !pones.IsNull) {
					ppszNew = ptens;
					ppszNew1 = pones;
				}
				else if (!phundreds.IsNull && !pones.IsNull) {
					ppszNew = phundreds;
					ppszNew1 = pones;
				}
				else if (!phundreds.IsNull && !ptens.IsNull) {
					ppszNew = phundreds;
					ppszNew1 = ptens;
				}
				return;
			case 3:
				ppszNew = phundreds;
				ppszNew1 = ptens;
				ppszNew2 = pones;
				return;
		}
	}

	// find most recent ent of this type marked as dead

	static int VOX_LookupLastDeadIndex(int type) {
		float timemax = -1;
		int ifound = -1;
		int i;

		for (i = 0; i < CENTNAMESMAX; i++) {
			if (g_entnames[i].type == type && g_entnames[i].fdied) {
				if (g_entnames[i].timedied >= timemax) {
					timemax = g_entnames[i].timedied;
					ifound = i;
				}
			}
		}

		return ifound;
	}

	public static readonly ConVar snd_vox_globaltimeout = new("snd_vox_globaltimeout", "300"); // n second timeout to reset global vox words
	public static readonly ConVar snd_vox_seqtimeout = new("snd_vox_seqtimetout", "300");      // n second timeout to reset global sequential vox words
	public static readonly ConVar snd_vox_sectimeout = new("snd_vox_sectimetout", "300");      // n second timeout to reset global sector id
	public static readonly ConVar snd_vox_captiontrace = new("snd_vox_captiontrace", "0", 0, "Shows sentence name for sentences which are set not to show captions.");

	// return index to ent which knows the current sector.
	// if no ent found, alloc a new one and establish shector.
	// sectors expire after approx 5 minutes.

	const int VOXSECTORMAX = 20;

	static float g_vox_lastsectorupdate = 0;
	static int g_vox_isector = -1;

	static BytePtr VOX_LookupSectorVirtual(BytePtr pGroupname) {
		float curtime = (float)soundServices.GetClientTime();

		if (g_vox_isector == -1)
			g_vox_isector = RandomInt(0, VOXSECTORMAX - 1);

		// update sector every 5 min

		if (curtime - g_vox_lastsectorupdate > snd_vox_sectimeout.GetInt()) {
			g_vox_isector++;
			if (g_vox_isector > VOXSECTORMAX)
				g_vox_isector = 1;
			g_vox_lastsectorupdate = curtime;
		}

		return VOX_LookupNumber(pGroupname, g_vox_isector);
	}



	static BytePtr VOX_LookupGlobalVirtual(int type, int soundsource, BytePtr pGroupName, int iglobal) {
		int i;
		float curtime = (float)soundServices.GetClientTime();

		// look for ent of this type with un-expired global

		for (i = 0; i < CENTNAMESMAX; i++) {
			if (g_entnames[i].type == type) {
				if (curtime - g_entnames[i].timestamp[iglobal] <= snd_vox_globaltimeout.GetInt()) {
					// if this ent has an un-expired global, return it, otherwise break

					if (!g_entnames[i].pszglobal[iglobal].IsNull)
						return g_entnames[i].pszglobal[iglobal];
					else
						break;
				}
			}
		}

		// if not found, construct a new global for this ent

		// pick random word from groupname

		BytePtr psz = VOX_LookupRndVirtual(pGroupName);

		// get existing ent index, or index to new slot

		int ient = VOX_LookupEntIndex(type, soundsource, true);

		g_entnames[ient].timestamp[iglobal] = curtime;
		g_entnames[ient].pszglobal[iglobal] = psz;

		return psz;
	}

	// lookup global values in group in sequence - get next value
	// in sequence. sequence counter expires every 2.5 minutes.

	static BytePtr VOX_LookupGlobalSeqVirtual(int type, int soundsource, BytePtr pGroupName, int iglobal) {

		int i;
		int ient;
		float curtime = (float)soundServices.GetClientTime();

		// look for ent of this type with un-expired global

		for (i = 0; i < CENTNAMESMAX; i++) {
			if (g_entnames[i].type == type) {
				if (curtime - g_entnames[i].timestampseq[iglobal] <= (snd_vox_seqtimeout.GetInt() / 2)) {
					// if first ent found has an un-expired global sequence set,
					// get next value in sequence, otherwise break

					ient = i;
					goto Pick_next;
				}
				else {
					// global has expired - reset sequence

					ient = i;
					g_entnames[ient].iseq[iglobal] = 0;
					goto Pick_next;
				}
			}
		}

		// if not found, construct a new sequential global for this ent

		ient = VOX_LookupEntIndex(type, soundsource, true);

	// pick next word from groupname
	Pick_next:
		int ipick = g_entnames[ient].iseq[iglobal];
		int ipicknext = 0;

		BytePtr psz = VOX_LookupSentenceByIndex(Str(pGroupName), ipick, new Span<int>(ref ipicknext));
		g_entnames[ient].iseq[iglobal] = ipicknext;

		// get existing ent index, or index to new slot

		g_entnames[ient].timestampseq[iglobal] = curtime;
		g_entnames[ient].pszglobalseq[iglobal] = psz;

		return psz;
	}

	// insert new words into rgpparseword at 'ireplace' slot

	static void VOX_InsertWords(int ireplace, int cnew, BytePtr pszNew, BytePtr pszNew1, BytePtr pszNew2) {
		if (cnew != 0) {
			// make space in rgpparseword for 'cnew - 1' new words
			int ccopy = cnew - 1; // number of new slots we need
			int j;

			if (ccopy != 0) {
				for (j = CVOXWORDMAX - 1; j > ireplace + ccopy; j--)
					rgpparseword[j] = rgpparseword[j - ccopy];
			}

			// replace rgpparseword entry(s) with the substitued name(s)

			rgpparseword[ireplace] = pszNew;

			if (cnew == 2 || cnew == 3)
				rgpparseword[ireplace + 1] = pszNew1;

			if (cnew == 3)
				rgpparseword[ireplace + 2] = pszNew2;
		}
	}

	// remove 'silent' word from rgpparseword

	static void VOX_DeleteWord(int iword) {
		if (iword < 0 || iword >= CVOXWORDMAX)
			return;

		rgpparseword[iword] = BytePtr.Null;

		// slide all words > iword up into vacated slot

		for (int j = iword; j < CVOXWORDMAX - 1; j++)
			rgpparseword[j] = rgpparseword[j + 1];
	}


	// get global list of map names from sentences.txt
	// map names are stored in order in V_MAPNAMES group

	static void VOX_LookupMapnames() {
		// get group V_MAPNAMES

		int i;
		BytePtr psz;
		int inext = 0;

		for (i = 0; i < CVOXMAPNAMESMAX; i++) {
			// step sequentially through group - return ptr to 1st word in each group (map name)

			psz = VOX_LookupSentenceByIndex("V_MAPNAME", i, new Span<int>(ref inext));

			if (psz.IsNull)
				return;

			g_rgmapnames[i] = psz;
			g_cmapnames++;
		}
	}

	// get index of current map name
	// return 0 as default index if not found

	static int VOX_GetMapNameIndex(ReadOnlySpan<char> pszmapname) {
		for (int i = 0; i < g_cmapnames; i++) {
			if (pszmapname.IndexOf(Str(g_rgmapnames[i])) >= 0)
				return i;
		}
		return 0;
	}

	// look for virtual 'V_' values in rgpparseword.
	// V_MYNAME - replace with saved name value (based on type + entity)
	//			- if no saved name, create one and save
	// V_MYNUM  - replace with saved number value (based on type + entity)
	//			- if no saved num, create on and save
	// V_RNDNUM	- grab a random number string from V_RNDNUM_<type>
	// V_RNDNAME - grab a random name string from V_RNDNAME_<type>

	// replace any 'V_' values with actual string names in rgpparseword

	static bool IsVirtualName(BytePtr pName) {
		return pName[0] == 'V' && pName[1] == '_';
	}

	static void VOX_ReplaceVirtualNames(Channel? pchan) {
		// for each word in the sentence, check for V_, if found
		// replace virtual word with saved word or rnd word

		int i = 0;
		BytePtr pszNew = BytePtr.Null;
		BytePtr pszNew1 = BytePtr.Null;
		BytePtr pszNew2 = BytePtr.Null;
		int iname = -1;
		int cnew = 0;
		bool fbymap;
		BytePtr pszmaptoken;
		int soundsource = pchan != null ? pchan.SoundSource : 0;

		ReadOnlySpan<char> pszmap = soundServices.GetHostMap();

		BytePtr szparseword = AllocBuffer(256);

		// get global list of map names from sentences.txt

		while (!rgpparseword[i].IsNull) {

			if (IsVirtualName(rgpparseword[i])) {
				iname = -1;
				cnew = 0;
				pszNew = BytePtr.Null;
				pszNew1 = BytePtr.Null;
				pszNew2 = BytePtr.Null;

				int slen = strlen(rgpparseword[i]);
				byte chtype = rgpparseword[i][slen - 1];

				// copy word to temp location so we can perform in-place substitutions

				strncpy(szparseword, rgpparseword[i], 256);

				// fbymap is true if lookup is performed via mapname instead of via ordinal

				pszmaptoken = strstr(szparseword, "_MAP__");

				fbymap = pszmaptoken.IsNull ? false : true;

				if (fbymap) {
					int imap = VOX_GetMapNameIndex(pszmap);
					imap = Math.Clamp(imap, 0, 99);

					// replace last 2 characters in _MAP__ substring
					// with imap - this effectively makes all
					// '_map_' lookups relative to the mapname
					if (imap >= 10) {
						pszmaptoken[4] = (byte)((imap / 10) + '0');
						pszmaptoken[5] = (byte)((imap % 10) + '0');
					}
					else {
						pszmaptoken[4] = (byte)'0';
						pszmaptoken[5] = (byte)(imap + '0');
					}
				}

				if (!strstr(szparseword, "V_MYNAME").IsNull)
					iname = 1;
				else if (!strstr(szparseword, "V_MYNUM").IsNull)
					iname = 0;

				if (iname >= 0) {

					// lookup ent & type, return static, null terminated string
					// if no saved string, create one

					pszNew = VOX_LookupMyVirtual(iname, szparseword, chtype, soundsource);
					cnew = 1;
				}
				else {
					if (!strstr(szparseword, "V_RND").IsNull) {
						// lookup random first word from this named group,
						// return static, null terminated string

						pszNew = VOX_LookupRndVirtual(szparseword);
						cnew = 1;
					}
					else if (!strstr(szparseword, "V_DIST").IsNull) {
						// get range from ent to player, return pointers to new words
						VOX_LookupRangeHeadingOrGrid(0, szparseword, pchan!, soundsource, ref pszNew, ref pszNew1, ref pszNew2, ref cnew, true);
					}
					else if (!strstr(szparseword, "V_DIR").IsNull) {
						// get heading from ent to player, return pointers to new words
						VOX_LookupRangeHeadingOrGrid(1, szparseword, pchan!, soundsource, ref pszNew, ref pszNew1, ref pszNew2, ref cnew, false);
					}
					else if (!strstr(szparseword, "V_IDIED").IsNull) {
						// SILENT MARKER - this ent died - mark as dead and timestamp

						int ient = VOX_LookupEntIndex(chtype, soundsource, false);
						if (ient < 0) {
							// if not found, allocate new ent, give him a name & number, mark as dead
							BytePtr szgroup1 = AllocBuffer(32);
							BytePtr szgroup2 = AllocBuffer(32);
							strncpy(szgroup1, "V_MYNAME", 32);
							szgroup1[8] = chtype;
							szgroup1[9] = 0;

							strncpy(szgroup2, "V_MYNUM", 32);
							szgroup2[7] = chtype;
							szgroup2[8] = 0;

							ient = VOX_LookupEntIndex(chtype, soundsource, true);
							g_entnames[ient].pszname = VOX_LookupRndVirtual(szgroup1);
							g_entnames[ient].psznum = VOX_LookupRndVirtual(szgroup2);
						}

						g_entnames[ient].fdied = true;
						g_entnames[ient].timedied = (float)soundServices.GetClientTime();

						// clear this 'silent' word from rgpparseword

						VOX_DeleteWord(i);

					}
					else if (!strstr(szparseword, "V_WHODIED").IsNull) {
						// get last dead unit of this type

						int ient = VOX_LookupLastDeadIndex(chtype);

						// get name and number

						if (ient >= 0) {
							cnew = 1;
							pszNew = g_entnames[ient].pszname;
							pszNew1 = g_entnames[ient].psznum;
							if (!pszNew1.IsNull)
								cnew++;
						}
						else {
							// no dead units, just clear V_WHODIED

							VOX_DeleteWord(i);
						}

					}
					else if (!strstr(szparseword, "V_SECTOR").IsNull) {
						// sectors are fictional - they simply
						// increase sequentially and expire every 5 minutes

						pszNew = VOX_LookupSectorVirtual(szparseword);
						if (!pszNew.IsNull)
							cnew = 1;
					}
					else if (!strstr(szparseword, "V_GRIDX").IsNull) {
						// player x position in 10 meter increments
						VOX_LookupRangeHeadingOrGrid(2, szparseword, pchan!, soundsource, ref pszNew, ref pszNew1, ref pszNew2, ref cnew, true);
					}
					else if (!strstr(szparseword, "V_GRIDY").IsNull) {
						// player y position in 10 meter increments
						VOX_LookupRangeHeadingOrGrid(3, szparseword, pchan!, soundsource, ref pszNew, ref pszNew1, ref pszNew2, ref cnew, true);

					}
					else if (!strstr(szparseword, "V_G0_").IsNull) {
						// 4 rnd globals per type, globals expire after 5 minutes
						// used for target designation, master sector code name etc.

						pszNew = VOX_LookupGlobalVirtual(chtype, soundsource, szparseword, 0);
						if (!pszNew.IsNull)
							cnew = 1;
					}
					else if (!strstr(szparseword, "V_G1_").IsNull) {
						// 4 rnd globals per type, globals expire after 5 minutes
						// used for target designation, master sector code name etc.

						pszNew = VOX_LookupGlobalVirtual(chtype, soundsource, szparseword, 1);
						if (!pszNew.IsNull)
							cnew = 1;
					}
					else if (!strstr(szparseword, "V_G2_").IsNull) {
						// 4 rnd globals per type, globals expire after 5 minutes
						// used for target designation, master sector code name etc.

						pszNew = VOX_LookupGlobalVirtual(chtype, soundsource, szparseword, 2);
						if (!pszNew.IsNull)
							cnew = 1;
					}
					else if (!strstr(szparseword, "V_G3_").IsNull) {
						// 4 rnd globals per type, globals expire after 5 minutes
						// used for target designation, master sector code name etc.

						pszNew = VOX_LookupGlobalVirtual(chtype, soundsource, szparseword, 3);
						if (!pszNew.IsNull)
							cnew = 1;
					}
					else if (!strstr(szparseword, "V_SEQG0_").IsNull) {
						// 4 sequential globals per type, selected sequentially in list
						// used for total target hit count etc.

						pszNew = VOX_LookupGlobalSeqVirtual(chtype, soundsource, szparseword, 0);
						if (!pszNew.IsNull)
							cnew = 1;
					}
					else if (!strstr(szparseword, "V_SEQG1_").IsNull) {
						// 4 sequential globals per type, selected sequentially in list
						// used for total target hit count etc.

						pszNew = VOX_LookupGlobalSeqVirtual(chtype, soundsource, szparseword, 1);
						if (!pszNew.IsNull)
							cnew = 1;
					}
					else if (!strstr(szparseword, "V_SEQG2_").IsNull) {
						// 4 sequential globals per type, selected sequentially in list
						// used for total target hit count etc.

						pszNew = VOX_LookupGlobalSeqVirtual(chtype, soundsource, szparseword, 2);
						if (!pszNew.IsNull)
							cnew = 1;
					}
					else if (!strstr(szparseword, "V_SEQG3_").IsNull) {
						// 4 sequential globals per type, selected sequentially in list
						// used for total target hit count etc.

						pszNew = VOX_LookupGlobalSeqVirtual(chtype, soundsource, szparseword, 3);
						if (!pszNew.IsNull)
							cnew = 1;
					}

				}

				// insert up to 3 new words into rgpparseword at 'i' location

				VOX_InsertWords(i, cnew, pszNew, pszNew1, pszNew2);
			}
			i++;
		}
	}

	public static void VOX_Precache(IEngineSound pSoundSystem, int sentenceIndex, ReadOnlySpan<char> pPathOverride = default) {
		VoxWord[] rgvoxword = new VoxWord[CVOXWORDMAX];
		BytePtr buffer = AllocBuffer(512);
		BytePtr szpath = AllocBuffer(MAX_PATH);
		BytePtr[] pWords = new BytePtr[CVOXWORDMAX];   // array of pointers to parsed words

		Sentence_t sentence = g_Sentences[sentenceIndex];
		if (!IsVirtualName(sentence.pName)) {
			sentence.isPrecached = true;
			g_Sentences[sentenceIndex] = sentence;
		}

		BytePtr psz = sentence.pName + strlen(sentence.pName) + 1;
		// get directory from string, advance psz
		psz = VOX_GetDirectory(szpath, MAX_PATH, psz);
		strncpy(buffer, psz, 512);
		psz = buffer;
		if (!pPathOverride.IsEmpty)
			strncpy(szpath, new string(pPathOverride), MAX_PATH);

		// parse sentence (also inserts null terminators between words)

		VOX_ParseString(psz);
		int i = 0, count = 0;
		// copy the parsed words out of the globals
		for (i = 0; !rgpparseword[i].IsNull; i++) {
			pWords[i] = rgpparseword[i];
			count++;
		}
		int cword = 0;
		for (i = 0; i < count; i++) {
			if (IsVirtualName(pWords[i])) {
				List<WordBuf> list = [];

				VOX_BuildVirtualNameList(pWords[i], list);

				int c = list.Count;
				for (int j = 0; j < c; ++j) {
					string pathbuffer = $"{Str(szpath)}{list[j].word}.wav";
					pSoundSystem.PrecacheSound(pathbuffer, false);
				}
			}
			else {
				// Get any pitch, volume, start, end params into voxword
				if (VOX_ParseWordParams(pWords[i], ref rgvoxword[cword], i == 0) != 0) {
					// this is a valid word (as opposed to a parameter block)
					string pathbuffer = $"{Str(szpath)}{Str(pWords[i])}.wav";
					// find name, if already in cache, mark voxword
					// so we don't discard when word is done playing
					pSoundSystem.PrecacheSound(pathbuffer, false);
					cword++;
				}
			}
		}
	}

	public static void VOX_PrecacheSentenceGroup(IEngineSound pSoundSystem, ReadOnlySpan<char> pGroupName, ReadOnlySpan<char> pPathOverride) {
		string groupName = new(pGroupName);
		int len = groupName.Length;
		for (int i = 0; i < g_Sentences.Count; i++) {
			if (!g_Sentences[i].isPrecached && strnicmp(g_Sentences[i].pName, groupName, len) == 0)
				VOX_Precache(pSoundSystem, i, pPathOverride);
		}
	}


	// link all sounds in sentence, start playing first word.
	// return number of words loaded
	public static void VOX_LoadSound(Channel pchan, ReadOnlySpan<char> pszin) {
		BytePtr buffer = AllocBuffer(512);
		int i, cword;
		BytePtr szpath = AllocBuffer(MAX_PATH);
		VoxWord[] rgvoxword = new VoxWord[CVOXWORDMAX];
		BytePtr psz;
		bool emitcaption = false;
		string? captionSymbol = null;
		float duration = 0.0f;

		if (pszin.IsEmpty)
			return;

		buffer.Array.AsSpan(0, 512).Clear();

		// lookup actual string in g_Sentences,
		// set pointer to string data

		psz = VOX_LookupString(pszin, default, new Span<bool>(ref emitcaption), ref captionSymbol, new Span<float>(ref duration));

		if (psz.IsNull) {
			DevMsg($"VOX_LoadSound: no sentence named {pszin}\n");
			return;
		}

		// get directory from string, advance psz
		psz = VOX_GetDirectory(szpath, MAX_PATH, psz);

		if (strlen(psz) > 512 - 1) {
			DevMsg($"VOX_LoadSound: sentence is too long {Str(psz)}\n");
			return;
		}

		// copy into buffer
		strncpy(buffer, psz, 512);
		psz = buffer;

		// parse sentence (also inserts null terminators between words)

		VOX_ParseString(psz);

		// replace any 'V_' values with actual string names in rgpparseword

		VOX_ReplaceVirtualNames(pchan);

		// for each word in the sentence, construct the filename,
		// lookup the sfx and save each pointer in a temp array

		i = 0;
		cword = 0;

		StringBuilder captionstream = new();

		string groupname = new(pszin);

		int len = groupname.Length;

		while (len > 0 && char.IsDigit(groupname[len - 1])) {
			groupname = groupname[..(len - 1)];
			--len;
		}

		captionstream.Append($"{groupname} ");

		string path = Str(szpath);

		while (!rgpparseword[i].IsNull) {
			// Get any pitch, volume, start, end params into voxword

			if (VOX_ParseWordParams(rgpparseword[i], ref rgvoxword[cword], i == 0) != 0) {
				// this is a valid word (as opposed to a parameter block)
				string pathbuffer = $"{path}{Str(rgpparseword[i])}.wav";

				// find name, if already in cache, mark voxword
				// so we don't discard when word is done playing
				int keepCached = 0;
				rgvoxword[cword].Sfx = S_FindName(pathbuffer, new Span<int>(ref keepCached));
				rgvoxword[cword].KeepCached = keepCached;
				// JAY: HACKHACK: Keep all sentences cached for now
				rgvoxword[cword].KeepCached = 1;

				string captiontoken = $"S({path}{Str(rgpparseword[i])}) ";

				captionstream.Append(captiontoken);

				cword++;
			}
			i++;
		}

		pchan.Mixer = null;

		if (cword != 0) {
			// some 'virtual' sentences can end up with 0 words
			// if no words, then pchan->pMixer is null; chan will be released right away.

			pchan.Mixer = SentenceMixer.CreateSentenceMixer(rgvoxword);
			if (pchan.Mixer == null)
				return;

			pchan.Flags.IsSentence = true;
			pchan.Sfx = rgvoxword[0].Sfx;
			Assert(pchan.Sfx);

			if (soundServices != null) {
				if (emitcaption) {
					if (captionSymbol != null) {
						soundServices.EmitCloseCaption(captionSymbol, duration);

						if (snd_vox_captiontrace.GetBool())
							Msg($"Vox: caption '{captionSymbol}'\n");
					}
					else {
						soundServices.EmitSentenceCloseCaption(captionstream.ToString());

						if (snd_vox_captiontrace.GetBool())
							Msg($"Vox: captionstream '{captionstream}'\n");
					}
				}
				else {
					if (snd_vox_captiontrace.GetBool())
						Msg($"Vox:  No caption for '{pszin}'\n");
				}
			}
		}
	}

	static int CCPairLessFunc(CCPair lhs, CCPair rhs) {
		return string.Compare(lhs.token.word, rhs.token.word, StringComparison.OrdinalIgnoreCase);
	}

	static void VOX_AddNumbers(BytePtr pGroupName, List<WordBuf> list) {
		// construct group name from V_NUMBERS + TYPE
		for (int i = 0; i <= 30; ++i) {
			int glen = strlen(pGroupName);

			// insert type character
			string sznumbers = "V_NUMBERS" + (char)pGroupName[glen - 1];

			WordBuf w = new();
			// w.Set( VOX_LookupString( VOX_LookupSentenceByIndex( sznumbers, i, NULL ), NULL ) );
			w.Set(VOX_LookupSentenceByIndex(sznumbers, i, default));
			list.Add(w);
		}
	}

	static void VOX_AddRndVirtual(BytePtr pGroupName, List<WordBuf> list) {
		// get group index

		int isentenceg = VOX_GroupIndexFromName(Str(pGroupName));

		if (isentenceg < 0)
			return;

		string szgroupname = g_SentenceGroups[isentenceg].GroupName();

		// get pointer to sentence name within group, using lru
		for (int snum = 0; snum < g_SentenceGroups[isentenceg].count; ++snum) {
			string szsentencename = $"{szgroupname}{snum}";
			if (szsentencename.Length > 31)
				szsentencename = szsentencename[..31];

			BytePtr psz = VOX_LookupString(szsentencename[0] == '!' ? szsentencename[1..] : szsentencename, default);

			if (!psz.IsNull) {
				WordBuf w = new();
				w.Set(psz);
				list.Add(w);
			}
		}
	}

	static void VOX_AddMyVirtualWords(int iname, BytePtr pGroupName, byte chtype, List<WordBuf> list) {
		VOX_AddRndVirtual(pGroupName, list);
	}

	static void VOX_BuildVirtualNameList(BytePtr word, List<WordBuf> list) {
		// for each word in the sentence, check for V_, if found
		// replace virtual word with saved word or rnd word

		int iname = -1;
		bool fbymap;
		BytePtr pszmaptoken;


		BytePtr szparseword = AllocBuffer(256);

		int slen = strlen(word);
		byte chtype = word[slen - 1];

		// copy word to temp location so we can perform in-place substitutions

		strncpy(szparseword, word, 256);

		// fbymap is true if lookup is performed via mapname instead of via ordinal

		pszmaptoken = strstr(szparseword, "_MAP__");

		fbymap = pszmaptoken.IsNull ? false : true;

		if (fbymap) {
			for (int imap = 0; imap < g_cmapnames; ++imap) {
				// replace last 2 characters in _MAP__ substring
				// with imap - this effectively makes all
				// '_map_' lookups relative to the mapname
				pszmaptoken[4] = (byte)'0';
				if (imap < 10)
					pszmaptoken[5] = 0;
				else {
					pszmaptoken[4] = (byte)(imap.ToString()[0]);
					pszmaptoken[5] = 0;
				}

				// Recurse...
				VOX_BuildVirtualNameList(szparseword, list);
			}
			return;
		}

		if (!strstr(szparseword, "V_MYNAME").IsNull)
			iname = 1;
		else if (!strstr(szparseword, "V_MYNUM").IsNull)
			iname = 0;

		if (iname >= 0) {

			// lookup ent & type, return static, null terminated string
			// if no saved string, create one

			VOX_AddMyVirtualWords(iname, szparseword, chtype, list);
		}
		else {
			if (!strstr(szparseword, "V_RND").IsNull) {
				// lookup random first word from this named group,
				// return static, null terminated string
				VOX_AddRndVirtual(szparseword, list);
			}
			else if (!strstr(szparseword, "V_DIST").IsNull)
				VOX_AddNumbers(szparseword, list);
			else if (!strstr(szparseword, "V_DIR").IsNull)
				VOX_AddNumbers(szparseword, list);
			else if (!strstr(szparseword, "V_IDIED").IsNull) {
				// SILENT MARKER - this ent died - mark as dead and timestamp

				// if not found, allocate new ent, give him a name & number, mark as dead
				BytePtr szgroup1 = AllocBuffer(32);
				BytePtr szgroup2 = AllocBuffer(32);
				strncpy(szgroup1, "V_MYNAME", 32);
				szgroup1[8] = chtype;
				szgroup1[9] = 0;

				strncpy(szgroup2, "V_MYNUM", 32);
				szgroup2[7] = chtype;
				szgroup2[8] = 0;

				VOX_BuildVirtualNameList(szgroup1, list);
				VOX_BuildVirtualNameList(szgroup2, list);
				return;

			}
			else if (!strstr(szparseword, "V_WHODIED").IsNull) {
				// get last dead unit of this type
				/*

				int ient = VOX_LookupLastDeadIndex( chtype );

				// get name and number

				if (ient >= 0)
				{
					cnew = 1;
					pszNew = g_entnames[ient].pszname;
					pszNew1 = g_entnames[ient].psznum;
					if (pszNew1)
						cnew++;
				}
				else
				{
					// no dead units, just clear V_WHODIED

					VOX_DeleteWord(i);
				}
				*/

			}
			else if (!strstr(szparseword, "V_SECTOR").IsNull)
				VOX_AddNumbers(szparseword, list);
			else if (!strstr(szparseword, "V_GRIDX").IsNull)
				VOX_AddNumbers(szparseword, list);
			else if (!strstr(szparseword, "V_GRIDY").IsNull)
				VOX_AddNumbers(szparseword, list);
			else if (!strstr(szparseword, "V_G0_").IsNull)
				VOX_AddRndVirtual(szparseword, list);
			else if (!strstr(szparseword, "V_G1_").IsNull)
				VOX_AddRndVirtual(szparseword, list);
			else if (!strstr(szparseword, "V_G2_").IsNull)
				VOX_AddRndVirtual(szparseword, list);
			else if (!strstr(szparseword, "V_G3_").IsNull)
				VOX_AddRndVirtual(szparseword, list);
			else if (!strstr(szparseword, "V_SEQG0_").IsNull)
				VOX_AddRndVirtual(szparseword, list);
			else if (!strstr(szparseword, "V_SEQG1_").IsNull)
				VOX_AddRndVirtual(szparseword, list);
			else if (!strstr(szparseword, "V_SEQG2_").IsNull)
				VOX_AddRndVirtual(szparseword, list);
			else if (!strstr(szparseword, "V_SEQG3_").IsNull)
				VOX_AddRndVirtual(szparseword, list);

		}

		if (strnicmp(szparseword, "V_", 2) != 0) {
			WordBuf w = new();
			w.Set(szparseword);
			list.Add(w);
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: For generating reslists, adds the wavefile to the dictionary
	// Input  : *fn -
	//-----------------------------------------------------------------------------
	static void VOX_Touch(string fn, SortedSet<string> list) {
		list.Add(fn);
	}

	//-----------------------------------------------------------------------------
	// Purpose: Iterates the touch list and touches all referenced .wav files.
	// Input  : int -
	//			list -
	//-----------------------------------------------------------------------------
	static void VOX_TouchSounds(SortedSet<string> list, List<CCPair> ccpairs, bool spewsentences) {
		foreach (string fn in list) {
			// Msg( "touch %s\n", fn );
			string expanded = $"sound/{fn}";

			IFileHandle? fh = filesystem.Open(expanded, FileOpenOptions.Read | FileOpenOptions.Binary);
			fh?.Dispose();
		}

		if (spewsentences) {
			foreach (CCPair pair in ccpairs)
				Msg($"\"{pair.token.word}\"\t\"{pair.value.word}\"\n");

			IFileHandle? fh = filesystem.Open("sentences.m3u", FileOpenOptions.Write, "GAME");
			if (fh != null) {
				using (StreamWriter writer = new(fh.Stream)) {
					foreach (CCPair pair in ccpairs)
						writer.Write($"{pair.fullpath.word}\n");
				}

				fh.Dispose();
			}
		}
	}

	static void CCPairsInsert(List<CCPair> ccpairs, CCPair pair) {
		int index = ccpairs.BinarySearch(pair, Comparer<CCPair>.Create(CCPairLessFunc));
		if (index < 0)
			ccpairs.Insert(~index, pair);
	}

	// link all sounds in sentence, start playing first word.
	// return number of words loaded
	static void VOX_TouchSound(ReadOnlySpan<char> pszin, SortedSet<string> filelist, List<CCPair> ccpairs, bool spewsentences) {
		BytePtr buffer = AllocBuffer(512);
		int i, cword;
		BytePtr szpath = AllocBuffer(MAX_PATH);
		VoxWord[] rgvoxword = new VoxWord[CVOXWORDMAX];
		BytePtr psz;

		if (pszin.IsEmpty)
			return;

		buffer.Array.AsSpan(0, 512).Clear();

		// lookup actual string in g_Sentences,
		// set pointer to string data

		psz = VOX_LookupString(pszin, default);

		if (psz.IsNull) {
			DevMsg($"VOX_TouchSound: no sentence named {pszin}\n");
			return;
		}

		// get directory from string, advance psz
		psz = VOX_GetDirectory(szpath, MAX_PATH, psz);

		if (strlen(psz) > 512 - 1) {
			DevMsg($"VOX_TouchSound: sentence is too long {Str(psz)}\n");
			return;
		}

		// copy into buffer
		strncpy(buffer, psz, 512);
		psz = buffer;

		// parse sentence (also inserts null terminators between words)

		VOX_ParseString(psz);

		// for each word in the sentence, construct the filename,
		// lookup the sfx and save each pointer in a temp array

		i = 0;
		cword = 0;

		List<WordBuf> rep = [];
		string path = Str(szpath);

		while (!rgpparseword[i].IsNull) {
			// Get any pitch, volume, start, end params into voxword

			if (VOX_ParseWordParams(rgpparseword[i], ref rgvoxword[cword], i == 0) != 0) {
				// Iterate all virtuals here...
				if (strnicmp(rgpparseword[i], "V_", 2) == 0) {
					List<WordBuf> list = [];

					VOX_BuildVirtualNameList(rgpparseword[i], list);

					int c = list.Count;
					for (int j = 0; j < c; ++j) {
						string name = list[j].word;

						if (name.StartsWith("V_", StringComparison.OrdinalIgnoreCase))
							Warning($"VOX_TouchSound didn't resolve virtual token {name}!\n");

						string pathbuffer = $"{path}{name}.wav";
						VOX_Touch(pathbuffer, filelist);

						WordBuf w = new();
						if (j == 0) {
							w.Set(name);
							rep.Add(w);
						}
						CCPair pair = new();
						pair.token.word = $"S({path}{name})";
						pair.value.Set(name);

						pathbuffer = $"{soundServices.GetGameDir()}/sound/{path}{name}.wav".Replace('/', '\\');
						pair.fullpath.Set(pathbuffer);

						CCPairsInsert(ccpairs, pair);
					}
				}
				else {
					// this is a valid word (as opposed to a parameter block)
					string pathbuffer = $"{path}{Str(rgpparseword[i])}.wav";
					VOX_Touch(pathbuffer, filelist);

					WordBuf w = new();
					w.Set(rgpparseword[i]);
					rep.Add(w);

					CCPair pair = new();
					pair.token.word = $"S({path}{Str(rgpparseword[i])})";
					pair.value.Set(rgpparseword[i]);

					pathbuffer = $"{soundServices.GetGameDir()}/sound/{path}{Str(rgpparseword[i])}.wav".Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
					pair.fullpath.Set(pathbuffer);

					CCPairsInsert(ccpairs, pair);
				}
			}
			i++;
		}

		if (spewsentences) {
			StringBuilder outbuf = new();
			// Build representative text
			for (i = 0; i < rep.Count; ++i) {
				/*
				if ( !Q_stricmp( rep[ i ].word, "_comma" ) )
				{
					if ( i != 0 && Q_strlen( outbuf ) >= 1 )
					{
						outbuf[ Q_strlen( outbuf ) - 1 ] =0;
					}

					// Don't end sentence with comma..
					if ( i != rep.Count() - 1 )
					{
						Q_strncat( outbuf, ", ", sizeof( outbuf ), COPY_ALL_CHARACTERS );
					}
					continue;
				}
				*/

				outbuf.Append(rep[i].word);
				if (i != rep.Count - 1)
					outbuf.Append(' ');
			}

			Msg($"     {outbuf}\n");
		}
	}


	//-----------------------------------------------------------------------------
	// Purpose: Take a NULL terminated sentence, and parse any commands contained in
	//			{}.  The string is rewritten in place with those commands removed.
	//
	// Input  : *pSentenceData - sentence data to be modified in place
	//			sentenceIndex - global sentence table index for any data that is
	//							parsed out
	//-----------------------------------------------------------------------------
	static void VOX_ParseLineCommands(BytePtr pSentenceData, int sentenceIndex) {
		BytePtr tempBuffer = AllocBuffer(512);
		BytePtr pNext, pStart;
		int length, tempBufferPos = 0;
		Span<char> com_token = stackalloc char[1024];

		if (pSentenceData.IsNull)
			return;

		pStart = pSentenceData;

		while (pSentenceData[0] != 0) {
			pNext = ScanForwardUntil(pSentenceData, (byte)'{');

			// Find length of "good" portion of the string (not a {} command)
			length = pNext - pSentenceData;
			if (tempBufferPos + length > 512) {
				DevMsg("Error! sentence too long!\n");
				return;
			}

			// Copy good string to temp buffer
			pSentenceData.Array.AsSpan(pSentenceData.Offset, length).CopyTo(tempBuffer.Array.AsSpan(tempBuffer.Offset + tempBufferPos, 512 - tempBufferPos));

			// Move the copy position
			tempBufferPos += length;

			pSentenceData = pNext;

			// Skip ahead of the opening brace
			if (pSentenceData[0] != 0)
				pSentenceData++;

			while (true) {
				// Skip whitespace
				while (pSentenceData[0] != 0 && pSentenceData[0] <= 32)
					pSentenceData++;

				// Simple comparison of string commands:
				switch (char.ToLowerInvariant((char)pSentenceData[0])) {
					case 'l':
						// All commands starting with the letter 'l' here
						if (strnicmp(pSentenceData, "len", 3) == 0) {
							Sentence_t sentence = g_Sentences[sentenceIndex];
							sentence.length = SndDsp.strtof(Str(pSentenceData + 3));
							g_Sentences[sentenceIndex] = sentence;

							// "len " len + space
							pSentenceData += 4;

							// Skip until next } or whitespace character
							while (pSentenceData[0] != 0 && (pSentenceData[0] != '}' && !(pSentenceData[0] <= 32)))
								pSentenceData++;
						}
						break;
					case 'c':
						// This sentence should emit a close caption
						if (strnicmp(pSentenceData, "closecaption", 12) == 0) {
							Sentence_t sentence = g_Sentences[sentenceIndex];
							sentence.closecaption = true;

							pSentenceData += 12;

							string remaining = Str(pSentenceData);
							ReadOnlySpan<char> rest = SndParse.COM_Parse(remaining, com_token);
							pSentenceData += remaining.Length - rest.Length;

							// Skip until next } or whitespace character
							while (pSentenceData[0] != 0 && (pSentenceData[0] != '}' && !(pSentenceData[0] <= 32)))
								pSentenceData++;

							if (com_token[0] != '\0')
								sentence.caption = new string(com_token.SliceNullTerminatedString());
							else
								sentence.caption = null;
							g_Sentences[sentenceIndex] = sentence;
						}
						break;
					case '\0':
					default: {
							// Skip until next } or whitespace character
							while (pSentenceData[0] != 0 && (pSentenceData[0] != '}' && !(pSentenceData[0] <= 32)))
								pSentenceData++;
						}
						break;
				}

				// Done?
				if (pSentenceData[0] == 0 || pSentenceData[0] == '}')
					break;
			}

			// pSentenceData = ScanForwardUntil( pSentenceData, '}' );

			// Skip the closing brace
			if (pSentenceData[0] != 0)
				pSentenceData++;

			// Skip trailing whitespace
			while (pSentenceData[0] != 0 && pSentenceData[0] <= 32)
				pSentenceData++;
		}

		if (tempBufferPos < 512) {
			// terminate cleaned up copy
			tempBuffer[tempBufferPos] = 0;

			// Copy it over the original data
			tempBuffer.Array.AsSpan(tempBuffer.Offset, tempBufferPos + 1).CopyTo(pStart.Array.AsSpan(pStart.Offset, tempBufferPos + 1));
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Add a new group or increment count of the existing one
	// Input  : *pSentenceName - text of the sentence name
	//-----------------------------------------------------------------------------
	static int VOX_GroupAdd(BytePtr pSentenceName) {
		int len = strlen(pSentenceName) - 1;

		// group members end in a number
		if (len <= 0 || !isdigit(pSentenceName[len]))
			return -1;

		// truncate away the index
		while (len > 0 && isdigit(pSentenceName[len]))
			len--;

		// make a copy of the actual group name
		string groupName = Str(pSentenceName)[..(len + 1)];

		// check for it in the list
		int i;

		int groupCount = g_SentenceGroups.Count;
		for (i = 0; i < groupCount; i++) {
			int groupIndex = (i + groupCount - 1) % groupCount;

			// Start at the last group a loop around
			SentenceGroup pGroup = g_SentenceGroups[groupIndex];
			if (string.Equals(groupName, pGroup.GroupName(), StringComparison.OrdinalIgnoreCase)) {
				// Matches previous group, bump count
				pGroup.count++;
				g_SentenceGroups[groupIndex] = pGroup;
				return i;
			}
		}

		// new group
		SentenceGroup group = new();
		group.SetGroupName(groupName);
		group.count = 1;
		g_SentenceGroups.Add(group);
		return g_SentenceGroups.Count - 1;
	}


	static void VOX_LRUInit(ref SentenceGroup pGroup) {
		int i, n1, n2, temp;

		if (pGroup.count != 0) {
			Span<byte> pLRU = g_GroupLRU.AsSpan(pGroup.lru);
			for (i = 0; i < pGroup.count; i++)
				pLRU[i] = (byte)i;

			// randomize array by swapping random elements
			for (i = 0; i < (pGroup.count * 4); i++) {
				// FIXME: This should probably call through g_pSoundServices
				// or some other such call?
				n1 = RandomInt(0, pGroup.count - 1);
				n2 = RandomInt(0, pGroup.count - 1);
				temp = pLRU[n1];
				pLRU[n1] = pLRU[n2];
				pLRU[n2] = (byte)temp;
			}
		}
	}


	//-----------------------------------------------------------------------------
	// Purpose: Init the LRU for each sentence group
	//-----------------------------------------------------------------------------
	static void VOX_GroupInitAllLRUs() {
		int i;

		int totalCount = 0;
		for (i = 0; i < g_SentenceGroups.Count; i++) {
			SentenceGroup group = g_SentenceGroups[i];
			group.lru = (short)totalCount;
			g_SentenceGroups[i] = group;
			totalCount += group.count;
		}
		g_GroupLRU = new byte[totalCount];
		for (i = 0; i < g_SentenceGroups.Count; i++) {
			SentenceGroup group = g_SentenceGroups[i];
			VOX_LRUInit(ref group);
			g_SentenceGroups[i] = group;
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Only during reslist generation
	//-----------------------------------------------------------------------------
	static void VOX_AddSentenceWavesToResList() {
		if (CommandLine.FindParm("-makereslists") == 0 &&
			 CommandLine.FindParm("-spewsentences") == 0) {
			return;
		}

		bool spewsentences = CommandLine.FindParm("-spewsentences") != 0 ? true : false;

		SortedSet<string> list = new(StringComparer.OrdinalIgnoreCase);
		List<CCPair> ccpairs = [];

		int i;
		int sentencecount = g_Sentences.Count;

		for (i = 0; i < sentencecount; i++) {
			// Walk through all nonvirtual sentences and touch the referenced sounds...
			Sentence_t pSentence = g_Sentences[i];

			if (strnicmp(pSentence.pName, "V_", 2) == 0)
				continue;

			if (spewsentences) {
				BytePtr psz = VOX_LookupString(Str(pSentence.pName), default);
				if (!psz.IsNull)
					Msg($"{Str(pSentence.pName)} : {Str(psz)}\n");
			}

			VOX_TouchSound(Str(pSentence.pName), list, ccpairs, spewsentences);

		}

		VOX_TouchSounds(list, ccpairs, spewsentences);

		list.Clear();
	}


	//-----------------------------------------------------------------------------
	// Purpose: Given a group name, return that group's index
	// Input  : *pGroupName - name of the group
	// Output : int - index in group table, returns -1 if no matching group is found
	//-----------------------------------------------------------------------------
	public static int VOX_GroupIndexFromName(ReadOnlySpan<char> pGroupName) {
		int i;

		pGroupName = pGroupName.SliceNullTerminatedString();
		if (!pGroupName.IsEmpty) {
			// search rgsentenceg for match on szgroupname
			for (i = 0; i < g_SentenceGroups.Count; i++) {
				if (pGroupName.Equals(g_SentenceGroups[i].GroupName(), StringComparison.OrdinalIgnoreCase))
					return i;
			}
		}

		return -1;
	}


	//-----------------------------------------------------------------------------
	// Purpose: return the group's name
	// Input  : groupIndex - index of the group
	// Output : const char * - name pointer
	//-----------------------------------------------------------------------------
	public static ReadOnlySpan<char> VOX_GroupNameFromIndex(int groupIndex) {
		if (groupIndex >= 0 && groupIndex < g_SentenceGroups.Count)
			return g_SentenceGroups[groupIndex].GroupName();

		return null;
	}

	// ignore lru. pick next sentence from sentence group. Go in order until we hit the last sentence,
	// then repeat list if freset is true.  If freset is false, then repeat last sentence.
	// ipick is passed in as the requested sentence ordinal.
	// ipick 'next' is returned.
	// return of -1 indicates an error.

	public static int VOX_GroupPickSequential(int isentenceg, out string szfound, int szfoundLen, int ipick, bool freset) {
		string szgroupname;
		byte count;

		szfound = "";

		if (isentenceg < 0 || isentenceg > g_SentenceGroups.Count)
			return -1;

		szgroupname = g_SentenceGroups[isentenceg].GroupName();
		count = (byte)g_SentenceGroups[isentenceg].count;

		if (count == 0)
			return -1;

		if (ipick >= count)
			ipick = count - 1;

		szfound = $"!{szgroupname}{ipick}";
		if (szfound.Length > szfoundLen - 1)
			szfound = szfound[..(szfoundLen - 1)];

		if (ipick >= count) {
			if (freset)
				// reset at end of list
				return 0;
			else
				return count;
		}

		return ipick + 1;
	}



	// pick a random sentence from rootname0 to rootnameX.
	// picks from the rgsentenceg[isentenceg] least
	// recently used, modifies lru array. returns the sentencename.
	// note, lru must be seeded with 0-n randomized sentence numbers, with the
	// rest of the lru filled with -1. The first integer in the lru is
	// actually the size of the list.  Returns ipick, the ordinal
	// of the picked sentence within the group.

	public static int VOX_GroupPick(int isentenceg, out string szfound, int strLen) {
		string szgroupname;
		byte i;
		byte count;
		byte ipick = 0;
		bool ffound = false;

		szfound = "";

		if (isentenceg < 0 || isentenceg > g_SentenceGroups.Count)
			return -1;

		szgroupname = g_SentenceGroups[isentenceg].GroupName();
		count = (byte)g_SentenceGroups[isentenceg].count;
		int plru = g_SentenceGroups[isentenceg].lru;

		while (!ffound) {
			for (i = 0; i < count; i++)
				if (g_GroupLRU[plru + i] != 0xFF) {
					ipick = g_GroupLRU[plru + i];
					g_GroupLRU[plru + i] = 0xFF;
					ffound = true;
					break;
				}

			if (!ffound) {
				SentenceGroup group = g_SentenceGroups[isentenceg];
				VOX_LRUInit(ref group);
				g_SentenceGroups[isentenceg] = group;
			}
			else {
				szfound = $"!{szgroupname}{ipick}";
				if (szfound.Length > strLen - 1)
					szfound = szfound[..(strLen - 1)];
				return ipick;
			}
		}
		return -1;
	}


	static readonly List<string> g_pSentenceFileList = [];

	//-----------------------------------------------------------------------------
	// Purpose: clear / reinitialize the vox list
	//-----------------------------------------------------------------------------
	static void VOX_ListClear() {
		g_pSentenceFileList.Clear();
	}

	//-----------------------------------------------------------------------------
	// Purpose: Check to see if this file is in the list
	// Input  : *psentenceFileName -
	// Output : int, true if the file is in the list, false if not
	//-----------------------------------------------------------------------------
	static bool VOX_ListFileIsLoaded(string psentenceFileName) {
		foreach (string pList in g_pSentenceFileList) {
			if (psentenceFileName == pList)
				return true;
		}

		return false;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Add this file name to the sentence list
	// Input  : *psentenceFileName -
	//-----------------------------------------------------------------------------
	static void VOX_ListMarkFileLoaded(string psentenceFileName) {
		g_pSentenceFileList.Insert(0, psentenceFileName);
	}

	// This creates a compact copy of the sentence file in memory with only the necessary data
	static void VOX_CompactSentenceFile() {
		int totalMem = 0;
		int i;
		for (i = 0; i < g_Sentences.Count; i++) {
			int len = strlen(g_Sentences[i].pName) + 1;
			BytePtr pData = g_Sentences[i].pName + len;
			int dataLen = strlen(pData) + 1;
			totalMem += len + dataLen;
		}
		BytePtr newFile = AllocBuffer(Math.Max(totalMem, 1));
		totalMem = 0;
		for (i = 0; i < g_Sentences.Count; i++) {
			Sentence_t sentence = g_Sentences[i];
			int len = strlen(sentence.pName) + 1;
			BytePtr pData = sentence.pName + len;
			int dataLen = strlen(pData) + 1;
			BytePtr pDest = newFile + totalMem;
			sentence.pName.Array.AsSpan(sentence.pName.Offset, len + dataLen).CopyTo(pDest.Array.AsSpan(pDest.Offset, len + dataLen));
			sentence.pName = pDest;
			g_Sentences[i] = sentence;
			totalMem += len + dataLen;
		}
		g_SentenceFile = newFile;
	}

	// Load sentence file into memory, insert null terminators to
	// delimit sentence name/sentence pairs.  Keep pointer to each
	// sentence name so we can search later.

	public static void VOX_ReadSentenceFile(string psentenceFileName) {
		BytePtr pch;
		BytePtr pFileData;
		int fileSize;
		byte c;
		BytePtr pchlast, pSentenceData;
		const string whitespace = "\n\r\t ";

		// Have we already loaded this file?
		if (VOX_ListFileIsLoaded(psentenceFileName)) {
			// must touch any sentence wavs again to ensure the map's init path gets the results
			if (soundServices.IsReslistLoggingToMap())
				VOX_AddSentenceWavesToResList();
			return;
		}

		// load file

		IFileHandle? file;
		file = filesystem.Open(psentenceFileName, FileOpenOptions.Read | FileOpenOptions.Binary);
		if (file == null) {
			DevMsg($"Couldn't load {psentenceFileName}\n");
			return;
		}

		fileSize = (int)file.Stream.Length;
		if (fileSize <= 0) {
			DevMsg($"VOX_ReadSentenceFile: {psentenceFileName} has invalid size {fileSize}\n");
			file.Dispose();
			return;
		}

		pFileData = AllocBuffer(fileSize + 1);

		// Read the data and close the file
		file.Stream.ReadExactly(pFileData.Array.AsSpan(0, fileSize));
		file.Dispose();

		// Make sure we end with a null terminator
		pFileData[fileSize] = 0;

		pch = pFileData;
		pchlast = pch + fileSize;
		BytePtr pName = BytePtr.Null;
		while (pch < pchlast) {
			// Only process this pass on sentences
			pSentenceData = BytePtr.Null;

			// skip newline, cr, tab, space

			c = pch[0];
			while (pch < pchlast && IN_CHARACTERSET(whitespace, c))
				c = (++pch)[0];

			// YWB:  Fix possible crashes reading past end of file if the last line has only whitespace on it...
			if (pch[0] == 0)
				break;

			// skip entire line if first char is /
			if (pch[0] != '/') {
				Sentence_t pSentence = new();
				pName = pch;
				pSentence.pName = pch;
				pSentence.length = 0;
				pSentence.closecaption = false;
				pSentence.isPrecached = false;
				pSentence.caption = null;
				g_Sentences.Add(pSentence);

				// scan forward to first space, insert null terminator
				// after sentence name

				c = pch[0];
				while (pch < pchlast && c != ' ')
					c = (++pch)[0];

				if (pch < pchlast) {
					pch[0] = 0;
					pch++;
				}

				// A sentence may have some line commands, make an extra pass
				pSentenceData = pch;
			}
			// scan forward to end of sentence or eof
			while (pch < pchlast && pch[0] != '\n' && pch[0] != '\r')
				pch++;

			// insert null terminator
			if (pch < pchlast) {
				pch[0] = 0;
				pch++;
			}

			// If we have some sentence data, parse out any line commands
			if (!pSentenceData.IsNull && pSentenceData < pchlast) {
				// Add a new group or increment count of the existing one
				VOX_GroupAdd(pName);
				int index = g_Sentences.Count - 1;
				// The current sentence has an index of count-1
				VOX_ParseLineCommands(pSentenceData, index);

			}
		}
		// now compact the file data in memory
		VOX_CompactSentenceFile();

		VOX_GroupInitAllLRUs();

		// This only does stuff during reslist generation...
		VOX_AddSentenceWavesToResList();

		VOX_ListMarkFileLoaded(psentenceFileName);
	}


	//-----------------------------------------------------------------------------
	// Purpose: Get the current number of sentences in the database
	// Output : int
	//-----------------------------------------------------------------------------
	public static int VOX_SentenceCount() {
		return g_Sentences.Count;
	}


	public static float VOX_SentenceLength(int sentence_num) {
		if (sentence_num < 0 || sentence_num > g_Sentences.Count - 1)
			return 0.0f;

		return g_Sentences[sentence_num].length;
	}

	// scan g_Sentences, looking for pszin sentence name
	// return pointer to sentence data if found, null if not
	// CONSIDER: if we have a large number of sentences, should
	// CONSIDER: sort strings in g_Sentences and do binary search.
	public static BytePtr VOX_LookupString(ReadOnlySpan<char> pSentenceName, Span<int> psentencenum) {
		string? caption = null;
		return VOX_LookupString(pSentenceName, psentencenum, default, ref caption, default);
	}

	public static BytePtr VOX_LookupString(ReadOnlySpan<char> pSentenceName, Span<int> psentencenum, Span<bool> pbEmitCaption, ref string? pCaptionSymbol, Span<float> pflDuration) {
		if (!pbEmitCaption.IsEmpty)
			pbEmitCaption[0] = false;

		pCaptionSymbol = null;

		if (!pflDuration.IsEmpty)
			pflDuration[0] = 0.0f;

		string sentenceName = new(pSentenceName.SliceNullTerminatedString());

		int i;
		int c = g_Sentences.Count;
		for (i = 0; i < c; i++) {
			BytePtr name = g_Sentences[i].pName;

			if (stricmp(name, sentenceName) == 0) {
				if (!psentencenum.IsEmpty)
					psentencenum[0] = i;

				if (!pbEmitCaption.IsEmpty)
					pbEmitCaption[0] = g_Sentences[i].closecaption;

				pCaptionSymbol = g_Sentences[i].caption;

				if (!pflDuration.IsEmpty)
					pflDuration[0] = g_Sentences[i].length;

				return name + strlen(name) + 1;
			}
		}
		return BytePtr.Null;
	}

	public static string? VOX_LookupStringManaged(ReadOnlySpan<char> pSentenceName, out int sentencenum) {
		int num = -1;
		BytePtr psz = VOX_LookupString(pSentenceName, new Span<int>(ref num));
		sentencenum = num;
		return psz.IsNull ? null : Str(psz);
	}


	// Abstraction for sentence name array
	public static ReadOnlySpan<char> VOX_SentenceNameFromIndex(int sentencenum) {
		if (sentencenum < g_Sentences.Count)
			return Str(g_Sentences[sentencenum].pName);
		return null;
	}

	internal static void VOX_AddTempSentence(string name, string text) {
		BytePtr p = AllocBuffer(name.Length + 1 + text.Length + 1);
		for (int i = 0; i < name.Length; i++)
			p[i] = (byte)name[i];
		for (int i = 0; i < text.Length; i++)
			p[name.Length + 1 + i] = (byte)text[i];

		Sentence_t pSentence = new();
		pSentence.pName = p;
		pSentence.length = 0;
		g_Sentences.Add(pSentence);
	}

	internal static void VOX_RemoveLastSentence() {
		Sentence_t last = g_Sentences[^1];
		g_Sentences.RemoveAt(g_Sentences.Count - 1);
	}
}
