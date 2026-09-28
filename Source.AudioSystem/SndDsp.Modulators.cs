using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source.AudioSystem;

public class LfoWav     // lfo or envelope wave table
{
	public int type;            // lfo type
	public Dly? pdly;           // delay holds wav values and step pointers

	public void Clear() {
		type = 0;
		pdly = null;
	}
}

public sealed class Lfo : DspProcessor
{
	public bool fused;          // true if slot take

	public Dly? pdly;           // delay points to lfo wav within lfowav_t (don't free this)

	public int gain;

	public float f;             // playback frequency in hz

	public Pos pos;             // current position within wav table, looping
	public PosOne pos1;         // current position within wav table, one shot

	public int foneshot;        // true - one shot only, don't repeat

	public void Clear() {
		fused = false;
		pdly = null;
		gain = 0;
		f = 0;
		pos = default;
		pos1 = default;
		foneshot = 0;
	}

	public override int GetNext(int x) => SndDsp.LFO_GetNext(this, x);
	public override void GetNextN(Span<PortableSamplePair> pbuffer, int SampleCount, int op) => SndDsp.LFO_GetNextN(this, pbuffer, SampleCount, op);
	public override void Free() => SndDsp.LFO_Free(this);
	public override void Mod(float v) => SndDsp.LFO_Mod(this, v);
}

public sealed class Ptc : DspProcessor
{
	public bool fused;

	public Dly? pdly_in;        // input buffer space
	public Dly? pdly_out;       // output buffer space

	public int[]? pin;          // input buffer (pdly_in->w)
	public int[]? pout;         // output buffer (pdly_out->w)

	public int cin;             // # samples in input buffer
	public int cout;            // # samples in output buffer

	public int cxfade;          // # samples in crossfade segment
	public int ccut;            // # samples to cut
	public int cduplicate;      // # samples to duplicate (redundant - same as ccut)

	public int iin;             // current index into input buffer (reading)

	public PosOne psn;          // stepping index through output buffer

	public bool fdup;           // true if duplicating, false if cutting

	public float fstep;         // pitch shift & time compress/expand

	public void Clear() {
		fused = false;
		pdly_in = pdly_out = null;
		pin = pout = null;
		cin = cout = 0;
		cxfade = ccut = cduplicate = 0;
		iin = 0;
		psn = default;
		fdup = false;
		fstep = 0;
	}

	public override int GetNext(int x) => SndDsp.PTC_GetNext(this, x);
	public override void GetNextN(Span<PortableSamplePair> pbuffer, int SampleCount, int op) => SndDsp.PTC_GetNextN(this, pbuffer, SampleCount, op);
	public override void Free() => SndDsp.PTC_Free(this);
	public override void Mod(float v) => SndDsp.PTC_Mod(this, v);
}

[InlineArray(SndDsp.CENVRMPS)]
public struct RmpArray4
{
	Rmp element;
}

public sealed class Env : DspProcessor
{
	public bool fused;

	public bool fhitend;        // true if done
	public bool fexp;           // true if exponential ramps

	public int ienv;            // current ramp
	public RmpArray4 rmps;      // ramps

	public void Clear() {
		fused = false;
		fhitend = false;
		fexp = false;
		ienv = 0;
		rmps = default;
	}

	public override int GetNext(int x) => SndDsp.ENV_GetNext(this, x);
	public override void GetNextN(Span<PortableSamplePair> pbuffer, int SampleCount, int op) => SndDsp.ENV_GetNextN(this, pbuffer, SampleCount, op);
	public override void Free() => SndDsp.ENV_Free(this);
	public override void Mod(float v) => SndDsp.ENV_Mod(this, v);
}

public sealed class Efo : DspProcessor
{
	public bool fused;

	public int xout;            // current output value

	// gate params

	public bool bgate;          // if true, gate function is on

	public bool bgateon;        // if true, gate is on
	public bool bexp;           // if true, use exponential fade out

	public int thresh;          // amplitude threshold for gate on
	public int thresh_off;      // amplitidue threshold for gate off

	public float attack_time;   // gate attack time in seconds
	public float decay_time;    // gate decay time in seconds

	public Rmp rmp_attack;      // gate on ramp - attack
	public Rmp rmp_decay;       // gate off ramp - decay

	public void Clear() {
		fused = false;
		xout = 0;
		bgate = bgateon = bexp = false;
		thresh = thresh_off = 0;
		attack_time = decay_time = 0;
		rmp_attack = default;
		rmp_decay = default;
	}

	public override int GetNext(int x) => SndDsp.EFO_GetNext(this, x);
	public override void GetNextN(Span<PortableSamplePair> pbuffer, int SampleCount, int op) => SndDsp.EFO_GetNextN(this, pbuffer, SampleCount, op);
	public override void Free() => SndDsp.EFO_Free(this);
	public override void Mod(float v) => SndDsp.EFO_Mod(this, v);
}

public sealed class Crs : DspProcessor
{
	public bool fused;

	public Mdy? pmdy;           // modulatable delay
	public Lfo? plfo;           // modulating lfo

	public int lfoprev;         // previous modulator value from lfo

	public void Clear() {
		fused = false;
		pmdy = null;
		plfo = null;
		lfoprev = 0;
	}

	public override int GetNext(int x) => SndDsp.CRS_GetNext(this, x);
	public override void GetNextN(Span<PortableSamplePair> pbuffer, int SampleCount, int op) => SndDsp.CRS_GetNextN(this, pbuffer, SampleCount, op);
	public override void Free() => SndDsp.CRS_Free(this);
	public override void Mod(float v) => SndDsp.CRS_Mod(this, v);
}

public sealed class Amp : DspProcessor
{
	public bool fused;

	public int gain;            // amplification 0-6.0 * PMAX
	public int gain_max;        // original gain setting
	public int distmix;         // 0-1.0 mix of distortion with clean * PMAX
	public int vfeed;           // 0-1.0 feedback with distortion * PMAX
	public int vthresh;         // amplitude of clipping threshold 0..32768


	public bool fchanging;      // true if modulating to new amp value
	public float ramptime;      // ramp 'glide' time - time in seconds to change between values
	public int mtime;           // time in samples between amp changes. 0 implies no self-modulating
	public int mtimecur;        // current time in samples until next amp change
	public int depth;           // modulate amp from A to A - (A*depth)  depth 0-1.0
	public bool brand;          // if true, use random modulation otherwise alternate btwn max/min
	public Rmp rmp_interp;      // interpolation ramp 0...PMAX

	public void Clear() {
		fused = false;
		gain = gain_max = distmix = vfeed = vthresh = 0;
		fchanging = false;
		ramptime = 0;
		mtime = mtimecur = 0;
		depth = 0;
		brand = false;
		rmp_interp = default;
	}

	public override int GetNext(int x) => SndDsp.AMP_GetNext(this, x);
	public override void GetNextN(Span<PortableSamplePair> pbuffer, int SampleCount, int op) => SndDsp.AMP_GetNextN(this, pbuffer, SampleCount, op);
	public override void Free() => SndDsp.AMP_Free(this);
	public override void Mod(float v) => SndDsp.AMP_Mod(this, v);
}

public sealed class Nul : DspProcessor
{
	public int type;

	public void Clear() {
		type = 0;
	}

	public override int GetNext(int x) => SndDsp.NULL_GetNext(this, x);
	public override void GetNextN(Span<PortableSamplePair> pbuffer, int SampleCount, int op) => SndDsp.NULL_GetNextN(this, pbuffer, SampleCount, op);
	public override void Free() => SndDsp.NULL_Free(this);
	public override void Mod(float v) => SndDsp.NULL_Mod(this, v);
}

public static partial class SndDsp
{
	//////////////////////
	// LFO wav definitions
	//////////////////////

	public const int CLFOSAMPS = 512;                   // samples per wav table - single cycle only
	public const int LFOBITS = 14;                  // bits of peak amplitude of lfo wav
	public const int LFOAMP = (1 << LFOBITS) - 1;   // peak amplitude of lfo wav

	//types of lfo wavs

	public const int LFO_SIN = 0;   // sine wav
	public const int LFO_TRI = 1;   // triangle wav
	public const int LFO_SQR = 2;   // square wave, 50% duty cycle
	public const int LFO_SAW = 3;   // forward saw wav
	public const int LFO_RND = 4;   // random wav
	public const int LFO_LOG_IN = 5;    // logarithmic fade in
	public const int LFO_LOG_OUT = 6;   // logarithmic fade out
	public const int LFO_LIN_IN = 7;    // linear fade in
	public const int LFO_LIN_OUT = 8;   // linear fade out
	public const int LFO_MAX = LFO_LIN_OUT;

	public const int CLFOWAV = 9;           // number of LFO wav tables

	static readonly LfoWav[] lfowavs = CreatePool<LfoWav>(CLFOWAV);

	// deallocate lfo wave table. Called only when sound engine exits.

	static void LFOWAV_Free(LfoWav? plw) {
		// free delay

		if (plw != null)
			DLY_Free(plw.pdly);

		plw?.Clear();
	}

	// deallocate all lfo wave tables. Called only when sound engine exits.

	static void LFOWAV_FreeAll() {
		for (int i = 0; i < CLFOWAV; i++)
			LFOWAV_Free(lfowavs[i]);
	}

	// fill lfo array w with count samples of lfo type 'type'
	// all lfo wavs except fade out, rnd, and log_out should start with 0 output

	static void LFOWAV_Fill(Span<int> w, int count, int type) {
		int i, x;
		switch (type) {
			default:
			case LFO_SIN:           // sine wav, all values 0 <= x <= LFOAMP, initial value = 0
				for (i = 0; i < count; i++) {
					x = (int)((float)LFOAMP * MathF.Sin((2.0F * MathF.PI * (float)i / (float)count) + (MathF.PI * 1.5F)));
					w[i] = (x + LFOAMP) / 2;
				}
				break;
			case LFO_TRI:           // triangle wav, all values 0 <= x <= LFOAMP, initial value = 0
				for (i = 0; i < count; i++) {
					w[i] = (int)((float)(2 * LFOAMP * i) / (float)count);

					if (i > count / 2)
						w[i] = (int)((float)(2 * LFOAMP) - (float)(2 * LFOAMP * i) / (float)count);
				}
				break;
			case LFO_SQR:           // square wave, 50% duty cycle, all values 0 <= x <= LFOAMP, initial value = 0
				for (i = 0; i < count; i++)
					w[i] = i > count / 2 ? 0 : LFOAMP;
				break;
			case LFO_SAW:           // forward saw wav, aall values 0 <= x <= LFOAMP, initial value = 0
				for (i = 0; i < count; i++)
					w[i] = (int)((float)LFOAMP * (float)i / (float)count);
				break;
			case LFO_RND:           // random wav, all values 0 <= x <= LFOAMP
				for (i = 0; i < count; i++)
					w[i] = RandomInt(0, LFOAMP);
				break;
			case LFO_LOG_IN:        // logarithmic fade in, all values 0 <= x <= LFOAMP, initial value = 0
				for (i = 0; i < count; i++)
					w[i] = (int)((float)LFOAMP * MathF.Pow((float)i / (float)count, 2));
				break;
			case LFO_LOG_OUT:       // logarithmic fade out, all values 0 <= x <= LFOAMP, initial value = LFOAMP
				for (i = 0; i < count; i++)
					w[i] = (int)((float)LFOAMP * MathF.Pow(1.0F - ((float)i / (float)count), 2));
				break;
			case LFO_LIN_IN:        // linear fade in, all values 0 <= x <= LFOAMP, initial value = 0
				for (i = 0; i < count; i++)
					w[i] = (int)((float)LFOAMP * (float)i / (float)count);
				break;
			case LFO_LIN_OUT:       // linear fade out, all values 0 <= x <= LFOAMP, initial value = LFOAMP
				for (i = 0; i < count; i++)
					w[i] = LFOAMP - (int)((float)LFOAMP * (float)i / (float)count);
				break;
		}
	}

	// allocate all lfo wave tables.  Called only when sound engine loads.

	static void LFOWAV_InitAll() {
		int i;
		Dly? pdly;

		foreach (LfoWav lfowav in lfowavs)
			lfowav.Clear();

		// alloc space for each lfo wav type

		for (i = 0; i < CLFOWAV; i++) {
			pdly = DLY_Alloc(CLFOSAMPS, 0, 0, DLY_PLAIN);

			lfowavs[i].pdly = pdly;
			lfowavs[i].type = i;

			LFOWAV_Fill(pdly!.w, CLFOSAMPS, i);
		}

		// if any dlys fail to alloc, free all

		for (i = 0; i < CLFOWAV; i++) {
			if (lfowavs[i].pdly == null)
				LFOWAV_FreeAll();
		}
	}


	////////////////////////////////////////
	// LFO iterators - one shot and looping
	////////////////////////////////////////

	public const int CLFO = 16; // max active lfos (this steals from active delays)

	static readonly Lfo[] lfos = CreatePool<Lfo>(CLFO);

	static void LFO_Init(Lfo? plfo) { if (plfo != null) plfo.Clear(); }
	static void LFO_InitAll() { for (int i = 0; i < CLFO; i++) LFO_Init(lfos[i]); }
	internal static void LFO_Free(Lfo? plfo) { if (plfo != null) plfo.Clear(); }
	static void LFO_FreeAll() { for (int i = 0; i < CLFO; i++) LFO_Free(lfos[i]); }


	// get step value given desired playback frequency

	static float LFO_HzToStep(float freqHz) {
		float lfoHz;

		// calculate integer and fractional step values,
		// assume an update rate of SOUND_DMA_SPEED samples/sec

		// 1 cycle/CLFOSAMPS * SOUND_DMA_SPEED samps/sec = cycles/sec = current lfo rate
		//
		// lforate * X = freqHz  so X = freqHz/lforate = update rate

		lfoHz = (float)SOUND_DMA_SPEED / (float)CLFOSAMPS;

		return freqHz / lfoHz;
	}

	// return pointer to new lfo

	static Lfo? LFO_Alloc(int wtype, float freqHz, bool foneshot, float gain) {
		int i;
		int type = Math.Min(CLFOWAV - 1, wtype);
		float lfostep;

		for (i = 0; i < CLFO; i++)
			if (!lfos[i].fused) {
				Lfo plfo = lfos[i];

				LFO_Init(plfo);

				plfo.fused = true;
				plfo.pdly = lfowavs[type].pdly;        // pdly in lfo points to wav table data in lfowavs
				plfo.f = freqHz;
				plfo.foneshot = foneshot ? 1 : 0;
				plfo.gain = (int)(gain * PMAX);

				lfostep = LFO_HzToStep(freqHz);

				// init positional pointer (ie: fixed point updater for controlling pitch of lfo)

				if (!foneshot)
					POS_Init(ref plfo.pos, plfo.pdly!.D, lfostep);
				else
					POS_ONE_Init(ref plfo.pos1, plfo.pdly!.D, lfostep);

				return plfo;
			}
		DevMsg("DSP: Warning, failed to allocate LFO.\n");
		return null;
	}

	// get next lfo value
	// Value returned is 0..LFOAMP.  can be normalized by shifting right by LFOBITS
	// To play back at correct passed in frequency, routien should be
	// called once for every output sample (ie: at SOUND_DMA_SPEED)
	// x is dummy param

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static int LFO_GetNext(Lfo plfo, int x) {
		int i;

		// get current position

		if (plfo.foneshot == 0)
			i = POS_GetNext(ref plfo.pos);
		else
			i = POS_ONE_GetNext(ref plfo.pos1);

		// return current sample

		if (plfo.gain == PMAX)
			return plfo.pdly!.w![i];
		else
			return (plfo.pdly!.w![i] * plfo.gain) >> PBITS;
	}


	// batch version for performance

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static void LFO_GetNextN(Lfo plfo, Span<PortableSamplePair> pbuffer, int SampleCount, int op) {
		Span<PortableSamplePair> pb = pbuffer[..SampleCount];

		switch (op) {
			default:
			case OP_LEFT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = LFO_GetNext(plfo, pb[i].Left);
				return;
			case OP_RIGHT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Right = LFO_GetNext(plfo, pb[i].Right);
				return;
			case OP_LEFT_DUPLICATE:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = pb[i].Right = LFO_GetNext(plfo, pb[i].Left);
				return;
		}
	}

	// uses lfowav, rate, foneshot

	// parameter order

	const int lfo_iwav = 0;
	const int lfo_irate = 1;
	const int lfo_ifoneshot = 2;
	const int lfo_igain = 3;

	const int lfo_cparam = 4;           // # of params

	// parameter ranges

	static readonly PrmRng[] lfo_rng = [

		new(lfo_cparam, 0, 0),          // first entry is # of parameters

		new(lfo_iwav, 0.0f, LFO_MAX),   // lfo type to use (LFO_SIN, LFO_RND...)
		new(lfo_irate, 0.0f, 16000.0f), // modulation rate in hz. for MDY, 1/rate = 'glide' time in seconds
		new(lfo_ifoneshot, 0.0f, 1.0f),     // 1.0 if lfo is oneshot
		new(lfo_igain, 0.0f, 10.0f),        // output gain
	];


	static Lfo? LFO_Params(ref Prc pprc) {
		Lfo? plfo;
		bool foneshot = pprc.prm[lfo_ifoneshot] > 0;
		float gain = pprc.prm[lfo_igain];

		plfo = LFO_Alloc((int)pprc.prm[lfo_iwav], pprc.prm[lfo_irate], foneshot, gain);

		return plfo;
	}

	static void LFO_ChangeVal(Lfo plfo, float fhz) {
		float fstep = LFO_HzToStep(fhz);

		// change lfo playback rate to new frequency fhz

		if (plfo.foneshot != 0)
			POS_ChangeVal(ref plfo.pos, fstep);
		else
			POS_ChangeVal(ref plfo.pos1.p, fstep);
	}

	static DspProcessor? LFO_VParams(ref Prc p) {
		PRC_CheckParams(ref p, lfo_rng);
		return LFO_Params(ref p);
	}

	// v is +/- 0-1.0
	// v changes current lfo frequency up/down by +/- v%

	internal static void LFO_Mod(Lfo p, float v) {
		Lfo plfo = p;
		float fhz;
		float fhznew;

		fhz = plfo.f;
		fhznew = fhz * (1.0F + v);

		LFO_ChangeVal(plfo, fhznew);

		return;
	}


	////////////////////////////////////////
	// Time Compress/expand with pitch shift
	////////////////////////////////////////

	// realtime pitch shift - ie: pitch shift without change to playback rate

	public const int CPTCS = 64;

	static readonly Ptc[] ptcs = CreatePool<Ptc>(CPTCS);

	static void PTC_Init(Ptc? pptc) { if (pptc != null) pptc.Clear(); }
	internal static void PTC_Free(Ptc? pptc) {
		if (pptc != null) {
			DLY_Free(pptc.pdly_in);
			DLY_Free(pptc.pdly_out);

			pptc.Clear();
		}
	}
	static void PTC_InitAll() { for (int i = 0; i < CPTCS; i++) PTC_Init(ptcs[i]); }
	static void PTC_FreeAll() { for (int i = 0; i < CPTCS; i++) PTC_Free(ptcs[i]); }



	// Time compressor/expander with pitch shift (ie: pitch changes, playback rate does not)
	//
	// Algorithm:

	// 1) Duplicate or discard chunks of sound to provide tslice * fstep seconds of sound.
	//    (The user-selectable size of the buffer to process is tslice milliseconds in length)
	// 2) Resample this compressed/expanded buffer at fstep to produce a pitch shifted
	//    output with the same duration as the input (ie: #samples out = # samples in, an
	//    obvious requirement for realtime inline processing).

	// timeslice is size in milliseconds of full buffer to process.
	// timeslice * fstep is the size of the expanded/compressed buffer
	// timexfade is length in milliseconds of crossfade region between duplicated or cut sections
	// fstep is % expanded/compressed sound normalized to 0.01-2.0 (1% - 200%)

	// input buffer:

	// iin-->

	// [0...      tslice              ...D]						input samples 0...D (D is NEWEST sample)
	// [0...          ...n][m... tseg ...D]						region to be cut or duplicated m...D

	// [0...   [p..txf1..n][m... tseg ...D]						fade in  region 1 txf1 p...n
	// [0...          ...n][m..[q..txf2..D]						fade out region 2 txf2 q...D


	// pitch up: duplicate into output buffer:	tdup = tseg

	// [0...          ...n][m... tdup ...D][m... tdup ...D]		output buffer size with duplicate region
	// [0...          ...n][m..[p...xf1..n][m... tdup ...D]		fade in p...n while fading out q...D
	// [0...          ...n][m..[q...xf2..D][m... tdup ...D]
	// [0...          ...n][m..[.XFADE...n][m... tdup ...D]		final duplicated output buffer - resample at fstep

	// pitch down: cut into output buffer: tcut = tseg

	// [0...         ...n][m... tcut  ...D]				input samples with cut region delineated m...D
	// [0...         ...n]								output buffer size after cut
	// [0... [q..txf2...D]								fade in txf1 q...D while fade out txf2 p...n
	// [0... [.XFADE ...D]								final cut output buffer - resample at fstep


	static Ptc? PTC_Alloc(float timeslice, float timexfade, float fstep) {

		int i;
		Ptc pptc;
		float tout;
		int cin, cout;
		float tslice = timeslice;
		float txfade = timexfade;
		float tcutdup;

		// find time compressor slot

		for (i = 0; i < CPTCS; i++) {
			if (!ptcs[i].fused)
				break;
		}

		if (i == CPTCS) {
			DevMsg("DSP: Warning, failed to allocate pitch shifter.\n");
			return null;
		}

		pptc = ptcs[i];

		PTC_Init(pptc);

		// get size of region to cut or duplicate

		tcutdup = MathF.Abs((fstep - 1.0F) * timeslice);

		// to prevent buffer overruns:

		// make sure timeslice is greater than cut/dup time

		tslice = Math.Max(tslice, 1.1F * tcutdup);

		// make sure xfade time smaller than cut/dup time, and smaller than (timeslice-cutdup) time

		txfade = Math.Min(txfade, 0.9F * tcutdup);
		txfade = Math.Min(txfade, 0.9F * (tslice - tcutdup));

		pptc.cxfade = MSEC_TO_SAMPS(txfade);
		pptc.ccut = MSEC_TO_SAMPS(tcutdup);
		pptc.cduplicate = MSEC_TO_SAMPS(tcutdup);

		// alloc delay lines (buffers)

		tout = tslice * fstep;

		cin = MSEC_TO_SAMPS(tslice);
		cout = MSEC_TO_SAMPS(tout);

		pptc.pdly_in = DLY_Alloc(cin, 0, 1, DLY_LINEAR);           // alloc input buffer
		pptc.pdly_out = DLY_Alloc(cout, 0, 1, DLY_LINEAR);     // alloc output buffer

		if (pptc.pdly_in == null || pptc.pdly_out == null) {
			PTC_Free(pptc);
			DevMsg("DSP: Warning, failed to allocate delay for pitch shifter.\n");
			return null;
		}

		// buffer pointers

		pptc.pin = pptc.pdly_in.w;
		pptc.pout = pptc.pdly_out.w;

		// input buffer index

		pptc.iin = 0;

		// output buffer index

		POS_ONE_Init(ref pptc.psn, cout, fstep);

		// if fstep > 1.0 we're pitching shifting up, so fdup = true

		pptc.fdup = fstep > 1.0F;

		pptc.cin = cin;
		pptc.cout = cout;

		pptc.fstep = fstep;
		pptc.fused = true;

		return pptc;
	}

	// linear crossfader
	// yfadein - instantaneous value fading in
	// ydafeout -instantaneous value fading out
	// nsamples - duration in #samples of fade
	// isample - index in to fade 0...nsamples-1

	static int xfade(int yfadein, int yfadeout, int nsamples, int isample) {
		int yout;
		int m = (isample << PBITS) / nsamples;

		//	yout = ((yfadein * m) >> PBITS) + ((yfadeout * (PMAX - m)) >> PBITS);
		yout = (yfadeout + (yfadein - yfadeout) * m) >> PBITS;

		return yout;
	}

	// w - pointer to start of input buffer samples
	// v - pointer to start of output buffer samples
	// cin - # of input buffer samples
	// cout = # of output buffer samples
	// cxfade = # of crossfade samples
	// cduplicate = # of samples in duplicate/cut segment

	static void TimeExpand(int[] w, int[] v, int cin, int cout, int cxfade, int cduplicate) {
		int i, j;
		int m;
		int p;
		int q;
		int D;

		// input buffer
		//               xfade source   duplicate
		// [0...........][p.......n][m...........D]

		// output buffer
		//								 xfade region   duplicate
		// [0.....................n][m..[q.......D][m...........D]

		// D - index of last sample in input buffer
		// m - index of 1st sample in duplication region
		// p - index of 1st sample of crossfade source
		// q - index of 1st sample in crossfade region

		D = cin - 1;
		m = cin - cduplicate;
		p = m - cxfade;
		q = cin - cxfade;

		// copy up to crossfade region

		for (i = 0; i < q; i++)
			v[i] = w[i];

		// crossfade region

		j = p;

		for (i = q; i <= D; i++)
			v[i] = xfade(w[j++], w[i], cxfade, i - q);  // fade out p..n, fade in q..D

		// duplicate region

		j = D + 1;

		for (i = m; i <= D; i++)
			v[j++] = w[i];

	}

	// cut ccut samples from end of input buffer, crossfade end of cut section
	// with end of remaining section

	// w - pointer to start of input buffer samples
	// v - pointer to start of output buffer samples
	// cin - # of input buffer samples
	// cout = # of output buffer samples
	// cxfade = # of crossfade samples
	// ccut = # of samples in cut segment

	static void TimeCompress(int[] w, int[] v, int cin, int cout, int cxfade, int ccut) {
		int i, j;
		int m;
		int p;
		int q;
		int D;

		// input buffer
		//								  xfade source
		// [0.....................n][m..[p.......D]

		//              xfade region     cut
		// [0...........][q.......n][m...........D]

		// output buffer
		//               xfade to source
		// [0...........][p.......D]

		// D - index of last sample in input buffer
		// m - index of 1st sample in cut region
		// p - index of 1st sample of crossfade source
		// q - index of 1st sample in crossfade region

		D = cin - 1;
		m = cin - ccut;
		p = cin - cxfade;
		q = m - cxfade;

		// copy up to crossfade region

		for (i = 0; i < q; i++)
			v[i] = w[i];

		// crossfade region

		j = p;

		for (i = q; i < m; i++)
			v[i] = xfade(w[j++], w[i], cxfade, i - q);  // fade out p..n, fade in q..D

		// skip rest of input buffer
	}

	// get next sample

	// put input sample into input (delay) buffer
	// get output sample from output buffer, step by fstep %
	// output buffer is time expanded or compressed version of previous input buffer

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static int PTC_GetNext(Ptc pptc, int x) {
		int iout, xout;
		bool fhitend = false;

		// write x into input buffer
		Assert(pptc.iin < pptc.cin);

		pptc.pin![pptc.iin] = x;

		pptc.iin++;

		// check for end of input buffer

		if (pptc.iin >= pptc.cin)
			fhitend = true;

		// read sample from output buffer, resampling at fstep

		iout = POS_ONE_GetNext(ref pptc.psn);
		Assert(iout < pptc.cout);
		xout = pptc.pout![iout];

		if (fhitend) {
			// if hit end of input buffer (ie: input buffer is full)
			//		reset input buffer pointer
			//		reset output buffer pointer
			//		rebuild entire output buffer (TimeCompress/TimeExpand)

			pptc.iin = 0;

			POS_ONE_Init(ref pptc.psn, pptc.cout, pptc.fstep);

			if (pptc.fdup)
				TimeExpand(pptc.pin, pptc.pout, pptc.cin, pptc.cout, pptc.cxfade, pptc.cduplicate);
			else
				TimeCompress(pptc.pin, pptc.pout, pptc.cin, pptc.cout, pptc.cxfade, pptc.ccut);
		}

		return xout;
	}


	// batch version for performance

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static void PTC_GetNextN(Ptc pptc, Span<PortableSamplePair> pbuffer, int SampleCount, int op) {
		Span<PortableSamplePair> pb = pbuffer[..SampleCount];

		switch (op) {
			default:
			case OP_LEFT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = PTC_GetNext(pptc, pb[i].Left);
				return;
			case OP_RIGHT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Right = PTC_GetNext(pptc, pb[i].Right);
				return;
			case OP_LEFT_DUPLICATE:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = pb[i].Right = PTC_GetNext(pptc, pb[i].Left);
				return;
		}
	}

	// change time compression to new value
	// fstep is new value
	// ramptime is how long change takes in seconds (ramps smoothly), 0 for no ramp

	static void PTC_ChangeVal(Ptc pptc, float fstep, float ramptime) {
		// UNDONE: ignored
		// UNDONE: just realloc time compressor with new fstep
	}

	// uses pitch:
	// 1.0 = playback normal rate
	// 0.5 = cut 50% of sound (2x playback)
	// 1.5 = add 50% sound (0.5x playback)

	// parameter order

	const int ptc_ipitch = 0;
	const int ptc_itimeslice = 1;
	const int ptc_ixfade = 2;

	const int ptc_cparam = 3;           // # of params

	// diffusor parameter ranges

	static readonly PrmRng[] ptc_rng = [

		new(ptc_cparam, 0, 0),              // first entry is # of parameters

		new(ptc_ipitch, 0.1f, 4.0f),        // 0-n.0 where 1.0 = 1 octave up and 0.5 is one octave down
		new(ptc_itimeslice, 20.0f, 300.0f), // in milliseconds - size of sound chunk to analyze and cut/duplicate - 100ms nominal
		new(ptc_ixfade, 1.0f, 200.0f),  // in milliseconds - size of crossfade region between spliced chunks - 20ms nominal
	];


	static Ptc? PTC_Params(ref Prc pprc) {
		Ptc? pptc;

		float pitch = pprc.prm[ptc_ipitch];
		float timeslice = pprc.prm[ptc_itimeslice];
		float txfade = pprc.prm[ptc_ixfade];

		pptc = PTC_Alloc(timeslice, txfade, pitch);

		return pptc;
	}

	static DspProcessor? PTC_VParams(ref Prc p) {
		PRC_CheckParams(ref p, ptc_rng);
		return PTC_Params(ref p);
	}

	// change to new pitch value
	// v is +/- 0-1.0
	// v changes current pitch up/down by +/- v%

	internal static void PTC_Mod(Ptc p, float v) {
		Ptc pptc = p;
		float fstep;
		float fstepnew;

		fstep = pptc.fstep;
		fstepnew = fstep * (1.0F + v);

		PTC_ChangeVal(pptc, fstepnew, 0.01F);
	}


	////////////////////
	// ADSR envelope
	////////////////////

	public const int CENVS = 64;        // max # of envelopes active
	public const int CENVRMPS = 4;      // A, D, S, R

	public const int ENV_LIN = 0;       // linear a,d,s,r
	public const int ENV_EXP = 1;       // exponential a,d,s,r
	public const int ENV_MAX = ENV_EXP;

	const int ENV_BITS = 14;        // bits of resolution of ramp

	static readonly Env[] envs = CreatePool<Env>(CENVS);

	static void ENV_Init(Env? penv) { if (penv != null) penv.Clear(); }
	internal static void ENV_Free(Env? penv) { if (penv != null) penv.Clear(); }
	static void ENV_InitAll() { for (int i = 0; i < CENVS; i++) ENV_Init(envs[i]); }
	static void ENV_FreeAll() { for (int i = 0; i < CENVS; i++) ENV_Free(envs[i]); }


	// allocate ADSR envelope
	// all times are in seconds
	// amp1 - attack amplitude multiplier 0-1.0
	// amp2 - sustain amplitude multiplier 0-1.0
	// amp3 - end of sustain amplitude multiplier 0-1.0

	static Env? ENV_Alloc(int type, float famp1, float famp2, float famp3, float attack, float decay, float sustain, float release, bool fexp) {
		int i;
		Env penv;

		for (i = 0; i < CENVS; i++) {
			if (!envs[i].fused) {

				int amp1 = (int)(famp1 * (1 << ENV_BITS)); // ramp resolution
				int amp2 = (int)(famp2 * (1 << ENV_BITS));
				int amp3 = (int)(famp3 * (1 << ENV_BITS));

				penv = envs[i];

				ENV_Init(penv);

				// UNDONE: ignoring type = ENV_EXP - use oneshot LFOS instead with sawtooth/exponential

				// set up ramps

				Span<Rmp> rmps = penv.rmps;
				RMP_Init(ref rmps[0], attack, 0, amp1, true);
				RMP_Init(ref rmps[1], decay, amp1, amp2, true);
				RMP_Init(ref rmps[2], sustain, amp2, amp3, true);
				RMP_Init(ref rmps[3], release, amp3, 0, true);

				penv.ienv = 0;
				penv.fused = true;
				penv.fhitend = false;
				penv.fexp = fexp;
				return penv;
			}
		}
		DevMsg("DSP: Warning, failed to allocate envelope.\n");
		return null;
	}


	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static int ENV_GetNext(Env penv, int x) {
		if (!penv.fhitend) {
			int i;
			int y;

			Span<Rmp> rmps = penv.rmps;

			i = penv.ienv;
			y = RMP_GetNext(ref rmps[i]);

			// check for next ramp

			if (rmps[i].fhitend)
				i++;

			penv.ienv = i;

			// check for end of all ramps

			if (i > 3)
				penv.fhitend = true;

			// multiply input signal by ramp

			if (penv.fexp)
				return (((x * y) >> ENV_BITS) * y) >> ENV_BITS;
			else
				return (x * y) >> ENV_BITS;
		}

		return 0;
	}


	// batch version for performance

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static void ENV_GetNextN(Env penv, Span<PortableSamplePair> pbuffer, int SampleCount, int op) {
		Span<PortableSamplePair> pb = pbuffer[..SampleCount];

		switch (op) {
			default:
			case OP_LEFT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = ENV_GetNext(penv, pb[i].Left);
				return;
			case OP_RIGHT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Right = ENV_GetNext(penv, pb[i].Right);
				return;
			case OP_LEFT_DUPLICATE:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = pb[i].Right = ENV_GetNext(penv, pb[i].Left);
				return;
		}
	}

	// uses lfowav, amp1, amp2, amp3, attack, decay, sustain, release
	// lfowav is type, currently ignored - ie: LFO_LIN_IN, LFO_LOG_IN

	// parameter order

	const int env_itype = 0;
	const int env_iamp1 = 1;
	const int env_iamp2 = 2;
	const int env_iamp3 = 3;
	const int env_iattack = 4;
	const int env_idecay = 5;
	const int env_isustain = 6;
	const int env_irelease = 7;
	const int env_ifexp = 8;
	const int env_cparam = 9;           // # of params

	// parameter ranges

	static readonly PrmRng[] env_rng = [

		new(env_cparam, 0, 0),          // first entry is # of parameters

		new(env_itype, 0.0f, ENV_MAX),  // ENV_LINEAR, ENV_LOG - currently ignored
		new(env_iamp1, 0.0f, 1.0f),     // attack peak amplitude 0-1.0
		new(env_iamp2, 0.0f, 1.0f),     // decay target amplitued 0-1.0
		new(env_iamp3, 0.0f, 1.0f),     // sustain target amplitude 0-1.0
		new(env_iattack, 0.0f, 20000.0f),   // attack time in milliseconds
		new(env_idecay, 0.0f, 20000.0f),    // envelope decay time in milliseconds
		new(env_isustain, 0.0f, 20000.0f),  // sustain time in milliseconds
		new(env_irelease, 0.0f, 20000.0f),  // release time in milliseconds
		new(env_ifexp, 0.0f, 1.0f),     // 1.0 if exponential ramps
	];

	static Env? ENV_Params(ref Prc pprc) {
		Env? penv;

		float type = pprc.prm[env_itype];
		float amp1 = pprc.prm[env_iamp1];
		float amp2 = pprc.prm[env_iamp2];
		float amp3 = pprc.prm[env_iamp3];
		float attack = pprc.prm[env_iattack] / 1000.0F;
		float decay = pprc.prm[env_idecay] / 1000.0F;
		float sustain = pprc.prm[env_isustain] / 1000.0F;
		float release = pprc.prm[env_irelease] / 1000.0F;
		float fexp = pprc.prm[env_ifexp];
		bool bexp;

		bexp = fexp > 0.0;
		penv = ENV_Alloc((int)type, amp1, amp2, amp3, attack, decay, sustain, release, bexp);
		return penv;
	}

	static DspProcessor? ENV_VParams(ref Prc p) {
		PRC_CheckParams(ref p, env_rng);
		return ENV_Params(ref p);
	}

	internal static void ENV_Mod(Env p, float v) { return; }

	//////////////////////////
	// Gate & envelope follower
	//////////////////////////

	public const int CEFOS = 64;        // max # of envelope followers active

	static readonly Efo[] efos = CreatePool<Efo>(CEFOS);

	static void EFO_Init(Efo? pefo) { if (pefo != null) pefo.Clear(); }
	internal static void EFO_Free(Efo? pefo) { if (pefo != null) pefo.Clear(); }
	static void EFO_InitAll() { for (int i = 0; i < CEFOS; i++) EFO_Init(efos[i]); }
	static void EFO_FreeAll() { for (int i = 0; i < CEFOS; i++) EFO_Free(efos[i]); }

	// return true when gate is off AND decay ramp has hit end

	static bool EFO_GateOff(Efo pefo) {
		return !pefo.bgateon && RMP_HitEnd(ref pefo.rmp_decay);
	}


	// allocate enveloper follower

	const int EFO_HYST_AMP = 1000;      // hysteresis amplitude

	static Efo? EFO_Alloc(float threshold, float attack_sec, float decay_sec, bool bexp) {
		int i;
		Efo pefo;

		for (i = 0; i < CEFOS; i++) {
			if (!efos[i].fused) {
				pefo = efos[i];

				EFO_Init(pefo);

				pefo.xout = 0;
				pefo.fused = true;

				// init gate params

				pefo.bgate = threshold > 0.0;

				if (pefo.bgate) {
					pefo.attack_time = attack_sec;
					pefo.decay_time = decay_sec;

					RMP_Init(ref pefo.rmp_attack, attack_sec, 0, PMAX, false);
					RMP_Init(ref pefo.rmp_decay, decay_sec, PMAX, 0, false);
					RMP_SetEnd(ref pefo.rmp_attack);
					RMP_SetEnd(ref pefo.rmp_decay);

					pefo.thresh = (int)threshold;
					pefo.thresh_off = (int)Math.Max(1.0f, threshold - EFO_HYST_AMP);
					pefo.bgateon = false;
					pefo.bexp = bexp;
				}

				return pefo;
			}
		}

		DevMsg("DSP: Warning, failed to allocate envelope follower.\n");
		return null;
	}

	// values of L for CEFO_BITS_DIVIDE: L = (1 - 1/(1 << CEFO_BITS_DIVIDE))
	// 1	L = 0.5
	// 2	L = 0.75
	// 3	L = 0.875
	// 4	L = 0.9375
	// 5	L = 0.96875
	// 6	L = 0.984375
	// 7	L = 0.9921875
	// 8	L = 0.99609375
	// 9	L = 0.998046875
	// 10	L = 0.9990234375
	// 11	L = 0.99951171875
	// 12	L = 0.999755859375


	// decay time constant for values of L, for E = 10^-3 = 60dB of attenuation
	//
	//	Neff = Ln E / Ln L  = -6.9077552 / Ln L
	//
	//  1	L = 0.5				Neff = 10 samples
	//  2	L = 0.75			Neff = 24
	//  3	L = 0.875			Neff = 51
	//  4	L = 0.9375			Neff = 107
	//  5	L = 0.96875			Neff = 217
	//  6	L = 0.984375		Neff = 438
	// 	7	L = 0.9921875		Neff = 880
	// 	8	L = 0.99609375		Neff = 1764
	//  9	L = 0.998046875		Neff = 3533
	// 10	L = 0.9990234375	Neff = 7070
	// 11	L = 0.99951171875	Neff = 14143
	// 12	L = 0.999755859375	Neff = 28290

	const int CEFO_BITS = 11;           // 14143 samples in gate window (3hz)

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static int EFO_GetNext(Efo pefo, int x) {
		int r;
		int xa = Math.Abs(x);
		int xdif;


		// get envelope:
		//		Cn = L * Cn-1 + ( 1 - L ) * |x|

		// which simplifies to:
		//		Cn = |x| + (Cn-1 - |x|) * L

		// for  0 < L < 1

		// increasing L increases time to rise or fall to a new input level

		// so: increasing CEFO_BITS_DIVIDE increases rise/fall time

		// where: L = (1 - 1/(1 << CEFO_BITS))
		// xdif = Cn-1 - |x|
		// so:    xdif * L = xdif - xdif / (1 << CEFO_BITS) = ((xdif << CEFO_BITS) - xdif ) >> CEFO_BITS

		xdif = pefo.xout - xa;

		pefo.xout = xa + (((xdif << CEFO_BITS) - xdif) >> CEFO_BITS);

		if (pefo.bgate) {
			// gate

			bool bgateon_prev = pefo.bgateon;

			// gate hysteresis

			if (bgateon_prev)
				// gate was on - it's off only if amp drops below thresh_off
				pefo.bgateon = pefo.xout >= pefo.thresh_off;
			else
				// gate was off - it's on only if amp > thresh
				pefo.bgateon = pefo.xout >= pefo.thresh;

			if (pefo.bgateon) {
				// gate is on

				if (bgateon_prev && RMP_HitEnd(ref pefo.rmp_attack))
					return x;       // gate is fully on

				if (!bgateon_prev) {
					// gate just turned on, start ramp attack

					// start attack from previous decay ramp if active

					r = RMP_HitEnd(ref pefo.rmp_decay) ? 0 : RMP_GetNext(ref pefo.rmp_decay);
					RMP_SetEnd(ref pefo.rmp_decay);

					// DevMsg ("GATE ON \n");

					RMP_Init(ref pefo.rmp_attack, pefo.attack_time, r, PMAX, false);

					return (x * r) >> PBITS;
				}

				if (!RMP_HitEnd(ref pefo.rmp_attack)) {
					r = RMP_GetNext(ref pefo.rmp_attack);

					// gate is on and ramping up

					return (x * r) >> PBITS;
				}

			}
			else {
				// gate is fully off

				if (!bgateon_prev && RMP_HitEnd(ref pefo.rmp_decay))
					return 0;

				if (bgateon_prev) {
					// gate just turned off, start ramp decay

					// start decay from previous attack ramp if active

					r = RMP_HitEnd(ref pefo.rmp_attack) ? PMAX : RMP_GetNext(ref pefo.rmp_attack);
					RMP_SetEnd(ref pefo.rmp_attack);

					RMP_Init(ref pefo.rmp_decay, pefo.decay_time, r, 0, false);

					// DevMsg ("GATE OFF \n");

					// if exponential set, gate has exponential ramp down. Otherwise linear ramp down.

					if (pefo.bexp)
						return (((x * r) >> PBITS) * r) >> PBITS;
					else
						return (x * r) >> PBITS;

				}
				else if (!RMP_HitEnd(ref pefo.rmp_decay)) {
					// gate is off and ramping down

					r = RMP_GetNext(ref pefo.rmp_decay);


					// if exponential set, gate has exponential ramp down. Otherwise linear ramp down.

					if (pefo.bexp)
						return (((x * r) >> PBITS) * r) >> PBITS;
					else
						return (x * r) >> PBITS;
				}
			}

			return x;
		}

		return pefo.xout;
	}


	// batch version for performance

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static void EFO_GetNextN(Efo pefo, Span<PortableSamplePair> pbuffer, int SampleCount, int op) {
		Span<PortableSamplePair> pb = pbuffer[..SampleCount];

		switch (op) {
			default:
			case OP_LEFT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = EFO_GetNext(pefo, pb[i].Left);
				return;
			case OP_RIGHT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Right = EFO_GetNext(pefo, pb[i].Right);
				return;
			case OP_LEFT_DUPLICATE:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = pb[i].Right = EFO_GetNext(pefo, pb[i].Left);
				return;
		}
	}
	// parameter order

	const int efo_ithreshold = 0;
	const int efo_iattack = 1;
	const int efo_idecay = 2;
	const int efo_iexp = 3;

	const int efo_cparam = 4;           // # of params

	// parameter ranges

	static readonly PrmRng[] efo_rng = [

		new(efo_cparam, 0, 0),          // first entry is # of parameters

		new(efo_ithreshold, -140.0f, 0.0f), // gate threshold in db. if 0.0 then no gate.
		new(efo_iattack, 0.0f, 20000.0f),   // attack time in milliseconds
		new(efo_idecay, 0.0f, 20000.0f),    // envelope decay time in milliseconds
		new(efo_iexp, 0.0f, 1.0f),      // if 1, use exponential decay ramp (for more realistic reverb tail)

	];

	static Efo? EFO_Params(ref Prc pprc) {
		Efo? penv;

		float threshold = Gain_To_Amplitude(dB_To_Gain(pprc.prm[efo_ithreshold]));
		float attack = pprc.prm[efo_iattack] / 1000.0F;
		float decay = pprc.prm[efo_idecay] / 1000.0F;
		float fexp = pprc.prm[efo_iexp];
		bool bexp;

		// check for no gate

		if (pprc.prm[efo_ithreshold] == 0.0)
			threshold = 0.0f;

		bexp = fexp > 0.0;

		penv = EFO_Alloc(threshold, attack, decay, bexp);
		return penv;
	}

	static DspProcessor? EFO_VParams(ref Prc p) {
		PRC_CheckParams(ref p, efo_rng);
		return EFO_Params(ref p);
	}

	internal static void EFO_Mod(Efo p, float v) { return; }


	///////////////////////////////////////////
	// Chorus - lfo modulated delay
	///////////////////////////////////////////


	public const int CCRSS = 64;                // max number chorus' active

	static readonly Crs[] crss = CreatePool<Crs>(CCRSS);

	static void CRS_Init(Crs? pcrs) { if (pcrs != null) pcrs.Clear(); }
	internal static void CRS_Free(Crs? pcrs) {
		if (pcrs != null) {
			MDY_Free(pcrs.pmdy);
			LFO_Free(pcrs.plfo);
			pcrs.Clear();
		}
	}


	static void CRS_InitAll() { for (int i = 0; i < CCRSS; i++) CRS_Init(crss[i]); }
	static void CRS_FreeAll() { for (int i = 0; i < CCRSS; i++) CRS_Free(crss[i]); }

	// fstep is base pitch shift, ie: floating point step value, where 1.0 = +1 octave, 0.5 = -1 octave
	// lfotype is LFO_SIN, LFO_RND, LFO_TRI etc (LFO_RND for chorus, LFO_SIN for flange)
	// fHz is modulation frequency in Hz
	// depth is modulation depth, 0-1.0
	// mix is mix of chorus and clean signal

	const int CRS_DELAYMAX = 100;       // max milliseconds of sweepable delay
	const int CRS_RAMPTIME = 5;     // milliseconds to ramp between new delay values

	static Crs? CRS_Alloc(int lfotype, float fHz, float fdepth, float mix) {

		int i;
		Crs pcrs;
		Dly? pdly;
		Mdy? pmdy;
		Lfo? plfo;
		float ramptime;
		int D;

		// find free chorus slot

		for (i = 0; i < CCRSS; i++) {
			if (!crss[i].fused)
				break;
		}

		if (i == CCRSS) {
			DevMsg("DSP: Warning, failed to allocate chorus.\n");
			return null;
		}

		pcrs = crss[i];

		CRS_Init(pcrs);

		D = (int)(fdepth * MSEC_TO_SAMPS(CRS_DELAYMAX));        // sweep from 0 - n milliseconds

		ramptime = (float)CRS_RAMPTIME / 1000.0f;               // # milliseconds to ramp between new values

		pdly = DLY_Alloc(D, 0, 1, DLY_LINEAR);

		pmdy = MDY_Alloc(pdly, ramptime, 0.0f, 0.0f, mix);

		plfo = LFO_Alloc(lfotype, fHz, false, 1.0f);

		if (plfo == null || pmdy == null) {
			LFO_Free(plfo);
			MDY_Free(pmdy);
			DevMsg("DSP: Warning, failed to allocate lfo or mdy for chorus.\n");
			return null;
		}

		pcrs.pmdy = pmdy;
		pcrs.plfo = plfo;
		pcrs.fused = true;

		return pcrs;
	}

	// return next chorused sample (modulated delay) mixed with input sample

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static int CRS_GetNext(Crs pcrs, int x) {
		int l;
		int y;

		// get current mod delay value

		y = MDY_GetNext(pcrs.pmdy!, x);

		// get next lfo value for modulation
		// note: lfo must return 0 as first value

		l = LFO_GetNext(pcrs.plfo!, x);

		// if modulator has changed, change mdy

		if (l != pcrs.lfoprev) {
			// calculate new tap starts at D)

			int D = pcrs.pmdy!.pdly!.D0;
			int tap;

			// lfo should always output values 0 <= l <= LFOMAX

			if (l < 0)
				l = 0;

			tap = D - ((l * D) >> LFOBITS);

			MDY_ChangeVal(pcrs.pmdy!, tap);

			pcrs.lfoprev = l;
		}

		return y;
	}


	// batch version for performance

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static void CRS_GetNextN(Crs pcrs, Span<PortableSamplePair> pbuffer, int SampleCount, int op) {
		Span<PortableSamplePair> pb = pbuffer[..SampleCount];

		switch (op) {
			default:
			case OP_LEFT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = CRS_GetNext(pcrs, pb[i].Left);
				return;
			case OP_RIGHT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Right = CRS_GetNext(pcrs, pb[i].Right);
				return;
			case OP_LEFT_DUPLICATE:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = pb[i].Right = CRS_GetNext(pcrs, pb[i].Left);
				return;
		}
	}

	// parameter order

	const int crs_ilfotype = 0;
	const int crs_irate = 1;
	const int crs_idepth = 2;
	const int crs_imix = 3;

	const int crs_cparam = 4;


	// parameter ranges

	static readonly PrmRng[] crs_rng = [

		new(crs_cparam, 0, 0),              // first entry is # of parameters

		new(crs_ilfotype, 0, LFO_MAX),      // lfotype is LFO_SIN, LFO_RND, LFO_TRI etc (LFO_RND for chorus, LFO_SIN for flange)
		new(crs_irate, 0.0f, 1000.0f),      // rate is modulation frequency in Hz
		new(crs_idepth, 0.0f, 1.0f),            // depth is modulation depth, 0-1.0
		new(crs_imix, 0.0f, 1.0f),          // mix is mix of chorus and clean signal

	];

	// uses pitch, lfowav, rate, depth

	static Crs? CRS_Params(ref Prc pprc) {
		Crs? pcrs;

		pcrs = CRS_Alloc((int)pprc.prm[crs_ilfotype], pprc.prm[crs_irate], pprc.prm[crs_idepth], pprc.prm[crs_imix]);

		return pcrs;
	}

	static DspProcessor? CRS_VParams(ref Prc p) {
		PRC_CheckParams(ref p, crs_rng);
		return CRS_Params(ref p);
	}

	internal static void CRS_Mod(Crs p, float v) { return; }


	////////////////////////////////////////////////////
	// amplifier - modulatable gain, distortion
	////////////////////////////////////////////////////

	public const int CAMPS = 64;                // max number amps active

	const int AMPSLEW = 10;             // milliseconds of slew time between gain changes

	static readonly Amp[] amps = CreatePool<Amp>(CAMPS);

	static void AMP_Init(Amp? pamp) { if (pamp != null) pamp.Clear(); }
	internal static void AMP_Free(Amp? pamp) {
		if (pamp != null)
			pamp.Clear();
	}


	static void AMP_InitAll() { for (int i = 0; i < CAMPS; i++) AMP_Init(amps[i]); }
	static void AMP_FreeAll() { for (int i = 0; i < CAMPS; i++) AMP_Free(amps[i]); }


	static Amp? AMP_Alloc(float gain, float vthresh, float distmix, float vfeed, float ramptime, float modtime, float depth, bool brand) {
		int i;
		Amp pamp;

		// find free amp slot

		for (i = 0; i < CAMPS; i++) {
			if (!amps[i].fused)
				break;
		}

		if (i == CAMPS) {
			DevMsg("DSP: Warning, failed to allocate amp.\n");
			return null;
		}

		pamp = amps[i];

		AMP_Init(pamp);

		pamp.fused = true;

		pamp.gain = (int)(gain * PMAX);
		pamp.gain_max = (int)(gain * PMAX);
		pamp.distmix = (int)(distmix * PMAX);
		pamp.vfeed = (int)(vfeed * PMAX);
		pamp.vthresh = (int)(vthresh * 32767.0f);

		// modrate,	0.01, 200.0},		// frequency at which amplitude values change to new random value. 0 is no self-modulation
		// moddepth,	0.0, 1.0},			// how much amplitude changes (decreases) from current value (0-1.0)
		// modglide,	0.01, 100.0},		// glide time between mapcur and ampnew in milliseconds

		pamp.ramptime = ramptime;
		pamp.mtime = SEC_TO_SAMPS(modtime);
		pamp.mtimecur = pamp.mtime;
		pamp.depth = (int)(depth * PMAX);
		pamp.brand = brand;

		return pamp;
	}

	// return next amplified sample

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static int AMP_GetNext(Amp pamp, int x) {
		int y = x;
		int d;

		// if distortion is on, add distortion, feedback

		if (pamp.vthresh < PMAX && pamp.distmix != 0) {
			int vthresh = pamp.vthresh;

			/* 		if ( pamp->vfeed > 0.0 )
					{
						// UNDONE: feedback
					}
			*/
			// clip distort

			d = y > vthresh ? vthresh : (y < -vthresh ? -vthresh : y);

			// mix distorted with clean (1.0 = full distortion)

			if (pamp.distmix < PMAX)
				y = y + (((d - y) * pamp.distmix) >> PBITS);
			else
				y = d;
		}

		// get output for current gain value

		int xout = (y * pamp.gain) >> PBITS;

		if (!pamp.fchanging && pamp.mtime == 0) {
			// if not modulating and not self modulating, return right away

			return xout;
		}

		if (pamp.fchanging) {
			// modulating...

			// get next gain value

			pamp.gain = RMP_GetNext(ref pamp.rmp_interp); // 0...next gain

			if (RMP_HitEnd(ref pamp.rmp_interp)) {
				// done.

				pamp.fchanging = false;
			}
		}

		// if self-modulating and timer has expired, get next change

		if (pamp.mtime != 0 && pamp.mtimecur-- == 0) {
			pamp.mtimecur = pamp.mtime;

			int gain_new;
			int G1;
			int G2 = pamp.gain_max;

			// modulate between 0 and 100% of gain_max

			G1 = pamp.gain_max - ((pamp.gain_max * pamp.depth) >> PBITS);

			if (pamp.brand)
				gain_new = RandomInt(Math.Min(G1, G2), Math.Max(G1, G2));
			else {
				// alternate between min & max

				gain_new = pamp.gain == G1 ? G2 : G1;
			}

			// set up modulation to new value

			pamp.fchanging = true;

			// init gain ramp - always hit target

			RMP_Init(ref pamp.rmp_interp, pamp.ramptime, pamp.gain, gain_new, false);
		}

		return xout;

	}


	// batch version for performance

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static void AMP_GetNextN(Amp pamp, Span<PortableSamplePair> pbuffer, int SampleCount, int op) {
		Span<PortableSamplePair> pb = pbuffer[..SampleCount];

		switch (op) {
			default:
			case OP_LEFT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = AMP_GetNext(pamp, pb[i].Left);
				return;
			case OP_RIGHT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Right = AMP_GetNext(pamp, pb[i].Right);
				return;
			case OP_LEFT_DUPLICATE:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = pb[i].Right = AMP_GetNext(pamp, pb[i].Left);
				return;
		}
	}

	internal static void AMP_Mod(Amp pamp, float v) {
	}


	// parameter order

	const int amp_gain = 0;
	const int amp_vthresh = 1;
	const int amp_distmix = 2;
	const int amp_vfeed = 3;
	const int amp_imodrate = 4;
	const int amp_imoddepth = 5;
	const int amp_imodglide = 6;
	const int amp_irand = 7;
	const int amp_cparam = 8;


	// parameter ranges

	static readonly PrmRng[] amp_rng = [

		new(amp_cparam, 0, 0),              // first entry is # of parameters

		new(amp_gain, 0.0f, 1000.0f),       // amplification
		new(amp_vthresh, 0.0f, 1.0f),           // threshold for distortion (1.0 = no distortion)
		new(amp_distmix, 0.0f, 1.0f),           // mix of clean and distortion (1.0 = full distortion, 0.0 = full clean)
		new(amp_vfeed, 0.0f, 1.0f),         // distortion feedback

		new(amp_imodrate, 0.0f, 200.0f),        // frequency at which amplitude values change to new random value. 0 is no self-modulation
		new(amp_imoddepth, 0.0f, 1.0f),         // how much amplitude changes (decreases) from current value (0-1.0)
		new(amp_imodglide, 0.01f, 100.0f),      // glide time between mapcur and ampnew in milliseconds
		new(amp_irand, 0.0f, 1.0f),         // if 1, use random modulation otherwise alternate from max-min-max
	];

	static Amp? AMP_Params(ref Prc pprc) {
		Amp? pamp;

		float ramptime = 0.0f;
		float modtime = 0.0f;
		float depth = 0.0f;
		float rand = pprc.prm[amp_irand];
		bool brand;

		if (pprc.prm[amp_imodrate] > 0.0F) {
			ramptime = pprc.prm[amp_imodglide] / 1000.0F;          // get ramp time in seconds
			modtime = 1.0F / Math.Max(pprc.prm[amp_imodrate], 0.01F);      // time between modulations in seconds
			depth = pprc.prm[amp_imoddepth];                       // depth of modulations 0-1.0
		}

		brand = rand > 0.0F;

		pamp = AMP_Alloc(pprc.prm[amp_gain], pprc.prm[amp_vthresh], pprc.prm[amp_distmix], pprc.prm[amp_vfeed],
			ramptime, modtime, depth, brand);

		return pamp;
	}

	static DspProcessor? AMP_VParams(ref Prc p) {
		PRC_CheckParams(ref p, amp_rng);
		return AMP_Params(ref p);
	}


	/////////////////
	// NULL processor
	/////////////////

	static readonly Nul[] nuls = CreatePool<Nul>(1);

	static void NULL_Init(Nul? pnul) { }
	static void NULL_InitAll() { }
	internal static void NULL_Free(Nul? pnul) { }
	static void NULL_FreeAll() { }
	static Nul? NULL_Alloc() { return nuls[0]; }

	internal static int NULL_GetNext(Nul p, int x) { return x; }

	internal static void NULL_GetNextN(Nul pnul, Span<PortableSamplePair> pbuffer, int SampleCount, int op) { return; }

	internal static void NULL_Mod(Nul p, float v) { return; }

	static DspProcessor? NULL_VParams(ref Prc p) { return nuls[0]; }

	//////////////////////////
	// DSP processors presets - see dsp_presets.txt
	//////////////////////////




	// init array of processors - first store pfnParam, pfnGetNext and pfnFree functions for type,
	// then call the pfnParam function to initialize each processor

	// prcs - an array of prc structures, all with initialized params
	// count - number of elements in the array

	// returns false if failed to init one or more processors


	static bool PRC_InitAll(Span<Prc> prcs, int count) {
		int i;

		bool fok = true;

		if (count == 0)
			count = 1;

		// set up pointers to XXX_Free, XXX_GetNext and XXX_Params functions

		for (i = 0; i < count; i++) {
			// call param function, store pdata for the processor type

			switch (prcs[i].type) {
				default:
				case PRC_NULL:
					prcs[i].pdata = NULL_VParams(ref prcs[i]);
					break;
				case PRC_DLY:
					prcs[i].pdata = DLY_VParams(ref prcs[i]);
					break;
				case PRC_RVA:
					prcs[i].pdata = RVA_VParams(ref prcs[i]);
					break;
				case PRC_FLT:
					prcs[i].pdata = FLT_VParams(ref prcs[i]);
					break;
				case PRC_CRS:
					prcs[i].pdata = CRS_VParams(ref prcs[i]);
					break;
				case PRC_PTC:
					prcs[i].pdata = PTC_VParams(ref prcs[i]);
					break;
				case PRC_ENV:
					prcs[i].pdata = ENV_VParams(ref prcs[i]);
					break;
				case PRC_LFO:
					prcs[i].pdata = LFO_VParams(ref prcs[i]);
					break;
				case PRC_EFO:
					prcs[i].pdata = EFO_VParams(ref prcs[i]);
					break;
				case PRC_MDY:
					prcs[i].pdata = MDY_VParams(ref prcs[i]);
					break;
				case PRC_DFR:
					prcs[i].pdata = DFR_VParams(ref prcs[i]);
					break;
				case PRC_AMP:
					prcs[i].pdata = AMP_VParams(ref prcs[i]);
					break;
			}

			if (prcs[i].pdata == null)
				fok = false;
		}

		return fok;
	}

	// free individual processor's data

	static void PRC_Free(ref Prc pprc) {
		if (pprc.pdata != null)
			pprc.pdata.Free();
	}

	// free all processors for supplied array
	// prcs - array of processors
	// count - elements in array

	static void PRC_FreeAll(Span<Prc> prcs, int count) {
		for (int i = 0; i < count; i++)
			PRC_Free(ref prcs[i]);
	}

	// get next value for processor - (usually called directly by PSET_GetNext)

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	static int PRC_GetNext(ref Prc pprc, int x) {
		return pprc.pdata!.GetNext(x);
	}

	// automatic parameter range limiting
	// force parameters between specified min/max in param_rng

	static void PRC_CheckParams(ref Prc pprc, PrmRng[] prng) {
		// first entry in param_rng is # of parameters

		int cprm = prng[0].iprm;

		for (int i = 0; i < cprm; i++) {
			// if parameter is 0.0, always allow it (this is 'off' for most params)

			if (pprc.prm[i] != 0.0 && (pprc.prm[i] > prng[i + 1].hi || pprc.prm[i] < prng[i + 1].lo)) {
				DevMsg("DSP: Warning, clamping out of range parameter.\n");
				pprc.prm[i] = Math.Clamp(pprc.prm[i], prng[i + 1].lo, prng[i + 1].hi);
			}
		}
	}
}
