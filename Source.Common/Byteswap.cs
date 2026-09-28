using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source.Common;

public interface IByteOrder
{
	static abstract bool IsBigEndian();
}

public sealed class LittleEndianOrder : IByteOrder
{
	private LittleEndianOrder() { }
	public static bool IsBigEndian() => false;
}

public sealed class BigEndianOrder : IByteOrder
{
	private BigEndianOrder() { }
	public static bool IsBigEndian() => true;
}

public sealed class NativeEndianOrder : IByteOrder
{
	private NativeEndianOrder() { }
	public static bool IsBigEndian() => !BitConverter.IsLittleEndian;
}

public readonly struct Byteswap<TOrder> where TOrder : IByteOrder
{
	// Default behavior sets the target endian to match TOrder.
	// Stored relative to TOrder so that default(Byteswap<TOrder>) is valid.
	private readonly bool flipped;

	private Byteswap(bool flipped) => this.flipped = flipped;

	/// <summary>
	/// True if the current machine is detected as big endian.
	/// (Endienness is effectively detected at compile time when optimizations are
	/// enabled)
	/// </summary>
	public bool IsMachineBigEndian() => !BitConverter.IsLittleEndian;

	/// <summary>
	/// Sets the target byte ordering we are swapping to or from.
	/// <para>
	/// Braindead Endian Reference:<br/>
	///		x86 is LITTLE Endian<br/>
	///		PowerPC is BIG Endian
	/// </para>
	/// </summary>
	public Byteswap<TOrder> SetTargetBigEndian(bool bigEndian) => new(TOrder.IsBigEndian() != bigEndian);

	/// <summary>
	/// Changes target endian
	/// </summary>
	public Byteswap<TOrder> FlipTargetEndian() => new(!flipped);

	/// <summary>
	/// Forces byte swapping state, regardless of endianess
	/// </summary>
	public Byteswap<TOrder> ActivateByteSwapping(bool activate) => SetTargetBigEndian(IsMachineBigEndian() != activate);

	/// <summary>
	/// Returns true if the target machine is the same as this one in endianness.
	/// <para>
	/// Used to determine when a byteswap needs to take place.
	/// </para>
	/// </summary>
	public bool IsSwappingBytes() => IsMachineBigEndian() != IsTargetBigEndian(); // Are bytes being swapped?

	/// <summary>
	/// What is the current target endian?
	/// </summary>
	public bool IsTargetBigEndian() => TOrder.IsBigEndian() != flipped;

	/// <summary>
	/// IsByteSwapped()
	/// <para>
	/// When supplied with a chunk of input data and a constant or magic number
	/// (in native format) determines the endienness of the current machine in
	/// relation to the given input data.
	/// </para>
	/// <para>
	/// ( This is useful for detecting byteswapping in magic numbers in structure
	/// headers for example. )
	/// </para>
	/// </summary>
	/// <returns>
	///		1  if input is the same as nativeConstant.<br/>
	///		0  if input is byteswapped relative to nativeConstant.<br/>
	///		-1 if input is not the same as nativeConstant and not byteswapped either.
	/// </returns>
	public int SourceIsNativeEndian<T>(T input, T nativeConstant) where T : unmanaged, IEquatable<T> {
		// If it's the same, it isn't byteswapped:
		if (input.Equals(nativeConstant))
			return 1;

		if (LowLevelByteSwap(input).Equals(nativeConstant))
			return 0;

		Debug.Fail("If we get here, input is neither a swapped nor unswapped version of nativeConstant.");
		return -1;
	}

	/// <summary>
	/// Swaps an input buffer full of type T into the given output buffer.
	/// <para>
	/// Swaps [count] items from the inputBuffer to the outputBuffer.
	/// If inputBuffer is omitted or nullptr, then it is assumed to be the same as
	/// outputBuffer - effectively swapping the contents of the buffer in place.
	/// </para>
	/// </summary>
	public void SwapBuffer<T>(Span<T> outputBuffer) where T : unmanaged {
		// Optimization for the case when we are swapping in place.
		ReverseEndianness<T>(outputBuffer, outputBuffer);
	}

	/// <inheritdoc cref="SwapBuffer{T}(Span{T})"/>
	public void SwapBuffer<T>(Span<T> outputBuffer, ReadOnlySpan<T> inputBuffer) where T : unmanaged {
		// Swap everything in the buffer:
		ReverseEndianness(inputBuffer, outputBuffer);
	}

	/// <summary>
	/// Swaps an input buffer full of type T into the given output buffer.
	/// <para>
	/// Swaps [count] items from the inputBuffer to the outputBuffer.
	/// If inputBuffer is omitted or nullptr, then it is assumed to be the same as
	/// outputBuffer - effectively swapping the contents of the buffer in place.
	/// </para>
	/// </summary>
	public void SwapBufferToTargetEndian<T>(Span<T> outputBuffer) where T : unmanaged {
		// Are we already the correct endienness? ( or are we swapping 1 byte items? )
		if (!IsSwappingBytes() || Unsafe.SizeOf<T>() == 1)
			return;

		// Swap everything in the buffer:
		ReverseEndianness<T>(outputBuffer, outputBuffer);
	}

	/// <inheritdoc cref="SwapBufferToTargetEndian{T}(Span{T})"/>
	public void SwapBufferToTargetEndian<T>(Span<T> outputBuffer, ReadOnlySpan<T> inputBuffer) where T : unmanaged {
		// Are we already the correct endienness? ( or are we swapping 1 byte items? )
		if (!IsSwappingBytes() || Unsafe.SizeOf<T>() == 1) {
			// Otherwise copy the inputBuffer to the outputBuffer:
			inputBuffer.CopyTo(outputBuffer);
			return;
		}

		// Swap everything in the buffer:
		ReverseEndianness(inputBuffer, outputBuffer);
	}

	/// <summary>
	/// The lowest level byte swapping workhorse of doom.  output always contains the
	/// swapped version of input.  ( Doesn't compare machine to target endianness )
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T LowLevelByteSwap<T>(T input) where T : unmanaged {
		T output = default;
		ref byte src = ref Unsafe.As<T, byte>(ref input);
		ref byte dst = ref Unsafe.As<T, byte>(ref output);

		if (Unsafe.SizeOf<T>() == 1) {
			return input;
		}
		else if (Unsafe.SizeOf<T>() == sizeof(ushort)) {
			Unsafe.WriteUnaligned(ref dst, BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(ref src)));
		}
		else if (Unsafe.SizeOf<T>() == sizeof(uint)) {
			Unsafe.WriteUnaligned(ref dst, BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref src)));
		}
		else if (Unsafe.SizeOf<T>() == sizeof(ulong)) {
			Unsafe.WriteUnaligned(ref dst, BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref src)));
		}
		else {
			ThrowUnsupportedSize<T>();
		}

		return output;
	}

	private static void ReverseEndianness<T>(ReadOnlySpan<T> input, Span<T> output) where T : unmanaged {
		if (Unsafe.SizeOf<T>() == 1) {
			input.CopyTo(output);
		}
		else if (Unsafe.SizeOf<T>() == sizeof(ushort)) {
			BinaryPrimitives.ReverseEndianness(MemoryMarshal.Cast<T, ushort>(input), MemoryMarshal.Cast<T, ushort>(output));
		}
		else if (Unsafe.SizeOf<T>() == sizeof(uint)) {
			BinaryPrimitives.ReverseEndianness(MemoryMarshal.Cast<T, uint>(input), MemoryMarshal.Cast<T, uint>(output));
		}
		else if (Unsafe.SizeOf<T>() == sizeof(ulong)) {
			BinaryPrimitives.ReverseEndianness(MemoryMarshal.Cast<T, ulong>(input), MemoryMarshal.Cast<T, ulong>(output));
		}
		else {
			ThrowUnsupportedSize<T>();
		}
	}

	[DoesNotReturn]
	private static void ThrowUnsupportedSize<T>()
		=> throw new NotSupportedException($"Unknown size {Unsafe.SizeOf<T>()} not in {{2,4,8}} set.");
}
