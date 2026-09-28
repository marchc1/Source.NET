global using static Source.AudioSystem.SndDsp;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source.AudioSystem;

// snd_dsp.c -- audio processing routines

//===============================================================================
//
// Digital Signal Processing algorithms for audio FX.
//
// KellyB 2/18/03
//===============================================================================

// Performance notes:

// DSP processing should take no more than 3ms total time per frame to remain on par with hl1
// Assume a min frame rate of 24fps = 42ms per frame
// at 24fps, to maintain 44.1khz output rate, we must process about 1840 mono samples per frame.
// So we must process 1840 samples in 3ms.

// on a 1Ghz CPU (mid-low end CPU) 3ms provides roughly 3,000,000 cycles.
// Thus we have 3e6 / 1840 = 1630 cycles per sample.

public abstract class DspProcessor
{
	public abstract int GetNext(int x);
	public abstract void GetNextN(Span<PortableSamplePair> pbuffer, int SampleCount, int op);
	public abstract void Free();
	public abstract void Mod(float v);
}

[InlineArray(SndDsp.FLT_M + 1)]
public struct FltCoefs
{
	int element;
}

public sealed class Flt : DspProcessor
{
	public bool fused;              // true if slot in use

	public FltCoefs b;              // filter numerator parameters  (convert 0.0-1.0 to 0-PMAX representation)
	public FltCoefs a;              // filter denominator parameters (convert 0.0-1.0 to 0-PMAX representation)
	public FltCoefs w;              // filter state - samples (dimension of max (M, L))
	public int L;                   // filter order numerator (dimension of a[M+1])
	public int M;                   // filter order denominator (dimension of b[L+1])
	public int N;                   // # of series sections - 1 (0 = 1 section, 1 = 2 sections etc)

	public Flt? pf1;                // series cascaded versions of filter
	public Flt? pf2;
	public Flt? pf3;

	public void Clear() {
		fused = false;
		b = default;
		a = default;
		w = default;
		L = M = N = 0;
		pf1 = pf2 = pf3 = null;
	}

	public override int GetNext(int x) => SndDsp.FLT_GetNext(this, x);
	public override void GetNextN(Span<PortableSamplePair> pbuffer, int SampleCount, int op) => SndDsp.FLT_GetNextN(this, pbuffer, SampleCount, op);
	public override void Free() => SndDsp.FLT_Free(this);
	public override void Mod(float v) => SndDsp.FLT_Mod(this, v);
}

// looping position within a wav, with integer and fractional parts
// used for pitch shifting, upsampling/downsampling
// 20 bits of fraction, 8+ bits of integer

public struct Pos
{
	public int step;    // wave table whole and fractional step value
	public int cstep;   // current cummulative step value
	public int pos;     // current position within wav table

	public int D;       // max dimension of array w[0...D] ie: # of samples = D+1
}

// oneshot position within wav
public struct PosOne
{
	public Pos p;               // pos_t

	public bool fhitend;        // flag indicating we hit end of oneshot wav
}

// delay line

public sealed class Dly : DspProcessor
{
	public bool fused;          // true if dly is in use
	public int type;            // delay type

	public int D;               // delay size, in samples
	public int t;               // current tap, <= D
	public int tnew;            // crossfading to tnew
	public int xf;              // crossfade value of t		(0..PMAX)
	public int t1, t2, t3;      // additional taps for multi-tap delays
	public int a1, a2, a3;      // feedback values for taps
	public int D0;              // original delay size (only relevant if calling DLY_ChangeVal)
	public int p;               // circular buffer pointer
	public int[]? w;            // array of samples

	public int a;               // feedback value 0..PMAX,normalized to 0-1.0
	public int b;               // gain value 0..PMAX, normalized to 0-1.0

	public Flt? pflt;           // pointer to filter, if type DLY_LOWPASS

	public void Clear() {
		fused = false;
		type = D = t = tnew = xf = 0;
		t1 = t2 = t3 = 0;
		a1 = a2 = a3 = 0;
		D0 = p = 0;
		w = null;
		a = b = 0;
		pflt = null;
	}

	public override int GetNext(int x) => SndDsp.DLY_GetNext(this, x);
	public override void GetNextN(Span<PortableSamplePair> pbuffer, int SampleCount, int op) => SndDsp.DLY_GetNextN(this, pbuffer, SampleCount, op);
	public override void Free() => SndDsp.DLY_Free(this);
	public override void Mod(float v) => SndDsp.DLY_Mod(this, v);
}

public struct Rmp
{
	public int initval;         // initial ramp value
	public int target;          // final ramp value
	public int sign;            // increasing (1) or decreasing (-1) ramp

	public int yprev;           // previous output value
	public bool fhitend;        // true if hit end of ramp
	public bool bEndAtTime;     // if true, fhitend is true when ramp time is hit (even if target not hit)
								// if false, then fhitend is true only when target is hit
	public PosOne ps;           // current ramp output
}

public sealed class Mdy : DspProcessor
{
	public bool fused;

	public bool fchanging;      // true if modulating to new delay value

	public Dly? pdly;           // delay

	public float ramptime;      // ramp 'glide' time - time in seconds to change between values

	public int mtime;           // time in samples between delay changes. 0 implies no self-modulating
	public int mtimecur;        // current time in samples until next delay change
	public float depth;         // modulate delay from D to D - (D*depth)  depth 0-1.0

	public int mix;             // PMAX as % processed fx signal mix

	public Rmp rmp_interp;      // interpolation ramp 0...PMAX

	public bool bPhaseInvert;   // if true, invert phase of output

	public void Clear() {
		fused = false;
		fchanging = false;
		pdly = null;
		ramptime = 0;
		mtime = mtimecur = 0;
		depth = 0;
		mix = 0;
		rmp_interp = default;
		bPhaseInvert = false;
	}

	public override int GetNext(int x) => SndDsp.MDY_GetNext(this, x);
	public override void GetNextN(Span<PortableSamplePair> pbuffer, int SampleCount, int op) => SndDsp.MDY_GetNextN(this, pbuffer, SampleCount, op);
	public override void Free() => SndDsp.MDY_Free(this);
	public override void Mod(float v) => SndDsp.MDY_Mod(this, v);
}

[InlineArray(SndDsp.CRVA_DLYS)]
public struct RvaDlys
{
	Dly? element;
}

[InlineArray(SndDsp.CRVA_DLYS)]
public struct RvaMdys
{
	Mdy? element;
}

public sealed class Rva : DspProcessor
{
	public bool fused;
	public int m;               // number of parallel plain or lowpass delays
	public int fparallel;       // true if filters in parallel with delays, otherwise single output filter
	public Flt? pflt;           // series filters

	public RvaDlys pdlys;       // array of pointers to delays
	public RvaMdys pmdlys;      // array of pointers to mod delays

	public bool fmoddly;        // true if using mod delays

	public Dly? GetDly(int i) => pdlys[i];
	public void SetDly(int i, Dly? dly) => pdlys[i] = dly;
	public Mdy? GetMdy(int i) => pmdlys[i];
	public void SetMdy(int i, Mdy? mdy) => pmdlys[i] = mdy;

	public void Clear() {
		fused = false;
		m = 0;
		fparallel = 0;
		pflt = null;
		pdlys = default;
		pmdlys = default;
		fmoddly = false;
	}

	public override int GetNext(int x) => SndDsp.RVA_GetNext(this, x);
	public override void GetNextN(Span<PortableSamplePair> pbuffer, int SampleCount, int op) => SndDsp.RVA_GetNextN(this, pbuffer, SampleCount, op);
	public override void Free() => SndDsp.RVA_Free(this);
	public override void Mod(float v) => SndDsp.RVA_Mod(this, v);
}

[InlineArray(SndDsp.CDFR_DLYS)]
public struct DfrDlys
{
	Dly? element;
}

public sealed class Dfr : DspProcessor
{
	public bool fused;
	public int n;                               // series allpass delays
	public readonly int[] w = new int[SndDsp.CDFR_DLYS];       // internal state array for series allpass filters

	public DfrDlys pdlys;                       // array of pointers to delays

	public Dly? GetDly(int i) => pdlys[i];
	public void SetDly(int i, Dly? dly) => pdlys[i] = dly;

	public void Clear() {
		fused = false;
		n = 0;
		Array.Clear(w);
		pdlys = default;
	}

	public override int GetNext(int x) => SndDsp.DFR_GetNext(this, x);
	public override void GetNextN(Span<PortableSamplePair> pbuffer, int SampleCount, int op) => SndDsp.DFR_GetNextN(this, pbuffer, SampleCount, op);
	public override void Free() => SndDsp.DFR_Free(this);
	public override void Mod(float v) => SndDsp.DFR_Mod(this, v);
}

// processor parameter ranges - for validating parameters during allocation of new processor

public record struct PrmRng(int iprm, float lo, float hi);

[InlineArray(SndDsp.CPRCPARAMS)]
public struct PrcParams
{
	float element;
}

// processor definition - one for each running instance of a dsp processor

public struct Prc
{
	public int type;                        // PRC type

	public PrcParams prm;                   // dsp processor parameters - array of floats

	public DspProcessor? pdata;             // processor state data - ie: pdly, pflt etc.
}

public static partial class SndDsp
{
	static T[] CreatePool<T>(int count) where T : new() {
		T[] pool = new T[count];
		for (int i = 0; i < count; i++)
			pool[i] = new T();
		return pool;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] static int SIGN(int d) => d < 0 ? -1 : 1;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] static int ABS(int a) => Math.Abs(a);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int MSEC_TO_SAMPS(float a) => (int)((a * SOUND_DMA_SPEED) / 1000);      // convert milliseconds to # samples in equivalent time
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int MSEC_TO_SAMPS(int a) => (a * SOUND_DMA_SPEED) / 1000;      // convert milliseconds to # samples in equivalent time
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int SEC_TO_SAMPS(float a) => (int)(a * SOUND_DMA_SPEED);             // convert seconds to # samples in equivalent time

	// Suppress the noisy warnings caused by CLIP_DSP
	[MethodImpl(MethodImplOptions.AggressiveInlining)] static int CLIP_DSP(int x) => x;

	const int SOUND_MS_PER_FT = 1;          // sound travels approx 1 foot per millisecond
	const int ROOM_MAX_SIZE = 1000;         // max size in feet of room simulation for dsp

	public const int PBITS = 12;                    // parameter bits
	public const int PMAX = 1 << PBITS;         // parameter max

	// crossfade from y2 to y1 at point r (0 < r < PMAX)

	[MethodImpl(MethodImplOptions.AggressiveInlining)] static int XFADE(int y1, int y2, int r) => y2 + (((y1 - y2) * r) >> PBITS);

	// exponential crossfade from y2 to y1 at point r (0 < r < PMAX)

	[MethodImpl(MethodImplOptions.AggressiveInlining)] static int XFADE_EXP(int y1, int y2, int r) => y2 + (((((y1 - y2) * r) >> PBITS) * r) >> PBITS);

	/////////////////////
	// dsp helpers
	/////////////////////

	// reverse delay pointer

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void DlyPtrReverse(int dlysize, int[] psamps, ref int ppsamp) {
		// when *ppsamp = psamps - 1, it wraps around to *ppsamp = psamps + dlysize

		if (ppsamp < 0)
			ppsamp += dlysize + 1;
	}

	// advance delay pointer

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void DlyPtrForward(int dlysize, int[] psamps, ref int ppsamp) {
		// when *ppsamp = psamps + dlysize + 1, it wraps around to *ppsamp = psamps

		if (ppsamp > dlysize)
			ppsamp -= dlysize + 1;
	}

	// Infinite Impulse Response (feedback) filter, cannonical form

	//  returns single sample 'out' for current input value 'in'
	//  in:				input sample
	//	psamp:			internal state array, dimension max(cdenom,cnumer) + 1
	//  cnumer,cdenom:	numerator and denominator filter orders
	//  denom,numer:	cdenom+1 dimensional arrays of filter params
	//
	//  for cdenom = 4:
	//
	//                1   psamp0(n)     numer0
	// in(n)--->(+)--(*)---.------(*)---->(+)---> out(n)
	//           ^         |               ^
	//           |     [Delay d]           |
	//           |         |               |
	//           | -denom1 |psamp1 numer1  |
	//			 ----(*)---.------(*)-------
	//           ^         |               ^
	//           |     [Delay d]           |
	//           |         |               |
	//           | -denom2 |psamp2 numer2  |
	//			 ----(*)---.------(*)-------
	//           ^         |               ^
	//           |     [Delay d]           |
	//           |         |               |
	//           | -denom3 |psamp3 numer3  |
	//			 ----(*)---.------(*)-------
	//           ^         |               ^
	//           |     [Delay d]           |
	//           |         |               |
	//           | -denom4 |psamp4 numer4  |
	//			 ----(*)---.------(*)-------
	//
	//	for each input sample in:
	//			psamp0 = in - denom1*psamp1 - denom2*psamp2 - ...
	//			out = numer0*psamp0 + numer1*psamp1 + ...
	//			psampi = psampi-1, i = cmax, cmax-1, ..., 1

	static int IIRFilter_Update_OrderN(int cdenom, ref FltCoefs denom, int cnumer, ref FltCoefs numer, ref FltCoefs psamp, int @in) {
		int cmax, i;
		int @out;
		int in0;

		@out = 0;
		in0 = @in;

		cmax = Math.Max(cdenom, cnumer);

		// add input values

		// for (i = 1; i <= cdenom; i++)
		//	psamp[0] -= ( denom[i] * psamp[i] ) >> PBITS;

		for (i = (cdenom >= 2 && cdenom <= 12) ? cdenom : 1; i >= 1; i--)
			in0 -= (denom[i] * psamp[i]) >> PBITS;

		psamp[0] = in0;

		// add output values

		//for (i = 0; i <= cnumer; i++)
		//	out += ( numer[i] * psamp[i] ) >> PBITS;

		for (i = cnumer == 0 ? 0 : ((cnumer >= 2 && cnumer <= 12) ? cnumer : 1); i >= 0; i--)
			@out += (numer[i] * psamp[i]) >> PBITS;

		// update internal state (reverse order)

		for (i = cmax; i >= 1; i--)
			psamp[i] = psamp[i - 1];

		// return current output sample

		return @out;
	}

	// 1st order filter - faster version

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int IIRFilter_Update_Order1(ref FltCoefs denom, int cnumer, ref FltCoefs numer, ref FltCoefs psamp, int @in) {
		int @out;

		if (psamp[0] == 0 && psamp[1] == 0 && @in == 0)
			return 0;

		psamp[0] = @in - ((denom[1] * psamp[1]) >> PBITS);

		@out = ((numer[1] * psamp[1]) + (numer[0] * psamp[0])) >> PBITS;

		psamp[1] = psamp[0];

		return @out;
	}

	// return 'tdelay' delayed sample from delay buffer
	// dlysize:		delay samples
	// psamps:		head of delay buffer psamps[0...dlysize]
	// psamp:		current data pointer
	// sdly:		0...dlysize

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int GetDly(int dlysize, int[] psamps, int psamp, int tdelay) {
		int pout;

		pout = psamp + tdelay;

		if (pout > dlysize)
			pout -= dlysize + 1;

		return psamps[pout];
	}

	// update the delay buffer pointer
	// dlysize:		delay samples
	// psamps:		head of delay buffer psamps[0...dlysize]
	// ppsamp:		data pointer

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void DlyUpdate(int dlysize, int[] psamps, ref int ppsamp) {
		// decrement pointer and fix up on buffer boundary

		// when *ppsamp = psamps-1, it wraps around to *ppsamp = psamps+dlysize

		ppsamp--;
		DlyPtrReverse(dlysize, psamps, ref ppsamp);
	}

	// simple delay with feedback, no filter in feedback line.
	// delaysize:	delay line size in samples
	// tdelay:		tap from this location - <= delaysize
	// psamps:		delay line buffer pointer of dimension delaysize+1
	// ppsamp:		circular pointer, must be init to &psamps[0] before first call
	// fbgain:		feedback value, 0-PMAX (normalized to 0.0-1.0)
	// outgain:		gain
	// in:	input sample

	//                    psamps0(n)  outgain
	// in(n)--->(+)--------.-----(*)-> out(n)
	//           ^         |
	//           |     [Delay d]
	//           |         |
	//           | fbgain  |Wd(n)
	//			 ----(*)---.

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int ReverbSimple(int delaysize, int tdelay, int[] psamps, ref int ppsamp, int fbgain, int outgain, int @in) {
		int @out, sD;

		// get current delay output

		sD = GetDly(delaysize, psamps, ppsamp, tdelay);

		// calculate output + delay * gain

		@out = @in + ((fbgain * sD) >> PBITS);

		// write to delay

		psamps[ppsamp] = @out;

		// advance internal delay pointers

		DlyUpdate(delaysize, psamps, ref ppsamp);

		return (@out * outgain) >> PBITS;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int ReverbSimple_xfade(int delaysize, int tdelay, int tdelaynew, int xf, int[] psamps, ref int ppsamp, int fbgain, int outgain, int @in) {
		int @out, sD;
		int sDnew;

		// crossfade from tdelay to tdelaynew samples. xfade is 0..PMAX

		sD = GetDly(delaysize, psamps, ppsamp, tdelay);
		sDnew = GetDly(delaysize, psamps, ppsamp, tdelaynew);
		sD = sD + (((sDnew - sD) * xf) >> PBITS);

		@out = @in + ((fbgain * sD) >> PBITS);
		psamps[ppsamp] = @out;
		DlyUpdate(delaysize, psamps, ref ppsamp);

		return (@out * outgain) >> PBITS;
	}

	// multitap simple reverb

	// NOTE: tdelay3 > tdelay2 > tdelay1 > t0
	// NOTE: fbgain * 4 < 1!

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int ReverbSimple_multitap(int delaysize, int tdelay0, int tdelay1, int tdelay2, int tdelay3, int[] psamps, ref int ppsamp, int fbgain, int outgain, int @in) {
		int s1, s2, s3, s4, sum;

		s1 = GetDly(delaysize, psamps, ppsamp, tdelay0);
		s2 = GetDly(delaysize, psamps, ppsamp, tdelay1);
		s3 = GetDly(delaysize, psamps, ppsamp, tdelay2);
		s4 = GetDly(delaysize, psamps, ppsamp, tdelay3);

		sum = s1 + s2 + s3 + s4;

		// write to delay

		psamps[ppsamp] = @in + ((s4 * fbgain) >> PBITS);

		// update delay pointers

		DlyUpdate(delaysize, psamps, ref ppsamp);

		return ((sum + @in) * outgain) >> PBITS;
	}

	// modulate smallest tap delay only

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int ReverbSimple_multitap_xfade(int delaysize, int tdelay0, int tdelaynew, int xf, int tdelay1, int tdelay2, int tdelay3, int[] psamps, ref int ppsamp, int fbgain, int outgain, int @in) {
		int s1, s2, s3, s4, sum;
		int sD, sDnew;

		// crossfade from tdelay to tdelaynew tap. xfade is 0..PMAX

		sD = GetDly(delaysize, psamps, ppsamp, tdelay3);
		sDnew = GetDly(delaysize, psamps, ppsamp, tdelaynew);

		s4 = sD + (((sDnew - sD) * xf) >> PBITS);

		s1 = GetDly(delaysize, psamps, ppsamp, tdelay0);
		s2 = GetDly(delaysize, psamps, ppsamp, tdelay1);
		s3 = GetDly(delaysize, psamps, ppsamp, tdelay2);

		sum = s1 + s2 + s3 + s4;

		// write to delay

		psamps[ppsamp] = @in + ((s4 * fbgain) >> PBITS);

		// update delay pointers

		DlyUpdate(delaysize, psamps, ref ppsamp);

		return ((sum + @in) * outgain) >> PBITS;
	}

	// straight delay, no feedback
	//
	// delaysize:	 delay line size in samples
	// tdelay:		 tap from this location - <= delaysize
	// psamps:		 delay line buffer pointer of dimension delaysize+1
	// ppsamp:		 circular pointer, must be init to &psamps[0] before first call
	// in:			 input sample
	//
	//  in(n)--->[Delay d]---> out(n)
	//

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int DelayLinear(int delaysize, int tdelay, int[] psamps, ref int ppsamp, int @in) {
		int @out;

		@out = GetDly(delaysize, psamps, ppsamp, tdelay);

		psamps[ppsamp] = @in;

		DlyUpdate(delaysize, psamps, ref ppsamp);

		return @out;
	}

	// crossfade delay values from tdelay to tdelaynew, with xfade1 for tdelay and xfade2 for tdelaynew. xfade = 0...PMAX

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int DelayLinear_xfade(int delaysize, int tdelay, int tdelaynew, int xf, int[] psamps, ref int ppsamp, int @in) {
		int @out;
		int outnew;

		@out = GetDly(delaysize, psamps, ppsamp, tdelay);

		outnew = GetDly(delaysize, psamps, ppsamp, tdelaynew);

		@out = @out + (((outnew - @out) * xf) >> PBITS);

		psamps[ppsamp] = @in;

		DlyUpdate(delaysize, psamps, ref ppsamp);

		return @out;
	}

	// lowpass reverberator, replace feedback multiplier 'fbgain' in
	// reverberator with a low pass filter

	// delaysize:	delay line size in samples
	// tdelay:		tap from this location - <= delaysize
	// psamps:		delay line buffer pointer of dimension delaysize+1
	// ppsamp:		circular pointer, must be init to &w[0] before first call
	// fbgain:		feedback gain (built into filter gain)
	// outgain:		output gain
	// cnumer:		filter order
	// numer:		filter numerator, 0-PMAX (normalized to 0.0-1.0), cnumer+1 dimensional
	// denom:		filter denominator, 0-PMAX (normalized to 0.0-1.0), cnumer+1 dimensional
	// pfsamps:		filter state, cnumer+1 dimensional
	// in:			input sample

	//            psamps0(n)   	   outgain
	// in(n)--->(+)--------------.----(*)--> out(n)
	//           ^               |
	//           |           [Delay d]
	//           |               |
	//           |  fbgain       |Wd(n)
	//			 --(*)--[Filter])-

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int DelayLowpass(int delaysize, int tdelay, int[] psamps, ref int ppsamp, int fbgain, int outgain, ref FltCoefs denom, int Ll, ref FltCoefs numer, ref FltCoefs pfsamps, int @in) {
		int @out, sD;

		// delay output is filter input

		sD = GetDly(delaysize, psamps, ppsamp, tdelay);

		// filter output, with feedback 'fbgain' baked into filter params

		@out = @in + IIRFilter_Update_Order1(ref denom, Ll, ref numer, ref pfsamps, sD);

		// write to delay

		psamps[ppsamp] = @out;

		// update delay pointers

		DlyUpdate(delaysize, psamps, ref ppsamp);

		// output with gain

		return (@out * outgain) >> PBITS;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int DelayLowpass_xfade(int delaysize, int tdelay, int tdelaynew, int xf, int[] psamps, ref int ppsamp, int fbgain, int outgain, ref FltCoefs denom, int Ll, ref FltCoefs numer, ref FltCoefs pfsamps, int @in) {
		int @out, sD;
		int sDnew;

		// crossfade from tdelay to tdelaynew tap. xfade is 0..PMAX

		sD = GetDly(delaysize, psamps, ppsamp, tdelay);
		sDnew = GetDly(delaysize, psamps, ppsamp, tdelaynew);
		sD = sD + (((sDnew - sD) * xf) >> PBITS);

		// filter output with feedback 'fbgain' baked into filter params

		@out = @in + IIRFilter_Update_Order1(ref denom, Ll, ref numer, ref pfsamps, sD);

		// write to delay

		psamps[ppsamp] = @out;

		// update delay ptrs

		DlyUpdate(delaysize, psamps, ref ppsamp);

		// output with gain

		return (@out * outgain) >> PBITS;
	}

	// delay is multitap tdelay0,tdelay1,tdelay2,tdelay3

	// NOTE: tdelay3 > tdelay2 > tdelay1 > tdelay0
	// NOTE: fbgain * 4 < 1!

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int DelayLowpass_multitap(int delaysize, int tdelay0, int tdelay1, int tdelay2, int tdelay3, int[] psamps, ref int ppsamp, int fbgain, int outgain, ref FltCoefs denom, int Ll, ref FltCoefs numer, ref FltCoefs pfsamps, int @in) {
		int s0, s1, s2, s3, s4, sum;

		s1 = GetDly(delaysize, psamps, ppsamp, tdelay0);
		s2 = GetDly(delaysize, psamps, ppsamp, tdelay1);
		s3 = GetDly(delaysize, psamps, ppsamp, tdelay2);
		s4 = GetDly(delaysize, psamps, ppsamp, tdelay3);

		sum = s1 + s2 + s3 + s4;

		s0 = @in + IIRFilter_Update_Order1(ref denom, Ll, ref numer, ref pfsamps, s4);

		// write to delay

		psamps[ppsamp] = s0;

		// update delay ptrs

		DlyUpdate(delaysize, psamps, ref ppsamp);

		return ((sum + @in) * outgain) >> PBITS;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int DelayLowpass_multitap_xfade(int delaysize, int tdelay0, int tdelaynew, int xf, int tdelay1, int tdelay2, int tdelay3, int[] psamps, ref int ppsamp, int fbgain, int outgain, ref FltCoefs denom, int Ll, ref FltCoefs numer, ref FltCoefs pfsamps, int @in) {
		int s0, s1, s2, s3, s4, sum;

		int sD, sDnew;

		// crossfade from tdelay to tdelaynew tap. xfade is 0..PMAX

		sD = GetDly(delaysize, psamps, ppsamp, tdelay3);
		sDnew = GetDly(delaysize, psamps, ppsamp, tdelaynew);

		s4 = sD + (((sDnew - sD) * xf) >> PBITS);

		s1 = GetDly(delaysize, psamps, ppsamp, tdelay0);
		s2 = GetDly(delaysize, psamps, ppsamp, tdelay1);
		s3 = GetDly(delaysize, psamps, ppsamp, tdelay2);

		sum = s1 + s2 + s3 + s4;

		s0 = @in + IIRFilter_Update_Order1(ref denom, Ll, ref numer, ref pfsamps, s4);

		psamps[ppsamp] = s0;
		DlyUpdate(delaysize, psamps, ref ppsamp);

		return ((sum + @in) * outgain) >> PBITS;
	}

	// linear delay with lowpass filter on delay output and gain stage
	// delaysize:	delay line size in samples
	// tdelay:		delay tap from this location - <= delaysize
	// psamps:		delay line buffer pointer of dimension delaysize+1
	// ppsamp:		circular pointer, must init &psamps[0] before first call
	// fbgain:		feedback gain (ignored)
	// outgain:		output gain
	// cnumer:		filter order
	// numer:		filter numerator, 0-PMAX (normalized to 0.0-1.0), cnumer+1 dimensional
	// denom:		filter denominator, 0-PMAX (normalized to 0.0-1.0), cnumer+1 dimensional
	// pfsamps:		filter state, cnumer+1 dimensional
	// in:			input sample

	//  in(n)--->[Delay d]--->[Filter]-->(*outgain)---> out(n)

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int DelayLinear_lowpass(int delaysize, int tdelay, int[] psamps, ref int ppsamp, int fbgain, int outgain, ref FltCoefs denom, int cnumer, ref FltCoefs numer, ref FltCoefs pfsamps, int @in) {
		int @out, sD;

		// delay output is filter input

		sD = GetDly(delaysize, psamps, ppsamp, tdelay);

		// calc filter output

		@out = IIRFilter_Update_Order1(ref denom, cnumer, ref numer, ref pfsamps, sD);

		// input sample to delay input

		psamps[ppsamp] = @in;

		// update delay pointers

		DlyUpdate(delaysize, psamps, ref ppsamp);

		// output with gain

		return (@out * outgain) >> PBITS;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int DelayLinear_lowpass_xfade(int delaysize, int tdelay, int tdelaynew, int xf, int[] psamps, ref int ppsamp, int fbgain, int outgain, ref FltCoefs denom, int cnumer, ref FltCoefs numer, ref FltCoefs pfsamps, int @in) {
		int @out, sD;
		int sDnew;

		// crossfade from tdelay to tdelaynew tap. xfade is 0..PMAX

		sD = GetDly(delaysize, psamps, ppsamp, tdelay);
		sDnew = GetDly(delaysize, psamps, ppsamp, tdelaynew);
		sD = sD + (((sDnew - sD) * xf) >> PBITS);

		@out = IIRFilter_Update_Order1(ref denom, cnumer, ref numer, ref pfsamps, sD);

		psamps[ppsamp] = @in;

		DlyUpdate(delaysize, psamps, ref ppsamp);

		return (@out * outgain) >> PBITS;
	}


	// classic allpass reverb
	// delaysize:	delay line size in samples
	// tdelay:		tap from this location - <= D
	// psamps:		delay line buffer pointer of dimension delaysize+1
	// ppsamp:		circular pointer, must be init to &psamps[0] before first call
	// fbgain:		feedback value, 0-PMAX (normalized to 0.0-1.0)
	// outgain:		gain

	//                    psamps0(n)  -fbgain outgain
	//  in(n)--->(+)--------.-----(*)-->(+)--(*)-> out(n)
	//           ^         |            ^
	//           |     [Delay d]        |
	//           |         |            |
	//           | fbgain  |psampsd(n)  |
	//			 ----(*)---.-------------
	//
	//	for each input sample 'in':
	//		psamps0 = in + fbgain * psampsd
	//		y = -fbgain * psamps0 + psampsd
	//		delay (d, psamps) - psamps is the delay buffer array
	//
	// or, using circular delay, for each input sample 'in':
	//
	//		Sd = GetDly (delaysize,psamps,ppsamp,delaysize)
	//		S0 = in + fbgain*Sd
	//		y = -fbgain*S0 + Sd
	//		*ppsamp = S0
	//		DlyUpdate(delaysize, psamps, &ppsamp)

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int DelayAllpass(int delaysize, int tdelay, int[] psamps, ref int ppsamp, int fbgain, int outgain, int @in) {
		int @out, s0, sD;

		sD = GetDly(delaysize, psamps, ppsamp, tdelay);
		s0 = @in + ((fbgain * sD) >> PBITS);

		@out = ((-fbgain * s0) >> PBITS) + sD;
		psamps[ppsamp] = s0;
		DlyUpdate(delaysize, psamps, ref ppsamp);

		return (@out * outgain) >> PBITS;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int DelayAllpass_xfade(int delaysize, int tdelay, int tdelaynew, int xf, int[] psamps, ref int ppsamp, int fbgain, int outgain, int @in) {
		int @out, s0, sD;
		int sDnew;

		// crossfade from t to tnew tap. xfade is 0..PMAX

		sD = GetDly(delaysize, psamps, ppsamp, tdelay);
		sDnew = GetDly(delaysize, psamps, ppsamp, tdelaynew);
		sD = sD + (((sDnew - sD) * xf) >> PBITS);

		s0 = @in + ((fbgain * sD) >> PBITS);

		@out = ((-fbgain * s0) >> PBITS) + sD;
		psamps[ppsamp] = s0;
		DlyUpdate(delaysize, psamps, ref ppsamp);

		return (@out * outgain) >> PBITS;
	}

	///////////////////////////////////////////////////////////////////////////////////
	// fixed point math for real-time wave table traversing, pitch shifting, resampling
	///////////////////////////////////////////////////////////////////////////////////

	const int FIX20_BITS = 20;                                  // 20 bits of fractional part
	const int FIX20_SCALE = 1 << FIX20_BITS;

	const int FIX20_INTMAX = (1 << (32 - FIX20_BITS)) - 1;      // maximum step integer

	[MethodImpl(MethodImplOptions.AggressiveInlining)] static int FLOAT_TO_FIX20(float a) => (int)(a * (float)FIX20_SCALE);       // convert float to fixed point
	[MethodImpl(MethodImplOptions.AggressiveInlining)] static int INT_TO_FIX20(int a) => a << FIX20_BITS;                        // convert int to fixed point
	[MethodImpl(MethodImplOptions.AggressiveInlining)] static float FIX20_TO_FLOAT(int a) => (float)a / (float)FIX20_SCALE;      // convert fix20 to float
	[MethodImpl(MethodImplOptions.AggressiveInlining)] static int FIX20_INTPART(int a) => a >> FIX20_BITS;                       // get integer part of fixed point
	[MethodImpl(MethodImplOptions.AggressiveInlining)] static int FIX20_FRACPART(int a) => a - ((a >> FIX20_BITS) << FIX20_BITS);   // get fractional part of fixed point

	[MethodImpl(MethodImplOptions.AggressiveInlining)] static int FIX20_FRACTION(int a, int b) => FIX(a) / b;                     // convert int a to fixed point, divide by b

	/////////////////////////////////
	// DSP processor parameter block
	/////////////////////////////////

	// NOTE: these prototypes must match the XXX_Params ( prc_t *pprc ) and XXX_GetNext ( XXX_t *p, int x ) functions

	public const int OP_LEFT = 0;           // batch process left channel in place
	public const int OP_RIGHT = 1;          // batch process right channel in place
	public const int OP_LEFT_DUPLICATE = 2; // batch process left channel in place, duplicate to right channel

	public const int PRC_NULL = 0;      // pass through - must be 0
	public const int PRC_DLY = 1;       // simple feedback reverb
	public const int PRC_RVA = 2;       // parallel reverbs
	public const int PRC_FLT = 3;       // lowpass or highpass filter
	public const int PRC_CRS = 4;       // chorus
	public const int PRC_PTC = 5;       // pitch shifter
	public const int PRC_ENV = 6;       // adsr envelope
	public const int PRC_LFO = 7;       // lfo
	public const int PRC_EFO = 8;       // envelope follower
	public const int PRC_MDY = 9;       // mod delay
	public const int PRC_DFR = 10;      // diffusor - n series allpass delays
	public const int PRC_AMP = 11;      // amplifier with distortion

	public const int QUA_LO = 0;        // quality of filter or reverb.  Must be 0,1,2,3.
	public const int QUA_MED = 1;
	public const int QUA_HI = 2;
	public const int QUA_VHI = 3;
	public const int QUA_MAX = QUA_VHI;

	public const int CPRCPARAMS = 16;   // up to 16 floating point params for each processor type

	///////////
	// Filters
	///////////

	public const int CFLTS = 64;        // max number of filters simultaneously active
	public const int FLT_M = 12;        // max order of any filter

	public const int FLT_LP = 0;        // lowpass filter
	public const int FLT_HP = 1;        // highpass filter
	public const int FLT_BP = 2;        // bandpass filter
	public const int FTR_MAX = FLT_BP;

	// flt flts

	static readonly Flt[] flts = CreatePool<Flt>(CFLTS);

	static void FLT_Init(Flt? pf) { if (pf != null) pf.Clear(); }
	static void FLT_InitAll() { for (int i = 0; i < CFLTS; i++) FLT_Init(flts[i]); }

	internal static void FLT_Free(Flt? pf) {
		if (pf != null) {
			if (pf.pf1 != null)
				pf.pf1.Clear();

			if (pf.pf2 != null)
				pf.pf2.Clear();

			if (pf.pf3 != null)
				pf.pf3.Clear();

			pf.Clear();
		}
	}

	static void FLT_FreeAll() { for (int i = 0; i < CFLTS; i++) FLT_Free(flts[i]); }


	// find a free filter from the filter pool
	// initialize filter numerator, denominator b[0..M], a[0..L]
	// gain scales filter numerator
	// N is # of series sections - 1

	static Flt? FLT_Alloc(int N, int M, int L, ReadOnlySpan<int> a, ReadOnlySpan<int> b, float gain) {
		int i, j;
		Flt? pf = null;

		for (i = 0; i < CFLTS; i++) {
			if (!flts[i].fused) {
				pf = flts[i];

				// transfer filter params into filter struct
				pf.M = M;
				pf.L = L;
				pf.N = N;

				for (j = 0; j <= M; j++)
					pf.a[j] = a[j];

				for (j = 0; j <= L; j++)
					pf.b[j] = (int)((float)b[j] * gain);

				pf.pf1 = null;
				pf.pf2 = null;
				pf.pf3 = null;

				pf.fused = true;
				break;
			}
		}

		Assert(pf != null); // make sure we're not trying to alloc more than CFLTS flts

		return pf;
	}

	// convert filter params cutoff and type into
	// iir transfer function params M, L, a[], b[]

	// iir filter, 1st order, transfer function is H(z) = b0 + b1 Z^-1  /  a0 + a1 Z^-1
	// or H(z) = b0 - b1 Z^-1 / a0 + a1 Z^-1 for lowpass

	// design cutoff filter at 3db (.5 gain) p579

	static void FLT_Design_3db_IIR(float cutoff, float ftype, out int pM, out int pL, Span<int> a, Span<int> b) {
		// ftype: FLT_LP, FLT_HP, FLT_BP

		double Wc = 2.0 * Math.PI * cutoff / SOUND_DMA_SPEED;           // radians per sample
		double Oc;
		double fa;
		double fb;

		// calculations:
		// Wc = 2pi * fc/44100								convert to radians
		// Oc = tan (Wc/2) * Gc / sqt ( 1 - Gc^2)			get analog version, low pass
		// Oc = tan (Wc/2) * (sqt (1 - Gc^2)) / Gc			analog version, high pass
		// Gc = 10 ^ (-Ac/20)								gain at cutoff.  Ac = 3db, so Gc^2 = 0.5
		// a = ( 1 - Oc ) / ( 1 + Oc )
		// b = ( 1 - a ) / 2

		Oc = Math.Tan(Wc / 2.0);

		fa = (1.0 - Oc) / (1.0 + Oc);

		fb = (1.0 - fa) / 2.0;

		if (ftype == FLT_HP)
			fb = (1.0 + fa) / 2.0;

		a[0] = 0;                       // a0 always ignored
		a[1] = (int)(-fa * PMAX);       // quantize params down to 0-PMAX >> PBITS
		b[0] = (int)(fb * PMAX);
		b[1] = b[0];

		if (ftype == FLT_HP)
			b[1] = -b[1];

		pM = pL = 1;

		return;
	}

	// filter parameter order

	const int flt_iftype = 0;
	const int flt_icutoff = 1;
	const int flt_iqwidth = 2;
	const int flt_iquality = 3;
	const int flt_igain = 4;

	const int flt_cparam = 5;               // # of params

	// filter parameter ranges

	static readonly PrmRng[] flt_rng = [

		new(flt_cparam, 0, 0),          // first entry is # of parameters

		new(flt_iftype, 0, FTR_MAX),    // filter type FLT_LP, FLT_HP, FLT_BP
		new(flt_icutoff, 10, 22050),        // cutoff frequency in hz at -3db gain
		new(flt_iqwidth, 0, 11025),     // width of BP (cut in starts at cutoff)
		new(flt_iquality, 0, QUA_MAX),  // QUA_LO, _MED, _HI, _VHI = # of series sections
		new(flt_igain, 0.0f, 10.0f),        // output gain 0-10.0
	];


	// convert prc float params to iir filter params, alloc filter and return ptr to it
	// filter quality set by prc quality - 0,1,2

	static Flt? FLT_Params(ref Prc pprc) {
		float qual = pprc.prm[flt_iquality];
		float cutoff = pprc.prm[flt_icutoff];
		float ftype = pprc.prm[flt_iftype];
		float qwidth = pprc.prm[flt_iqwidth];
		float gain = pprc.prm[flt_igain];

		int L = 0;                  // numerator order
		int M = 0;                  // denominator order
		Span<int> b = stackalloc int[FLT_M + 1];             // numerator params	 0..PMAX
		Span<int> b_scaled = stackalloc int[FLT_M + 1];      // gain scaled numerator
		Span<int> a = stackalloc int[FLT_M + 1];             // denominator params 0..PMAX

		int L_bp = 0;               // bandpass numerator order
		int M_bp = 0;               // bandpass denominator order
		Span<int> b_bp = stackalloc int[FLT_M + 1];          // bandpass numerator params	 0..PMAX
		Span<int> b_bp_scaled = stackalloc int[FLT_M + 1];   // gain scaled numerator
		Span<int> a_bp = stackalloc int[FLT_M + 1];          // bandpass denominator params 0..PMAX

		int N;                      // # of series sections
		bool bpass = false;

		// if qwidth > 0 then alloc bandpass filter (pf is lowpass)

		if (qwidth > 0.0)
			bpass = true;

		if (bpass)
			ftype = FLT_LP;

		// low pass and highpass filter design

		//	1st order IIR filter, 3db cutoff at fc

		if (bpass) {
			// highpass section

			FLT_Design_3db_IIR(cutoff, FLT_HP, out M_bp, out L_bp, a_bp, b_bp);
			M_bp = Math.Clamp(M_bp, 1, FLT_M);
			L_bp = Math.Clamp(L_bp, 1, FLT_M);
			cutoff += qwidth;
		}

		// lowpass section

		FLT_Design_3db_IIR(cutoff, (int)ftype, out M, out L, a, b);

		M = Math.Clamp(M, 1, FLT_M);
		L = Math.Clamp(L, 1, FLT_M);

		// quality = # of series sections - 1

		N = Math.Clamp((int)qual, 0, 3);

		// make sure we alloc at least 2 filters

		if (bpass)
			N = Math.Max(N, 1);

		Flt? pf0 = null;
		Flt? pf1 = null;
		Flt? pf2 = null;
		Flt? pf3 = null;

		// scale b numerators with gain - only scale for first filter if series filters

		for (int i = 0; i < FLT_M; i++) {
			b_bp_scaled[i] = (int)((float)b_bp[i] * gain);
			b_scaled[i] = (int)((float)b[i] * gain);
		}

		if (bpass) {
			// 1st filter is lowpass

			pf0 = FLT_Alloc(N, M_bp, L_bp, a_bp, b_bp_scaled, 1.0f);
		}
		else
			pf0 = FLT_Alloc(N, M, L, a, b_scaled, 1.0f);

		// allocate series filters

		if (pf0 != null) {
			switch (N) {
				case 3:
					// alloc last filter as lowpass also if FLT_BP
					if (bpass)
						pf3 = FLT_Alloc(0, M_bp, L_bp, a_bp, b_bp, 1.0f);
					else
						pf3 = FLT_Alloc(0, M, L, a, b, 1.0f);
					goto case 2;
				case 2:
					pf2 = FLT_Alloc(0, M, L, a, b, 1.0f);
					goto case 1;
				case 1:
					pf1 = FLT_Alloc(0, M, L, a, b, 1.0f);
					goto case 0;
				case 0:
					break;
			}

			pf0.pf1 = pf1;
			pf0.pf2 = pf2;
			pf0.pf3 = pf3;
		}

		return pf0;
	}

	static DspProcessor? FLT_VParams(ref Prc p) {
		PRC_CheckParams(ref p, flt_rng);
		return FLT_Params(ref p);
	}

	internal static void FLT_Mod(Flt p, float v) { return; }

	// get next filter value for filter pf and input x

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int FLT_GetNext(Flt pf, int x) {
		Flt pf1;
		Flt pf2;
		Flt pf3;
		int y;

		switch (pf.N) {
			default:
			case 0:
				return IIRFilter_Update_Order1(ref pf.a, pf.L, ref pf.b, ref pf.w, x);
			case 1:
				pf1 = pf.pf1!;

				y = IIRFilter_Update_Order1(ref pf.a, pf.L, ref pf.b, ref pf.w, x);
				return IIRFilter_Update_Order1(ref pf1.a, pf1.L, ref pf1.b, ref pf1.w, y);
			case 2:
				pf1 = pf.pf1!;
				pf2 = pf.pf2!;

				y = IIRFilter_Update_Order1(ref pf.a, pf.L, ref pf.b, ref pf.w, x);
				y = IIRFilter_Update_Order1(ref pf1.a, pf1.L, ref pf1.b, ref pf1.w, y);
				return IIRFilter_Update_Order1(ref pf2.a, pf2.L, ref pf2.b, ref pf2.w, y);
			case 3:
				pf1 = pf.pf1!;
				pf2 = pf.pf2!;
				pf3 = pf.pf3!;

				y = IIRFilter_Update_Order1(ref pf.a, pf.L, ref pf.b, ref pf.w, x);
				y = IIRFilter_Update_Order1(ref pf1.a, pf1.L, ref pf1.b, ref pf1.w, y);
				y = IIRFilter_Update_Order1(ref pf2.a, pf2.L, ref pf2.b, ref pf2.w, y);
				return IIRFilter_Update_Order1(ref pf3.a, pf3.L, ref pf3.b, ref pf3.w, y);
		}
	}


	// batch version for performance

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static void FLT_GetNextN(Flt pflt, Span<PortableSamplePair> pbuffer, int SampleCount, int op) {
		Span<PortableSamplePair> pb = pbuffer[..SampleCount];

		switch (op) {
			default:
			case OP_LEFT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = FLT_GetNext(pflt, pb[i].Left);
				return;
			case OP_RIGHT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Right = FLT_GetNext(pflt, pb[i].Right);
				return;
			case OP_LEFT_DUPLICATE:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = pb[i].Right = FLT_GetNext(pflt, pb[i].Left);
				return;
		}
	}

	///////////////////////////////////////////////////////////////////////////
	// Positional updaters for pitch shift etc
	///////////////////////////////////////////////////////////////////////////

	// circular wrap of pointer p, relative to array w
	// D max buffer index w[0...D] (count of samples in buffer is D+1)
	// i circular index

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void POS_Wrap(int D, ref int i) {
		if (i > D)
			i -= D + 1;        // when *pi = D + 1, it wraps around to *pi = 0

		if (i < 0)
			i += D + 1;        // when *pi = - 1, it wraps around to *pi = D
	}

	// set initial update value - fstep can have no more than 8 bits of integer and 20 bits of fract
	// D is array max dimension w[0...D] (ie: size D+1)
	// w is ptr to array
	// p is ptr to pos_t to initialize

	static void POS_Init(ref Pos p, int D, float fstep) {
		float step = fstep;

		// make sure int part of step is capped at fix20_intmax

		if ((int)step > FIX20_INTMAX)
			step = (step - (int)step) + FIX20_INTMAX;

		p.step = FLOAT_TO_FIX20(step); // convert fstep to fixed point
		p.cstep = 0;
		p.pos = 0;                         // current update value

		p.D = D;                           // always init to end value, in case we're stepping backwards
	}

	// change step value - this is an instantaneous change, not smoothed.

	static void POS_ChangeVal(ref Pos p, float fstepnew) {
		p.step = FLOAT_TO_FIX20(fstepnew); // convert fstep to fixed point
	}

	// return current integer position, then update internal position value

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int POS_GetNext(ref Pos p) {

		//float f = FIX20_TO_FLOAT(p->cstep);
		//int i1 = FIX20_INTPART(p->cstep);
		//float f1 = FIX20_TO_FLOAT(FIX20_FRACPART(p->cstep));
		//float f2 = FIX20_TO_FLOAT(p->step);

		p.cstep += p.step;                        // update accumulated fraction step value (fixed point)
		p.pos += FIX20_INTPART(p.cstep);          // update pos with integer part of accumulated step
		p.cstep = FIX20_FRACPART(p.cstep);        // throw away the integer part of accumulated step

		// wrap pos around either end of buffer if needed

		POS_Wrap(p.D, ref p.pos);

		// make sure returned position is within array bounds

		Assert(p.pos <= p.D);

		return p.pos;
	}

	// set initial update value - fstep can have no more than 8 bits of integer and 20 bits of fract
	// one shot position - play only once, don't wrap, when hit end of buffer, return last position

	static void POS_ONE_Init(ref PosOne p1, int D, float fstep) {
		POS_Init(ref p1.p, D, fstep);

		p1.fhitend = false;
	}

	// return current integer position, then update internal position value

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int POS_ONE_GetNext(ref PosOne p1) {
		int pos;

		pos = p1.p.pos;                            // return current position

		if (p1.fhitend)
			return pos;

		ref Pos p0 = ref p1.p;
		p0.cstep += p0.step;                      // update accumulated fraction step value (fixed point)
		p0.pos += FIX20_INTPART(p0.cstep);        // update pos with integer part of accumulated step
													//p0->cstep = SIGN(p0->cstep) * FIX20_FRACPART( p0->cstep );
		p0.cstep = FIX20_FRACPART(p0.cstep);      // throw away the integer part of accumulated step

		// if we wrapped, stop updating, always return last position
		// if step value is 0, return hit end

		if (p0.step == 0 || p0.pos < 0 || p0.pos >= p0.D)
			p1.fhitend = true;
		else
			pos = p0.pos;

		// make sure returned value is within array bounds

		Assert(pos <= p0.D);

		return pos;
	}


	/////////////////////
	// Reverbs and delays
	/////////////////////

	public const int CDLYS = 128;               // max delay lines active. Also used for lfos.

	public const int DLY_PLAIN = 0;             // single feedback loop
	public const int DLY_ALLPASS = 1;           // feedback and feedforward loop - flat frequency response (diffusor)
	public const int DLY_LOWPASS = 2;           // lowpass filter in feedback loop
	public const int DLY_LINEAR = 3;            // linear delay, no feedback, unity gain
	public const int DLY_FLINEAR = 4;           // linear delay with lowpass filter and output gain
	public const int DLY_LOWPASS_4TAP = 5;      // lowpass filter in feedback loop, 4 delay taps
	public const int DLY_PLAIN_4TAP = 6;        // single feedback loop, 4 delay taps

	public const int DLY_MAX = DLY_PLAIN_4TAP;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] static bool DLY_HAS_MULTITAP(int a) => a == DLY_LOWPASS_4TAP || a == DLY_PLAIN_4TAP;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] static bool DLY_HAS_FILTER(int a) => a == DLY_FLINEAR || a == DLY_LOWPASS || a == DLY_LOWPASS_4TAP;

	const float DLY_TAP_FEEDBACK_GAIN = 0.25f;      // drop multitap feedback to compensate for sum of taps in dly_*multitap()

	const float DLY_NORMALIZING_REDUCTION_MAX = 0.25f;  // don't reduce gain (due to feedback) below N% of original gain

	static readonly Dly[] dlys = CreatePool<Dly>(CDLYS);  // delay lines

	static void DLY_Init(Dly? pdly) { if (pdly != null) pdly.Clear(); }
	static void DLY_InitAll() { for (int i = 0; i < CDLYS; i++) DLY_Init(dlys[i]); }
	internal static void DLY_Free(Dly? pdly) {
		// free memory buffer

		if (pdly != null) {
			FLT_Free(pdly.pflt);

			pdly.w = null;

			// free dly slot

			pdly.Clear();
		}
	}


	static void DLY_FreeAll() { for (int i = 0; i < CDLYS; i++) DLY_Free(dlys[i]); }

	// return adjusted feedback value for given dly
	// such that decay time is same as that for dmin and fbmin

	// dmin - minimum delay
	// fbmin - minimum feedback
	// dly - delay to match decay to dmin, fbmin

	static float DLY_NormalizeFeedback(int dmin, float fbmin, int dly) {
		// minimum decay time T to -60db for a simple reverb is:

		//		Tmin = (ln 10^-3 / Ln fbmin) * (Dmin / fs)

		// where fs = sample frequency

		// similarly,

		//		Tdly = (ln 10^-3 / Ln fb) * (D / fs)

		// setting Tdly = Tmin and solving for fb gives:

		//		D / Dmin = ln fb / ln fbmin

		// since y^x = z gives x = ln z / ln y

		//		fb = fbmin ^ (D/Dmin)

		float fb = MathF.Pow(fbmin, (float)dly / (float)dmin);

		return fb;
	}

	static float DLY_NormalizeFeedback(float dmin, float fbmin, float dly) => DLY_NormalizeFeedback((int)dmin, fbmin, (int)dly);

	// set up 'b' gain parameter of feedback delay to
	// compensate for gain caused by feedback 'fb'.

	static void DLY_SetNormalizingGain(Dly pdly, int feedback) {
		// compute normalized gain, set as output gain

		// calculate gain of delay line with feedback, and use it to
		// reduce output.  ie: force delay line with feedback to unity gain

		// for constant input x with feedback fb:

		// out = x + x*fb + x * fb^2 + x * fb^3...
		// gain = out/x
		// so gain = 1 + fb + fb^2 + fb^3...
		// which, by the miracle of geometric series, equates to 1/1-fb
		// thus, gain = 1/(1-fb)

		float fgain = 0;
		float gain;
		int b;
		float fb = (float)feedback;

		fb = fb / (float)PMAX;
		fb = Math.Min(fb, 0.999f);

		// if b is 0, set b to PMAX (1)

		b = pdly.b != 0 ? pdly.b : PMAX;

		fgain = 1.0F / (1.0F - fb);

		// compensating gain -  multiply rva output by gain then >> PBITS

		gain = (int)((1.0F / fgain) * PMAX);

		gain = gain * 4;    // compensate for fact that gain calculation is for +/- 32767 amplitude wavs
							// ie: ok to allow a bit more gain because most wavs are not at theoretical peak amplitude at all times

		// limit gain reduction to N% PMAX

		gain = Math.Clamp(gain, (float)(PMAX * DLY_NORMALIZING_REDUCTION_MAX), (float)PMAX);

		gain = ((float)b / (float)PMAX) * gain; // scale final gain by pdly->b.

		pdly.b = (int)gain;
	}

	// allocate a new delay line
	// D number of samples to delay
	// a feedback value (0-PMAX normalized to 0.0-1.0)
	// b gain value (0-PMAX normalized to 0.0-1.0) - this is folded into the filter fb params
	// if DLY_LOWPASS or DLY_FLINEAR:
	//		L - numerator order of filter
	//		M - denominator order of filter
	//		fb - numerator params, M+1
	//		fa - denominator params, L+1

	static Dly? DLY_AllocLP(int D, int a, int b, int type, int M, int L, ReadOnlySpan<int> fa, ReadOnlySpan<int> fb) {
		int[] w;
		int i;
		Dly? pdly = null;
		int feedback;

		// find open slot

		for (i = 0; i < CDLYS; i++) {
			if (!dlys[i].fused) {
				pdly = dlys[i];
				DLY_Init(pdly);
				break;
			}
		}

		if (i == CDLYS) {
			DevMsg("DSP: Warning, failed to allocate delay line.\n");
			return null;                    // all delay lines in use
		}

		// save original feedback value

		feedback = a;

		// adjust feedback a, gain b if delay is multitap unit

		if (DLY_HAS_MULTITAP(type)) {
			// split output gain over 4 taps

			b = (int)((float)b * DLY_TAP_FEEDBACK_GAIN);
		}

		if (DLY_HAS_FILTER(type)) {
			// alloc lowpass iir_filter
			// delay feedback gain is built into filter gain

			float gain = (float)a / (float)PMAX;

			pdly.pflt = FLT_Alloc(0, M, L, fa, fb, gain);
			if (pdly.pflt == null) {
				DevMsg("DSP: Warning, failed to allocate filter for delay line.\n");
				return null;
			}
		}

		// alloc delay memory
		w = new int[D + 1];

		// init values

		pdly.type = type;
		pdly.D = D;
		pdly.t = D;        // set delay tap to full delay
		pdly.tnew = D;
		pdly.xf = 0;
		pdly.D0 = D;
		pdly.p = 0;        // init circular pointer to head of buffer
		pdly.w = w;
		pdly.a = Math.Min(a, PMAX - 1);        // do not allow 100% feedback
		pdly.b = b;
		pdly.fused = true;

		if (type == DLY_LINEAR || type == DLY_FLINEAR) {
			// linear delay has no feedback and unity gain

			pdly.a = 0;
			pdly.b = PMAX;
		}
		else {
			// adjust b to compensate for feedback gain of steady state max input

			DLY_SetNormalizingGain(pdly, feedback);
		}

		if (DLY_HAS_MULTITAP(type)) {
			// initially set up all taps to same value - caller uses DLY_ChangeTaps to change values

			DLY_ChangeTaps(pdly, D, D, D, D);
		}

		return pdly;
	}

	// allocate lowpass or allpass delay

	static Dly? DLY_Alloc(int D, int a, int b, int type) {
		return DLY_AllocLP(D, a, b, type, 0, 0, default, default);
	}


	// Allocate new delay, convert from float params in prc preset to internal parameters
	// Uses filter params in prc if delay is type lowpass

	// delay parameter order

	const int dly_idtype = 0;       // NOTE: first 8 params must match those in mdy_e
	const int dly_idelay = 1;
	const int dly_ifeedback = 2;
	const int dly_igain = 3;

	const int dly_iftype = 4;
	const int dly_icutoff = 5;
	const int dly_iqwidth = 6;
	const int dly_iquality = 7;

	const int dly_itap1 = 8;
	const int dly_itap2 = 9;
	const int dly_itap3 = 10;

	const int dly_cparam = 11;


	// delay parameter ranges

	static readonly PrmRng[] dly_rng = [

		new(dly_cparam, 0, 0),          // first entry is # of parameters

		// delay params

		new(dly_idtype, 0, DLY_MAX),        // delay type DLY_PLAIN, DLY_LOWPASS, DLY_ALLPASS etc
		new(dly_idelay, -1.0f, 1000.0f),        // delay in milliseconds (-1 forces auto dsp to set delay value from room size)
		new(dly_ifeedback, 0.0f, 0.99f),            // feedback 0-1.0
		new(dly_igain, 0.0f, 10.0f),            // final gain of output stage, 0-10.0

		// filter params if dly type DLY_LOWPASS or DLY_FLINEAR

		new(dly_iftype, 0, FTR_MAX),
		new(dly_icutoff, 10.0f, 22050.0f),
		new(dly_iqwidth, 100.0f, 11025.0f),
		new(dly_iquality, 0, QUA_MAX),
		// note: -1 flag tells auto dsp to get value directly from room size
		new(dly_itap1, -1.0f, 1000.0f),         // delay in milliseconds NOTE: delay > tap3 > tap2 > tap1
		new(dly_itap2, -1.0f, 1000.0f),         // delay in milliseconds
		new(dly_itap3, -1.0f, 1000.0f),         // delay in milliseconds
	];

	static Dly? DLY_Params(ref Prc pprc) {
		Dly? pdly = null;
		int D, a, b;

		float delay = MathF.Abs(pprc.prm[dly_idelay]);
		float feedback = pprc.prm[dly_ifeedback];
		float gain = pprc.prm[dly_igain];
		int type = (int)pprc.prm[dly_idtype];

		float ftype = pprc.prm[dly_iftype];
		float cutoff = pprc.prm[dly_icutoff];
		float qwidth = pprc.prm[dly_iqwidth];
		float qual = pprc.prm[dly_iquality];

		float t1 = MathF.Abs(pprc.prm[dly_itap1]);
		float t2 = MathF.Abs(pprc.prm[dly_itap2]);
		float t3 = MathF.Abs(pprc.prm[dly_itap3]);

		D = MSEC_TO_SAMPS(delay);                   // delay samples
		a = (int)(feedback * PMAX);                     // feedback
		b = (int)(gain * PMAX);                         // gain

		switch (type) {
			case DLY_PLAIN:
			case DLY_PLAIN_4TAP:
			case DLY_ALLPASS:
			case DLY_LINEAR:
				pdly = DLY_Alloc(D, a, b, type);
				break;

			case DLY_FLINEAR:
			case DLY_LOWPASS:
			case DLY_LOWPASS_4TAP: {
					// set up dummy lowpass filter to convert params

					Prc prcf = default;

					prcf.prm[flt_iquality] = qual;  // 0,1,2 -  (0 or 1 low quality implies faster execution time)
					prcf.prm[flt_icutoff] = cutoff;
					prcf.prm[flt_iftype] = ftype;
					prcf.prm[flt_iqwidth] = qwidth;
					prcf.prm[flt_igain] = 1.0f;

					Flt? pflt = FLT_Params(ref prcf);

					if (pflt == null) {
						DevMsg("DSP: Warning, failed to allocate filter.\n");
						return null;
					}

					pdly = DLY_AllocLP(D, a, b, type, pflt.M, pflt.L, pflt.a, pflt.b);

					FLT_Free(pflt);
					break;
				}
		}

		// set up multi-tap delays

		if (pdly != null && DLY_HAS_MULTITAP(type))
			DLY_ChangeTaps(pdly, D, MSEC_TO_SAMPS(t1), MSEC_TO_SAMPS(t2), MSEC_TO_SAMPS(t3));

		return pdly;
	}

	static DspProcessor? DLY_VParams(ref Prc p) {
		PRC_CheckParams(ref p, dly_rng);
		return DLY_Params(ref p);
	}

	// get next value from delay line, move x into delay line

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int DLY_GetNext(Dly pdly, int x) {
		switch (pdly.type) {
			default:
			case DLY_PLAIN:
				return ReverbSimple(pdly.D, pdly.t, pdly.w!, ref pdly.p, pdly.a, pdly.b, x);
			case DLY_ALLPASS:
				return DelayAllpass(pdly.D, pdly.t, pdly.w!, ref pdly.p, pdly.a, pdly.b, x);
			case DLY_LOWPASS:
				return DelayLowpass(pdly.D, pdly.t, pdly.w!, ref pdly.p, pdly.a, pdly.b, ref pdly.pflt!.a, pdly.pflt.L, ref pdly.pflt.b, ref pdly.pflt.w, x);
			case DLY_LINEAR:
				return DelayLinear(pdly.D, pdly.t, pdly.w!, ref pdly.p, x);
			case DLY_FLINEAR:
				return DelayLinear_lowpass(pdly.D, pdly.t, pdly.w!, ref pdly.p, pdly.a, pdly.b, ref pdly.pflt!.a, pdly.pflt.L, ref pdly.pflt.b, ref pdly.pflt.w, x);
			case DLY_PLAIN_4TAP:
				return ReverbSimple_multitap(pdly.D, pdly.t, pdly.t1, pdly.t2, pdly.t3, pdly.w!, ref pdly.p, pdly.a, pdly.b, x);
			case DLY_LOWPASS_4TAP:
				return DelayLowpass_multitap(pdly.D, pdly.t, pdly.t1, pdly.t2, pdly.t3, pdly.w!, ref pdly.p, pdly.a, pdly.b, ref pdly.pflt!.a, pdly.pflt.L, ref pdly.pflt.b, ref pdly.pflt.w, x);
		}
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int DLY_GetNextXfade(Dly pdly, int x) {

		switch (pdly.type) {
			default:
			case DLY_PLAIN:
				return ReverbSimple_xfade(pdly.D, pdly.t, pdly.tnew, pdly.xf, pdly.w!, ref pdly.p, pdly.a, pdly.b, x);
			case DLY_ALLPASS:
				return DelayAllpass_xfade(pdly.D, pdly.t, pdly.tnew, pdly.xf, pdly.w!, ref pdly.p, pdly.a, pdly.b, x);
			case DLY_LOWPASS:
				return DelayLowpass_xfade(pdly.D, pdly.t, pdly.tnew, pdly.xf, pdly.w!, ref pdly.p, pdly.a, pdly.b, ref pdly.pflt!.a, pdly.pflt.L, ref pdly.pflt.b, ref pdly.pflt.w, x);
			case DLY_LINEAR:
				return DelayLinear_xfade(pdly.D, pdly.t, pdly.tnew, pdly.xf, pdly.w!, ref pdly.p, x);
			case DLY_FLINEAR:
				return DelayLinear_lowpass_xfade(pdly.D, pdly.t, pdly.tnew, pdly.xf, pdly.w!, ref pdly.p, pdly.a, pdly.b, ref pdly.pflt!.a, pdly.pflt.L, ref pdly.pflt.b, ref pdly.pflt.w, x);
			case DLY_PLAIN_4TAP:
				return ReverbSimple_multitap_xfade(pdly.D, pdly.t, pdly.tnew, pdly.xf, pdly.t1, pdly.t2, pdly.t3, pdly.w!, ref pdly.p, pdly.a, pdly.b, x);
			case DLY_LOWPASS_4TAP:
				return DelayLowpass_multitap_xfade(pdly.D, pdly.t, pdly.tnew, pdly.xf, pdly.t1, pdly.t2, pdly.t3, pdly.w!, ref pdly.p, pdly.a, pdly.b, ref pdly.pflt!.a, pdly.pflt.L, ref pdly.pflt.b, ref pdly.pflt.w, x);
		}
	}

	// batch version for performance
	// UNDONE: a) unwind this more - pb increments by 2 to avoid pb->left or pb->right deref.
	// UNDONE: b) all filter and delay params are dereferenced outside of DLY_GetNext and passed as register values
	// UNDONE: c) pull case statement in dly_getnext out, so loop directly calls the inline dly_*() routine.

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static void DLY_GetNextN(Dly pdly, Span<PortableSamplePair> pbuffer, int SampleCount, int op) {
		Span<PortableSamplePair> pb = pbuffer[..SampleCount];

		switch (op) {
			default:
			case OP_LEFT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = DLY_GetNext(pdly, pb[i].Left);
				return;
			case OP_RIGHT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Right = DLY_GetNext(pdly, pb[i].Right);
				return;
			case OP_LEFT_DUPLICATE:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = pb[i].Right = DLY_GetNext(pdly, pb[i].Left);
				return;
		}
	}

	// get tap on t'th sample in delay - don't update buffer pointers, this is done via DLY_GetNext
	// Only valid for DLY_LINEAR.

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int DLY_GetTap(Dly pdly, int t) {
		return GetDly(pdly.D, pdly.w!, pdly.p, t);
	}

	// make instantaneous change to tap values t0..t3
	// all values of t must be less than original delay D
	// only processed for DLY_LOWPASS_4TAP & DLY_PLAIN_4TAP
	// NOTE: pdly->a feedback must have been set before this call!
	static void DLY_ChangeTaps(Dly? pdly, int t0, int t1, int t2, int t3) {
		if (pdly == null)
			return;

		// sort taps to make sure t3 > t2 > t1 > t0 !

		for (int i = 0; i < 4; i++) {
			if (t0 > t1) (t0, t1) = (t1, t0);
			if (t1 > t2) (t1, t2) = (t2, t1);
			if (t2 > t3) (t2, t3) = (t3, t2);
		}

		pdly.t = Math.Min(t0, pdly.D0);
		pdly.t1 = Math.Min(t1, pdly.D0);
		pdly.t2 = Math.Min(t2, pdly.D0);
		pdly.t3 = Math.Min(t3, pdly.D0);

	}

	// make instantaneous change for first delay tap 't' to new delay value.
	// t tap value must be <= original D (ie: we don't do any reallocation here)

	static void DLY_ChangeVal(Dly pdly, int t) {
		// never set delay > original delay

		pdly.t = Math.Min(t, pdly.D0);
	}

	// ignored - use MDY_ for modulatable delay

	internal static void DLY_Mod(Dly p, float v) { return; }


	/////////////////////////////////////////////////////////////////////////////
	// Ramp - used for varying smoothly between int parameters ie: modulation delays
	/////////////////////////////////////////////////////////////////////////////


	// ramp smoothly between initial value and target value in approx 'ramptime' seconds.
	// (initial value may be greater or less than target value)
	// never changes output by more than +1 or -1 (which can cause the ramp to take longer to complete than ramptime - see bEndAtTime)
	// called once per sample while ramping
	// ramptime - duration of ramp in seconds
	// initval - initial ramp value
	// targetval - target ramp value
	// if bEndAtTime is true, then RMP_HitEnd returns true when ramp time is reached, EVEN IF TARGETVAL IS NOT REACHED
	// if bEndAtTime is false, then RMP_HitEnd returns true when targetval is reached, EVEN IF DELTA IN RAMP VALUES IS > +/- 1

	static void RMP_Init(ref Rmp prmp, float ramptime, int initval, int targetval, bool bEndAtTime) {
		int rise;
		int run;

		prmp = default;

		run = (int)(ramptime * SOUND_DMA_SPEED);        // 'samples' in ramp
		rise = targetval - initval;                     // height of ramp

		// init fixed point iterator to iterate along the height of the ramp 'rise'
		// always iterates from 0..'rise', increasing in value

		POS_ONE_Init(ref prmp.ps, ABS(rise), MathF.Abs((float)rise) / ((float)run));

		prmp.yprev = initval;
		prmp.initval = initval;
		prmp.target = targetval;
		prmp.sign = SIGN(rise);
		prmp.bEndAtTime = bEndAtTime;

	}

	// continues from current position to new target position

	static void RMP_SetNext(ref Rmp prmp, float ramptime, int targetval) {
		RMP_Init(ref prmp, ramptime, prmp.yprev, targetval, prmp.bEndAtTime);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool RMP_HitEnd(ref Rmp prmp) {
		return prmp.fhitend;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void RMP_SetEnd(ref Rmp prmp) {
		prmp.fhitend = true;
	}

	// get next ramp value & update ramp, if bEndAtTime is true, never varies by more than +1 or -1 between calls
	// when ramp hits target value, it thereafter always returns last value

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int RMP_GetNext(ref Rmp prmp) {
		int y;
		int d;

		// if we hit ramp end, return last value

		if (prmp.fhitend)
			return prmp.yprev;

		// get next integer position in ramp height.

		d = POS_ONE_GetNext(ref prmp.ps);

		if (prmp.ps.fhitend)
			prmp.fhitend = true;

		// increase or decrease from initval, depending on ramp sign

		if (prmp.sign > 0)
			y = prmp.initval + d;
		else
			y = prmp.initval - d;

		// if bEndAtTime is true, only update current height by a max of +1 or -1
		// this also means that for short ramp times, we may not hit target

		if (prmp.bEndAtTime) {
			if (ABS(y - prmp.yprev) >= 1)
				prmp.yprev += prmp.sign;
		}
		else {
			// always hits target - but varies by more than +/- 1

			prmp.yprev = y;
		}

		return prmp.yprev;
	}

	// get current ramp value, don't update ramp

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static int RMP_GetCurrent(ref Rmp prmp) {
		return prmp.yprev;
	}


	//////////////
	// mod delay
	//////////////

	// modulate delay time anywhere from 0..D using MDY_ChangeVal. no output glitches (uses RMP)

	public const int CMDYS = 64;                // max # of mod delays active (steals from delays)

	static readonly Mdy[] mdys = CreatePool<Mdy>(CMDYS);

	static void MDY_Init(Mdy? pmdy) { if (pmdy != null) pmdy.Clear(); }
	internal static void MDY_Free(Mdy? pmdy) { if (pmdy != null) { DLY_Free(pmdy.pdly); pmdy.Clear(); } }
	static void MDY_InitAll() { for (int i = 0; i < CMDYS; i++) MDY_Init(mdys[i]); }
	static void MDY_FreeAll() { for (int i = 0; i < CMDYS; i++) MDY_Free(mdys[i]); }


	// allocate mod delay, given previously allocated dly (NOTE: mod delay only sweeps tap 0, not t1,t2 or t3)
	// ramptime is time in seconds for delay to change from dcur to dnew
	// modtime is time in seconds between modulations. 0 if no self-modulation
	// depth is 0-1.0 multiplier, new delay values when modulating are Dnew = randomlong (D - D*depth, D)
	// mix - 0-1.0, default 1.0 for 100% fx mix - pans between input signal and fx signal

	static Mdy? MDY_Alloc(Dly? pdly, float ramptime, float modtime, float depth, float mix) {
		int i;
		Mdy pmdy;

		if (pdly == null)
			return null;

		for (i = 0; i < CMDYS; i++) {
			if (!mdys[i].fused) {
				pmdy = mdys[i];

				MDY_Init(pmdy);

				pmdy.pdly = pdly;
				pmdy.fused = true;
				pmdy.ramptime = ramptime;
				pmdy.mtime = SEC_TO_SAMPS(modtime);
				pmdy.mtimecur = pmdy.mtime;
				pmdy.depth = depth;
				pmdy.mix = (int)(PMAX * mix);
				pmdy.bPhaseInvert = false;

				return pmdy;
			}
		}

		DevMsg("DSP: Warning, failed to allocate mod delay.\n");
		return null;
	}

	// change to new delay tap value t samples, ramp linearly over ramptime seconds

	static void MDY_ChangeVal(Mdy pmdy, int t) {
		// if D > original delay value, cap at original value

		t = Math.Min(pmdy.pdly!.D0, t);

		pmdy.fchanging = true;

		// init interpolation ramp - always hit target

		RMP_Init(ref pmdy.rmp_interp, pmdy.ramptime, 0, PMAX, false);

		// init delay xfade values

		pmdy.pdly.tnew = t;
		pmdy.pdly.xf = 0;
	}

	// interpolate between current and target delay values

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static int MDY_GetNext(Mdy pmdy, int x) {
		int xout;

		if (!pmdy.fchanging) {
			// not modulating...

			xout = DLY_GetNext(pmdy.pdly!, x);

			if (pmdy.mtime == 0) {
				// return right away if not modulating (not changing and not self modulating)

				goto mdy_return;
			}
		}
		else {
			// modulating...

			xout = DLY_GetNextXfade(pmdy.pdly!, x);

			// get xfade ramp & set up delay xfade value for next call to DLY_GetNextXfade()

			pmdy.pdly.xf = RMP_GetNext(ref pmdy.rmp_interp); // 0...PMAX

			if (RMP_HitEnd(ref pmdy.rmp_interp)) {
				// done. set delay tap & value = target

				DLY_ChangeVal(pmdy.pdly, pmdy.pdly.tnew);

				pmdy.pdly.t = pmdy.pdly.tnew;

				pmdy.fchanging = false;
			}
		}

		// if self-modulating and timer has expired, get next change

		if (pmdy.mtime != 0 && pmdy.mtimecur-- == 0) {
			pmdy.mtimecur = pmdy.mtime;

			int D0 = pmdy.pdly!.D0;
			int Dnew;
			float D1;

			// modulate between 0 and 100% of d0

			D1 = (float)D0 * (1.0F - pmdy.depth);

			Dnew = RandomInt((int)D1, D0);

			// set up modulation to new value

			MDY_ChangeVal(pmdy, Dnew);
		}

	mdy_return:

		// reverse phase of output

		if (pmdy.bPhaseInvert)
			xout = -xout;

		// 100% fx mix

		if (pmdy.mix == PMAX)
			return xout;

		// special case 50/50 mix

		if (pmdy.mix == PMAX / 2)
			return (xout + x) >> 1;

		// return mix of input and processed signal

		return x + (((xout - x) * pmdy.mix) >> PBITS);
	}



	// batch version for performance
	// UNDONE: unwind MDY_GetNext so that it directly calls DLY_GetNextN:
	// UNDONE: a) if not currently modulating and never self-modulating, then just unwind like DLY_GetNext
	// UNDONE: b) if not currently modulating, figure out how many samples N until self-modulation timer kicks in again
	//			  and stream out N samples just like DLY_GetNext

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static void MDY_GetNextN(Mdy pmdy, Span<PortableSamplePair> pbuffer, int SampleCount, int op) {
		Span<PortableSamplePair> pb = pbuffer[..SampleCount];

		switch (op) {
			default:
			case OP_LEFT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = MDY_GetNext(pmdy, pb[i].Left);
				return;
			case OP_RIGHT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Right = MDY_GetNext(pmdy, pb[i].Right);
				return;
			case OP_LEFT_DUPLICATE:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = pb[i].Right = MDY_GetNext(pmdy, pb[i].Left);
				return;
		}
	}

	// parameter order

	const int mdy_idtype = 0;           // NOTE: first 8 params must match params in dly_e
	const int mdy_idelay = 1;
	const int mdy_ifeedback = 2;
	const int mdy_igain = 3;

	const int mdy_iftype = 4;
	const int mdy_icutoff = 5;
	const int mdy_iqwidth = 6;
	const int mdy_iquality = 7;

	const int mdy_imodrate = 8;
	const int mdy_imoddepth = 9;
	const int mdy_imodglide = 10;

	const int mdy_imix = 11;
	const int mdy_ibxfade = 12;

	const int mdy_cparam = 13;


	// parameter ranges

	static readonly PrmRng[] mdy_rng = [

		new(mdy_cparam, 0, 0),              // first entry is # of parameters

		// delay params

		new(mdy_idtype, 0, DLY_MAX),        // delay type DLY_PLAIN, DLY_LOWPASS, DLY_ALLPASS
		new(mdy_idelay, 0.0f, 1000.0f),     // delay in milliseconds
		new(mdy_ifeedback, 0.0f, 0.99f),    // feedback 0-1.0
		new(mdy_igain, 0.0f, 1.0f),         // final gain of output stage, 0-1.0

		// filter params if mdy type DLY_LOWPASS

		new(mdy_iftype, 0, FTR_MAX),
		new(mdy_icutoff, 10.0f, 22050.0f),
		new(mdy_iqwidth, 100.0f, 11025.0f),
		new(mdy_iquality, 0, QUA_MAX),

		new(mdy_imodrate, 0.01f, 200.0f),   // frequency at which delay values change to new random value. 0 is no self-modulation
		new(mdy_imoddepth, 0.0f, 1.0f),     // how much delay changes (decreases) from current value (0-1.0)
		new(mdy_imodglide, 0.01f, 100.0f),  // glide time between dcur and dnew in milliseconds
		new(mdy_imix, 0.0f, 1.0f)           // 1.0 = full fx mix, 0.5 = 50% fx, 50% dry
	];


	// convert user parameters to internal parameters, allocate and return

	static Mdy? MDY_Params(ref Prc pprc) {
		Mdy? pmdy;
		Dly? pdly;

		float ramptime = pprc.prm[mdy_imodglide] / 1000.0F;            // get ramp time in seconds
		float modtime = 0.0f;
		if (pprc.prm[mdy_imodrate] != 0.0f)
			modtime = 1.0F / pprc.prm[mdy_imodrate];               // time between modulations in seconds
		float depth = pprc.prm[mdy_imoddepth];                     // depth of modulations 0-1.0
		float mix = pprc.prm[mdy_imix];

		// alloc plain, allpass or lowpass delay

		pdly = DLY_Params(ref pprc);

		if (pdly == null)
			return null;

		pmdy = MDY_Alloc(pdly, ramptime, modtime, depth, mix);

		return pmdy;
	}

	static DspProcessor? MDY_VParams(ref Prc p) {
		PRC_CheckParams(ref p, mdy_rng);
		return MDY_Params(ref p);
	}

	// v is +/- 0-1.0
	// change current delay value 0..D

	internal static void MDY_Mod(Mdy p, float v) {
		Mdy pmdy = p;

		int D0 = pmdy.pdly!.D0;                // base delay value
		float v2;

		// if v is < -2.0 then delay is v + 10.0
		// invert phase of output. hack.

		if (v < -2.0F) {
			v = v + 10.0F;
			pmdy.bPhaseInvert = true;
		}
		else
			pmdy.bPhaseInvert = false;

		v2 = -(v + 1.0F) / 2.0F;                // v2 varies -1.0-0.0

		// D0 varies 0..D0

		D0 = D0 + (int)((float)D0 * v2);

		// change delay

		MDY_ChangeVal(pmdy, D0);

		return;
	}


	///////////////////
	// Parallel reverbs
	///////////////////

	// Reverb A
	// M parallel reverbs, mixed to mono output

	public const int CRVAS = 64;                // max number of parallel series reverbs active

	public const int CRVA_DLYS = 12;            // max number of delays making up reverb_a

	static readonly Rva[] rvas = CreatePool<Rva>(CRVAS);

	static void RVA_Init(Rva? prva) { if (prva != null) prva.Clear(); }
	static void RVA_InitAll() { for (int i = 0; i < CRVAS; i++) RVA_Init(rvas[i]); }

	// free parallel series reverb

	internal static void RVA_Free(Rva? prva) {
		int i;

		if (prva != null) {
			// free all delays
			for (i = 0; i < CRVA_DLYS; i++)
				DLY_Free(prva.GetDly(i));

			// zero all ptrs to delays in mdy array
			for (i = 0; i < CRVA_DLYS; i++) {
				if (prva.GetMdy(i) != null)
					prva.GetMdy(i)!.pdly = null;
			}

			// free all mod delays
			for (i = 0; i < CRVA_DLYS; i++)
				MDY_Free(prva.GetMdy(i));

			FLT_Free(prva.pflt);

			prva.Clear();
		}
	}


	static void RVA_FreeAll() { for (int i = 0; i < CRVAS; i++) RVA_Free(rvas[i]); }

	// create parallel reverb - m parallel reverbs summed

	// D array of CRVB_DLYS reverb delay sizes max sample index w[0...D] (ie: D+1 samples)
	// a array of reverb feedback parms for parallel reverbs (CRVB_P_DLYS)
	//		if a[i] < 0 then this is a predelay - use DLY_FLINEAR instead of DLY_LOWPASS
	// b array of CRVB_P_DLYS - mix params for parallel reverbs
	// m - number of parallel delays
	// pflt - filter template, to be used by all parallel delays
	// fparallel - true if filter operates in parallel with delays, otherwise filter output only
	// fmoddly -  > 0 if delays are all mod delays (milliseconds of delay modulation)
	// fmodrate - # of delay repetitions between changes to mod delay
	// ftaps - if > 0, use 4 taps per reverb delay unit (increases density) tap = D - n*ftaps  n = 0,1,2,3

	static Rva? RVA_Alloc(ReadOnlySpan<int> D, ReadOnlySpan<int> a, ReadOnlySpan<int> b, int m, Flt? pflt, int fparallel, float fmoddly, float fmodrate, float ftaps) {

		int i;
		int dtype;
		Rva prva;
		Flt? pflt2 = null;

		bool btaps = ftaps > 0.0;

		// find open slot

		for (i = 0; i < CRVAS; i++) {
			if (!rvas[i].fused)
				break;
		}

		// return null if no free slots

		if (i == CRVAS) {
			DevMsg("DSP: Warning, failed to allocate reverb.\n");
			return null;
		}

		prva = rvas[i];

		// if series filter specified, alloc two series filters

		if (pflt != null && fparallel == 0) {
			// use filter data as template for a filter on output (2 cascaded filters)

			pflt2 = FLT_Alloc(0, pflt.M, pflt.L, pflt.a, pflt.b, 1.0f);

			if (pflt2 == null) {
				DevMsg("DSP: Warning, failed to allocate flt for reverb.\n");
				return null;
			}

			pflt2.pf1 = FLT_Alloc(0, pflt.M, pflt.L, pflt.a, pflt.b, 1.0f);
			pflt2.N = 1;
		}

		// allocate parallel delays

		for (i = 0; i < m; i++) {
			// set delay type

			if (pflt != null && fparallel != 0)
				// if a[i] param is < 0, allocate delay as predelay instead of feedback delay
				dtype = a[i] < 0 ? DLY_FLINEAR : DLY_LOWPASS;
			else
				// if no filter specified, alloc as plain or multitap plain delay
				dtype = btaps ? DLY_PLAIN_4TAP : DLY_PLAIN;

			if (dtype == DLY_LOWPASS && btaps)
				dtype = DLY_LOWPASS_4TAP;

			// if filter specified and parallel specified, alloc 1 filter per delay

			if (DLY_HAS_FILTER(dtype))
				prva.SetDly(i, DLY_AllocLP(D[i], Math.Abs(a[i]), b[i], dtype, pflt!.M, pflt.L, pflt.a, pflt.b));
			else
				prva.SetDly(i, DLY_Alloc(D[i], Math.Abs(a[i]), b[i], dtype));

			if (DLY_HAS_MULTITAP(dtype)) {
				// set up delay taps to increase density around delay value.

				// value of ftaps is the seed for all tap values

				float t1 = Math.Max((float)MSEC_TO_SAMPS(5), D[i] * (1.0F - ftaps * MathF.PI));
				float t2 = Math.Max((float)MSEC_TO_SAMPS(7), D[i] * (1.0F - ftaps * 1.697043F));
				float t3 = Math.Max((float)MSEC_TO_SAMPS(10), D[i] * (1.0F - ftaps * 0.96325F));

				DLY_ChangeTaps(prva.GetDly(i), (int)t1, (int)t2, (int)t3, D[i]);
			}
		}


		if (fmoddly > 0.0) {
			// alloc mod delays, using previously alloc'd delays

			// ramptime is time in seconds for delay to change from dcur to dnew
			// modtime is time in seconds between modulations. 0 if no self-modulation
			// depth is 0-1.0 multiplier, new delay values when modulating are Dnew = randomlong (D - D*depth, D)

			float ramptime;
			float modtime;
			float depth;

			for (i = 0; i < m; i++) {
				int Do = prva.GetDly(i)!.D;

				modtime = (float)Do / (float)SOUND_DMA_SPEED;   // seconds per delay
				depth = (fmoddly * 0.001f) / modtime;                               // convert milliseconds to 'depth' %
				depth = Math.Clamp(depth, 0.01f, 0.99f);
				modtime = modtime * fmodrate;                                       // modulate every N delay passes

				ramptime = Math.Min(20.0f / 1000.0f, modtime / 2);                          // ramp between delay values in N ms

				prva.SetMdy(i, MDY_Alloc(prva.GetDly(i), ramptime, modtime, depth, 1.0f));
			}

			prva.fmoddly = true;
		}

		// if we failed to alloc any reverb, free all, return NULL

		for (i = 0; i < m; i++) {
			if (prva.GetDly(i) == null) {
				FLT_Free(pflt2);
				RVA_Free(prva);
				DevMsg("DSP: Warning, failed to allocate delay for reverb.\n");
				return null;
			}
		}

		prva.fused = true;
		prva.m = m;
		prva.fparallel = fparallel;
		prva.pflt = pflt2;
		return prva;
	}


	// parallel reverberator
	//
	// for each input sample x do:
	//		x0 = plain(D0,w0,&p0,a0,x)
	//		x1 = plain(D1,w1,&p1,a1,x)
	//		x2 = plain(D2,w2,&p2,a2,x)
	//		x3 = plain(D3,w3,&p3,a3,x)
	//		y = b0*x0 + b1*x1 + b2*x2 + b3*x3
	//
	//		rgdly - array of M delays:
	//		D - Delay values (typical - 29, 37, 44, 50, 27, 31)
	//		w - array of delayed values
	//		p - array of pointers to circular delay line pointers
	//		a - array of M feedback values (typical - all equal, like 0.75 * PMAX)
	//		b - array of M gain values for plain reverb outputs (1, .9, .8, .7)
	//		xin - input value
	//		if fparallel, filters are built into delays,
	//		otherwise, filter is in feedback loop


	static readonly int[] g_MapIntoPBITSDivInt = [
		0, PMAX/1, PMAX/2,  PMAX/3, PMAX/4, PMAX/5, PMAX/6, PMAX/7, PMAX/8,
		   PMAX/9, PMAX/10, PMAX/11,PMAX/12,PMAX/13,PMAX/14,PMAX/15,PMAX/16,
	];

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static int RVA_GetNext(Rva prva, int x) {
		int m = prva.m;
		int y = 0;

		if (prva.fmoddly) {
			// get output of parallel mod delays

			for (int i = 0; i < m; i++)
				y += MDY_GetNext(prva.GetMdy(i)!, x);
		}
		else {
			// get output of parallel delays

			for (int i = 0; i < m; i++)
				y += DLY_GetNext(prva.GetDly(i)!, x);
		}

		// PERFORMANCE: y/m is now baked into the 'b' gain params for each delay ( b = b/m )
		// y = (y * g_MapIntoPBITSDivInt[m]) >> PBITS;

		if (prva.fparallel != 0)
			return y;

		// run series filters if present

		if (prva.pflt != null)
			y = FLT_GetNext(prva.pflt, y);

		return y;
	}



	// batch version for performance
	// UNDONE: unwind RVA_GetNextN so that it directly calls DLY_GetNextN or MDY_GetNextN

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static void RVA_GetNextN(Rva prva, Span<PortableSamplePair> pbuffer, int SampleCount, int op) {
		Span<PortableSamplePair> pb = pbuffer[..SampleCount];

		switch (op) {
			default:
			case OP_LEFT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = RVA_GetNext(prva, pb[i].Left);
				return;
			case OP_RIGHT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Right = RVA_GetNext(prva, pb[i].Right);
				return;
			case OP_LEFT_DUPLICATE:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = pb[i].Right = RVA_GetNext(prva, pb[i].Left);
				return;
		}
	}

	// reverb parameter order

	// parameter order

	const int rva_size_max = 0;
	const int rva_size_min = 1;

	const int rva_inumdelays = 2;
	const int rva_ifeedback = 3;
	const int rva_igain = 4;

	const int rva_icutoff = 5;

	const int rva_ifparallel = 6;
	const int rva_imoddly = 7;
	const int rva_imodrate = 8;

	const int rva_width = 9;
	const int rva_depth = 10;
	const int rva_height = 11;

	const int rva_fbwidth = 12;
	const int rva_fbdepth = 13;
	const int rva_fbheight = 14;

	const int rva_iftaps = 15;

	const int rva_cparam = 16;      // # of params

	// filter parameter ranges

	static readonly PrmRng[] rva_rng = [

		new(rva_cparam, 0, 0),          // first entry is # of parameters

		// reverb params
		new(rva_size_max, 0.0f, 1000.0f),   // max room delay in milliseconds
		new(rva_size_min, 0.0f, 1000.0f),   // min room delay in milliseconds
		new(rva_inumdelays, 1.0f, 12.0f),       // controls # of parallel or series delays
		new(rva_ifeedback, 0.0f, 1.0f),     // feedback of delays
		new(rva_igain, 0.0f, 10.0f),        // output gain

		// filter params for each parallel reverb (quality set to 0 for max execution speed)

		new(rva_icutoff, 10, 22050),

		new(rva_ifparallel, 0, 1),          // if 1, then all filters operate in parallel with delays. otherwise filter output only
		new(rva_imoddly, 0.0f, 50.0f),      // if > 0 then all delays are modulating delays, mod param controls milliseconds of mod depth
		new(rva_imodrate, 0.0f, 10.0f),     // how many delay repetitions pass between mod changes to delayl

		// override params - for more detailed description of room
		// note: width/depth/height < 0 only for some automatic dsp presets
		new(rva_width, -1000.0f, 1000.0f),  // 0-1000.0 millisec (room width in feet) - used instead of size if non-zero
		new(rva_depth, -1000.0f, 1000.0f),  // 0-1000.0 room depth in feet - used instead of size if non-zero
		new(rva_height, -1000.0f, 1000.0f), // 0-1000.0 room height in feet - used instead of size if non-zero

		new(rva_fbwidth, -1.0f, 1.0f),      // 0-1.0 material reflectivity - used as feedback param instead of decay if non-zero
		new(rva_fbdepth, -1.0f, 1.0f),      // 0-1.0 material reflectivity - used as feedback param instead of decay if non-zero
		new(rva_fbheight, -1.0f, 1.0f),     // 0-1.0 material reflectivity - used as feedback param instead of decay if non-zero
											// if < 0, a predelay is allocated, then feedback is -1*param given

		new(rva_iftaps, 0.0f, 0.333f)       // if > 0, use 3 extra taps with delay values = d * (1 - faps*n) n = 0,1,2,3
	];

	const int RVA_BASEM = 1;                // base number of parallel delays

	// nominal delay and feedback values. More delays = more density.

	const int RVADLYSMAX = 49;
	static readonly float[] rvadlys = [18, 23, 28, 33, 42, 21, 26, 36, 39, 45, 47, 30];
	static readonly float[] rvafbs = [0.9f, 0.9f, 0.9f, 0.85f, 0.8f, 0.9f, 0.9f, 0.85f, 0.8f, 0.8f, 0.8f, 0.85f];

	const int RVA_MIN_SEPARATION = 7;                   // minimum separation between reverbs, in ms.

	// Construct D,a,b delay arrays given array of length,width,height sizes and feedback values
	// rgd[] array of delay values in milliseconds (feet)
	// rgf[] array of feedback values 0..1
	// m # of parallel reverbs to construct
	// D[] array of output delay values for parallel reverbs
	// a[] array of output feedback values
	// b[] array of output gain values = 1/m
	// gain - output gain
	// feedback - default feedback if rgf members are 0

	static void RVA_ConstructDelays(Span<float> rgd, Span<float> rgf, int m, Span<int> D, Span<int> a, Span<int> b, float gain, float feedback) {

		int i;
		float r;
		int d;
		float d1, d2, dm;
		bool bpredelay;

		// sort descending, so rgd[0] is largest delay & rgd[2] is smallest

		if (rgd[2] > rgd[1]) { (rgd[2], rgd[1]) = (rgd[1], rgd[2]); (rgf[2], rgf[1]) = (rgf[1], rgf[2]); }
		if (rgd[1] > rgd[0]) { (rgd[0], rgd[1]) = (rgd[1], rgd[0]); (rgf[0], rgf[1]) = (rgf[1], rgf[0]); }
		if (rgd[2] > rgd[1]) { (rgd[2], rgd[1]) = (rgd[1], rgd[2]); (rgf[2], rgf[1]) = (rgf[1], rgf[2]); }

		// if all feedback values 0, use default feedback

		if (rgf[0] == 0.0 && rgf[1] == 0.0 && rgf[2] == 0.0) {
			// use feedback param for all

			rgf[0] = rgf[1] = rgf[2] = feedback;

			// adjust feedback down for larger delays so that decay is constant for all delays

			rgf[0] = DLY_NormalizeFeedback(rgd[2], rgf[2], rgd[0]);
			rgf[1] = DLY_NormalizeFeedback(rgd[2], rgf[2], rgd[1]);

		}

		// make sure all reverbs are different by at least RVA_MIN_SEPARATION * m/3	m is 3,6,9 or 12

		int dmin = (m / 3) * RVA_MIN_SEPARATION;

		d1 = rgd[1] - rgd[2];

		if (d1 <= dmin)
			rgd[1] += dmin - d1;    // make difference = dmin

		d2 = rgd[0] - rgd[1];

		if (d2 <= dmin)
			rgd[0] += dmin - d1;    // make difference = dmin

		for (i = 0; i < m; i++) {
			// reverberations due to room width, depth, height
			// assume sound moves at approx 1ft/ms

			int j = (int)((float)i % 3.0f);    // j counts   0,1,2  0,1,2 0,1..

			d = (int)rgd[j];
			r = MathF.Abs(rgf[j]);

			bpredelay = (rgf[j] < 0) && i < 3;

			// re-use predelay values as reverb values:

			if (rgf[j] < 0 && !bpredelay)
				d = Math.Max((int)(rgd[j] / 4.0F), RVA_MIN_SEPARATION);

			if (i < 3)
				dm = 0.0F;
			else
				dm = Math.Max((float)(RVA_MIN_SEPARATION * (i / 3)), ((i / 3) * ((float)d * 0.18F)));

			d += (int)dm;
			D[i] = MSEC_TO_SAMPS(d);

			// D[i] = MSEC_TO_SAMPS(d + ((i/3) * RVA_MIN_SEPARATION));		// (i/3) counts 0,0,0 1,1,1 2,2,2 ... separate all reverbs by 5ms

			// feedback - due to wall/floor/ceiling reflectivity
			a[i] = (int)Math.Min(0.999 * PMAX, (double)PMAX * r);

			if (bpredelay)
				a[i] = -a[i];       // flag delay as predelay

			b[i] = (int)((float)(gain * PMAX) / (float)m);
		}
	}

	static Rva? RVA_Params(ref Prc pprc) {
		Rva? prva;

		float size_max = pprc.prm[rva_size_max];   // max delay size
		float size_min = pprc.prm[rva_size_min];   // min delay size

		float numdelays = pprc.prm[rva_inumdelays];    // controls # of parallel delays
		float feedback = pprc.prm[rva_ifeedback];      // 0-1.0 controls feedback parameters
		float gain = pprc.prm[rva_igain];          // 0-10.0 controls output gain

		float cutoff = pprc.prm[rva_icutoff];      // filter cutoff

		float fparallel = pprc.prm[rva_ifparallel];    // if true, all filters are in delay feedback paths - otherwise single flt on output

		float fmoddly = pprc.prm[rva_imoddly];     // if > 0, milliseconds of delay mod depth
		float fmodrate = pprc.prm[rva_imodrate];       // if fmoddly > 0, # of delay repetitions between modulations

		float width = MathF.Abs(pprc.prm[rva_width]);          // 0-1000 controls size of 1/3 of delays - used instead of size if non-zero
		float depth = MathF.Abs(pprc.prm[rva_depth]);          // 0-1000 controls size of 1/3 of delays - used instead of size if non-zero
		float height = MathF.Abs(pprc.prm[rva_height]);        // 0-1000 controls size of 1/3 of delays - used instead of size if non-zero

		float fbwidth = pprc.prm[rva_fbwidth];     // feedback parameter for walls	0..2
		float fbdepth = pprc.prm[rva_fbdepth];     // feedback parameter for floor
		float fbheight = pprc.prm[rva_fbheight];       // feedback parameter for ceiling

		float ftaps = pprc.prm[rva_iftaps];        // if > 0 increase reverb density using 3 extra taps d = (1.0 - ftaps * n) n = 0,1,2,3



		//	RVA_PerfTest();

		// D array of CRVB_DLYS reverb delay sizes max sample index w[0...D] (ie: D+1 samples)
		// a array of reverb feedback parms for parallel delays
		// b array of CRVB_P_DLYS - mix params for parallel reverbs
		// m - number of parallel delays

		Span<int> D = stackalloc int[CRVA_DLYS];
		Span<int> a = stackalloc int[CRVA_DLYS];
		Span<int> b = stackalloc int[CRVA_DLYS];
		int m;

		// limit # delays 1-12

		m = (int)Math.Clamp(numdelays, (float)RVA_BASEM, (float)CRVA_DLYS);

		// set up D (delay) a (feedback) b (gain) arrays

		if ((int)width != 0 || (int)height != 0 || (int)depth != 0) {
			// if width, height, depth given, use values as simple delays

			Span<float> rgd = stackalloc float[3];
			Span<float> rgfb = stackalloc float[3];

			// force m to 3, 6, 9 or 12

			if (m < 3) m = 3;
			if (m > 3 && m < 6) m = 6;
			if (m > 6 && m < 9) m = 9;
			if (m > 9) m = 12;

			rgd[0] = width; rgfb[0] = fbwidth;
			rgd[1] = depth; rgfb[1] = fbdepth;
			rgd[2] = height; rgfb[2] = fbheight;

			RVA_ConstructDelays(rgd, rgfb, m, D, a, b, gain, feedback);
		}
		else {
			// use size parameter instead of width/depth/height

			for (int i = 0; i < m; i++) {
				// delays of parallel reverb.  D[0] = size_min.

				D[i] = MSEC_TO_SAMPS(size_min + (int)(((float)(size_max - size_min) / (float)m) * (float)i));

				// feedback and gain of parallel reverb

				if (i == 0) {
					// set feedback for smallest delay

					a[i] = (int)Math.Min(0.999 * PMAX, (double)PMAX * feedback);
				}
				else {
					// adjust feedback down for larger delays so that decay time is constant

					a[i] = (int)Math.Min(0.999 * PMAX, (double)PMAX * DLY_NormalizeFeedback(D[0], feedback, D[i]));
				}

				b[i] = (int)((float)(gain * PMAX) / (float)m);
			}
		}

		// add filter

		Flt? pflt = null;

		if (cutoff != 0) {

			// set up dummy lowpass filter to convert params

			Prc prcf = default;

			prcf.prm[flt_iquality] = QUA_LO;    // force filter to low quality for faster execution time
			prcf.prm[flt_icutoff] = cutoff;
			prcf.prm[flt_iftype] = FLT_LP;
			prcf.prm[flt_iqwidth] = 0;
			prcf.prm[flt_igain] = 1.0f;

			pflt = FLT_Params(ref prcf);
		}

		prva = RVA_Alloc(D, a, b, m, pflt, (int)fparallel, fmoddly, fmodrate, ftaps);

		FLT_Free(pflt);

		return prva;
	}


	static DspProcessor? RVA_VParams(ref Prc p) {
		PRC_CheckParams(ref p, rva_rng);
		return RVA_Params(ref p);
	}

	internal static void RVA_Mod(Rva p, float v) { return; }



	////////////
	// Diffusor
	///////////

	// (N series allpass reverbs)

	public const int CDFRS = 64;                // max number of series reverbs active

	public const int CDFR_DLYS = 16;            // max number of delays making up diffusor

	static readonly Dfr[] dfrs = CreatePool<Dfr>(CDFRS);

	static void DFR_Init(Dfr? pdfr) { if (pdfr != null) pdfr.Clear(); }
	static void DFR_InitAll() { for (int i = 0; i < CDFRS; i++) DFR_Init(dfrs[i]); }

	// free parallel series reverb

	internal static void DFR_Free(Dfr? pdfr) {
		if (pdfr != null) {
			// free all delays

			for (int i = 0; i < CDFR_DLYS; i++)
				DLY_Free(pdfr.GetDly(i));

			pdfr.Clear();
		}
	}


	static void DFR_FreeAll() { for (int i = 0; i < CDFRS; i++) DFR_Free(dfrs[i]); }

	// create n series allpass reverbs

	// D array of CRVB_DLYS reverb delay sizes max sample index w[0...D] (ie: D+1 samples)
	// a array of reverb feedback parms for series delays
	// b array of gain params for parallel reverbs
	// n - number of series delays

	static Dfr? DFR_Alloc(ReadOnlySpan<int> D, ReadOnlySpan<int> a, ReadOnlySpan<int> b, int n) {

		int i;
		Dfr pdfr;

		// find open slot

		for (i = 0; i < CDFRS; i++) {
			if (!dfrs[i].fused)
				break;
		}

		// return null if no free slots

		if (i == CDFRS) {
			DevMsg("DSP: Warning, failed to allocate diffusor.\n");
			return null;
		}

		pdfr = dfrs[i];

		DFR_Init(pdfr);

		// alloc reverbs

		for (i = 0; i < n; i++)
			pdfr.SetDly(i, DLY_Alloc(D[i], a[i], b[i], DLY_ALLPASS));

		// if we failed to alloc any reverb, free all, return NULL

		for (i = 0; i < n; i++) {
			if (pdfr.GetDly(i) == null) {
				DFR_Free(pdfr);
				DevMsg("DSP: Warning, failed to allocate delay for diffusor.\n");
				return null;
			}
		}

		pdfr.fused = true;
		pdfr.n = n;

		return pdfr;
	}


	// series reverberator

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static int DFR_GetNext(Dfr pdfr, int x) {
		int i;
		int y;
		Dly? pdly;

		y = x;

		for (i = 0; i < pdfr.n; i++) {
			pdly = pdfr.GetDly(i);
			y = DelayAllpass(pdly.D, pdly.t, pdly.w!, ref pdly.p, pdly.a, pdly.b, y);
		}

		return y;
	}


	// batch version for performance

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static void DFR_GetNextN(Dfr pdfr, Span<PortableSamplePair> pbuffer, int SampleCount, int op) {
		Span<PortableSamplePair> pb = pbuffer[..SampleCount];

		switch (op) {
			default:
			case OP_LEFT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = DFR_GetNext(pdfr, pb[i].Left);
				return;
			case OP_RIGHT:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Right = DFR_GetNext(pdfr, pb[i].Right);
				return;
			case OP_LEFT_DUPLICATE:
				for (int i = 0; i < pb.Length; i++)
					pb[i].Left = pb[i].Right = DFR_GetNext(pdfr, pb[i].Left);
				return;
		}
	}

	const int DFR_BASEN = 1;                // base number of series allpass delays

	// nominal diffusor delay and feedback values

	static readonly float[] dfrdlys = [13, 19, 26, 21, 32, 36, 38, 16, 24, 28, 41, 35, 10, 46, 50, 27];
	static readonly float[] dfrfbs = [1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f];


	// diffusor parameter order

	// parameter order

	const int dfr_isize = 0;
	const int dfr_inumdelays = 1;
	const int dfr_ifeedback = 2;
	const int dfr_igain = 3;

	const int dfr_cparam = 4;               // # of params

	// diffusor parameter ranges

	static readonly PrmRng[] dfr_rng = [

		new(dfr_cparam, 0, 0),          // first entry is # of parameters

		new(dfr_isize, 0.0f, 1.0f), // 0-1.0 scales all delays
		new(dfr_inumdelays, 0.0f, 4.0f),    // 0-4.0 controls # of series delays
		new(dfr_ifeedback, 0.0f, 1.0f), // 0-1.0 scales all feedback parameters
		new(dfr_igain, 0.0f, 10.0f),        // 0-1.0 scales all feedback parameters
	];


	static Dfr? DFR_Params(ref Prc pprc) {
		Dfr? pdfr;
		int i;
		int s;
		float size = pprc.prm[dfr_isize];          // 0-1.0 scales all delays
		float numdelays = pprc.prm[dfr_inumdelays];        // 0-4.0 controls # of series delays
		float feedback = pprc.prm[dfr_ifeedback];      // 0-1.0 scales all feedback parameters
		float gain = pprc.prm[dfr_igain];          // 0-10.0 controls output gain

		// D array of CRVB_DLYS reverb delay sizes max sample index w[0...D] (ie: D+1 samples)
		// a array of reverb feedback parms for series delays (CRVB_S_DLYS)
		// b gain of each reverb section
		// n - number of series delays

		Span<int> D = stackalloc int[CDFR_DLYS];
		Span<int> a = stackalloc int[CDFR_DLYS];
		Span<int> b = stackalloc int[CDFR_DLYS];
		int n;

		if (gain == 0.0)
			gain = 1.0f;

		// get # series diffusors

		// limit m, n to half max number of delays

		n = Math.Clamp((int)numdelays, DFR_BASEN, CDFR_DLYS / 2);

		// compute delays for diffusors

		for (i = 0; i < n; i++) {
			s = (int)(dfrdlys[i] * size);

			// delay of diffusor

			D[i] = MSEC_TO_SAMPS(s);

			// feedback and gain of diffusor

			a[i] = (int)Math.Min(0.999 * PMAX, (double)(dfrfbs[i] * PMAX * feedback));
			b[i] = (int)((float)(gain * (float)PMAX));
		}


		pdfr = DFR_Alloc(D, a, b, n);

		return pdfr;
	}

	static DspProcessor? DFR_VParams(ref Prc p) {
		PRC_CheckParams(ref p, dfr_rng);
		return DFR_Params(ref p);
	}

	internal static void DFR_Mod(Dfr p, float v) { return; }
}
