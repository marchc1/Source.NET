global using static Source.AudioSystem.SndMixBuf;
global using static Source.AudioSystem.SndFixedInt;
global using fixedint = uint;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source.AudioSystem;

public static class SndMixBuf
{
	// OPTIMIZE: note that making this larger will not increase performance (12/27/03)
	public const int PAINTBUFFER_SIZE = 1020;   // 44k: was 512

	public const int SOUND_BUFFER_PAINT = 0;
	public const int SOUND_BUFFER_ROOM = 1;
	public const int SOUND_BUFFER_FACING = 2;
	public const int SOUND_BUFFER_FACINGAWAY = 3;
	public const int SOUND_BUFFER_DRY = 4;
	public const int SOUND_BUFFER_SPEAKER = 5;

	public const int SOUND_BUFFER_BASETOTAL = 6;
	public const int SOUND_BUFFER_SPECIAL_START = SOUND_BUFFER_BASETOTAL;

	// sound mixing buffer
	public const int CPAINTFILTERMEM = 3;
	public const int CPAINTFILTERS = 4;         // maximum number of consecutive upsample passes per paintbuffer

	// must be at least PAINTBUFFER_SIZE+1 for upsampling
	public const int PAINTBUFFER_MEM_SIZE = PAINTBUFFER_SIZE + 4;

	// size in samples of copy buffer used by pitch shifters in mixing
	// allow more memory for this on PC for developers to pitch-shift their way through dialog
	public const int TEMP_COPY_BUFFER_SIZE = PAINTBUFFER_MEM_SIZE * 4;

	// hard clip input value to -32767 <= y <= 32767
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CLIP(int x) => x > 32767 ? 32767 : (x < -32767 ? -32767 : x);

	public static PortableSamplePair[] PAINTBUFFER => g_curpaintbuffer!;
	public static PortableSamplePair[]? REARPAINTBUFFER => g_currearpaintbuffer;
	public static PortableSamplePair[]? CENTERPAINTBUFFER => g_curcenterpaintbuffer;
}

// !!! if this is changed, it much be changed in native assembly too !!!
[StructLayout(LayoutKind.Sequential)]
public struct PortableSamplePair
{
	public const int SIZE = sizeof(int) * 2;

	public int Left;
	public int Right;
}

public class PaintBuffer
{
	public bool Active;                     // if true, mix to this paintbuffer using flags
	public bool Surround;                   // if true, mix to front and rear paintbuffers using flags
	public bool SurroundCenter;             // if true, mix to front, rear and center paintbuffers using flags

	public int IdspSpecialDsp;
	public int PrevSpecialDSP;
	public int SpecialDSP;

	public int Flags;                       // SOUND_BUSS_ROOM, SOUND_BUSS_FACING, SOUND_BUSS_FACINGAWAY, SOUND_BUSS_SPEAKER, SOUND_BUSS_SPECIAL_DSP, SOUND_BUSS_DRY

	public PortableSamplePair[] Buf = null!;    // front stereo mix buffer, for 2 or 4 channel mixing
	public PortableSamplePair[]? BufRear;       // rear mix buffer, for 4 channel mixing
	public PortableSamplePair[]? BufCenter;     // center mix buffer, for 5 channel mixing

	public int IFilter;                     // current filter memory buffer to use for upsampling pass

	public const int FILTER_MEM_COUNT = CPAINTFILTERS * CPAINTFILTERMEM;

	public readonly PortableSamplePair[] FltMem = new PortableSamplePair[FILTER_MEM_COUNT];          // filter memory, for upsampling with linear or cubic interpolation
	public readonly PortableSamplePair[] FltMemRear = new PortableSamplePair[FILTER_MEM_COUNT];      // filter memory, for upsampling with linear or cubic interpolation
	public readonly PortableSamplePair[] FltMemCenter = new PortableSamplePair[FILTER_MEM_COUNT];    // filter memory, for upsampling with linear or cubic interpolation

	public Span<PortableSamplePair> GetFltMem(int ifilter) => FltMem.AsSpan(ifilter * CPAINTFILTERMEM, CPAINTFILTERMEM);
	public Span<PortableSamplePair> GetFltMemRear(int ifilter) => FltMemRear.AsSpan(ifilter * CPAINTFILTERMEM, CPAINTFILTERMEM);
	public Span<PortableSamplePair> GetFltMemCenter(int ifilter) => FltMemCenter.AsSpan(ifilter * CPAINTFILTERMEM, CPAINTFILTERMEM);
}

// fixed point stuff for real-time resampling
public static class SndFixedInt
{
	public const int FIX_BITS = 28;
	public const int FIX_SCALE = 1 << FIX_BITS;
	public const int FIX_MASK = (1 << FIX_BITS) - 1;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int FIX_FLOAT(double a) => (int)(a * FIX_SCALE);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int FIX(int a) => a << FIX_BITS;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int FIX_INTPART(uint a) => ((int)a) >> FIX_BITS;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int FIX_FRACTION(int a, int b) => FIX(a) / b;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint FIX_FRACPART(uint a) => a & FIX_MASK;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static double FIX_TODOUBLE(uint a) => (double)a / (double)FIX_SCALE;

	public const int FIX_BITS14 = 14;
	public const int FIX_SCALE14 = 1 << FIX_BITS14;
	public const int FIX_MASK14 = (1 << FIX_BITS14) - 1;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int FIX_FLOAT14(double a) => (int)(a * FIX_SCALE14);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int FIX14(int a) => a << FIX_BITS14;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int FIX_INTPART14(uint a) => ((int)a) >> FIX_BITS14;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int FIX_FRACTION14(int a, int b) => FIX14(a) / b;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint FIX_FRACPART14(uint a) => a & FIX_MASK14;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static double FIX_14TODOUBLE(uint a) => (double)a / (double)FIX_SCALE14;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint FIX_28TO14(uint a) => (uint)(int)(a >> (FIX_BITS - 14));
}
