using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Source.Common;

public static unsafe class MemUtils
{
	public static void memset<T>(T* field, byte data, nuint size) where T : unmanaged {
		byte* write = (byte*)field;
		for (nuint i = 0; i < size; i++)
			write[i] = data;
	}

	public static void memset<T>(Span<T> field, T data) where T : unmanaged {
		fixed (T* ptr = field)
			for (nuint i = 0; i < (nuint)field.Length; i++)
				ptr[i] = data;
	}
	public static void memset<T>(Memory<T> field, T data) where T : unmanaged {
		fixed (T* ptr = field.Span)
			for (nuint i = 0; i < (nuint)field.Length; i++)
				ptr[i] = data;
	}
	/// <summary>
	/// This honestly might not be faster.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="field"></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void memreset<T>(Span<T> field) where T : struct {
		for (int i = 0; i < field.Length; i++) field[i] = default;
	}
	/// <summary>
	/// This honestly might not be faster.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="field"></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void memreset<T>(ref T field) where T : struct {
		Unsafe.InitBlock(ref Unsafe.As<T, byte>(ref field), 0, (uint)Unsafe.SizeOf<T>());
	}
	/// <summary>
	/// Performs C-style memory comparison on two unmanaged types.
	/// </summary>
	public static int memcmp<T>(in T buf1, in T buf2, nint size) where T : unmanaged {
		fixed (T* pBuf1 = &buf1) {
			fixed (T* pBuf2 = &buf2) {
				byte* b1 = (byte*)pBuf1;
				byte* b2 = (byte*)pBuf2;

				for (nint i = 0; i < size; i++) {
					int diff = b1[i] - b2[i];
					if (diff != 0)
						return diff;
				}

				return 0;
			}
		}
	}
	/// <summary>
	/// Performs C-style memory comparison on two unmanaged types, returning a boolean instead of an integer.
	/// </summary>
	public static bool memcmpb<T>(in T buf1, in T buf2, nint size) where T : unmanaged {
		fixed (T* pBuf1 = &buf1) {
			fixed (T* pBuf2 = &buf2) {
				byte* b1 = (byte*)pBuf1;
				byte* b2 = (byte*)pBuf2;

				for (nint i = 0; i < size; i++) {
					int diff = b1[i] - b2[i];
					if (diff != 0)
						return false;
				}

				return true;
			}
		}
	}
	public static void memcpy<T>(ref T dest, ref T src) where T : unmanaged {
		dest = src;
	}
	public static void memcpy<T>(Span<T> dest, ReadOnlySpan<T> src) where T : unmanaged {
		src.CopyTo(dest);
	}
}

public unsafe class UnmanagedHeapMemory : IDisposable
{
	byte* Pointer;
	nuint Len;

	public UnmanagedHeapMemory(nuint bytes) {
		Pointer = (byte*)NativeMemory.Alloc(bytes);
		Len = bytes;
	}

	public UnmanagedHeapMemory(int bytes) {
		Pointer = (byte*)NativeMemory.Alloc((nuint)bytes);
		Len = (nuint)bytes;
	}

	public void Memset(int value, nuint length) => Memset((byte)value, (int)length);
	public void Memset(int value, nint length) => Memset((byte)value, (int)length);
	public void Memset(int value, int length) => Memset((byte)value, length);
	public void Memset(byte value, nuint length) => Memset(value, (int)length);
	public void Memset(byte value, nint length) => Memset(value, (int)length);
	public void Memset(byte value, int pos, int length) => memset(new Span<byte>(Pointer, (int)Length).Slice(pos, length), value);
	public void Memset(byte value, int length) => memset(new Span<byte>(Pointer, (int)Length)[..length], value);
	public void Memset(byte value) => memset(new Span<byte>(Pointer, (int)Length), value);

	public static implicit operator Span<byte>(UnmanagedHeapMemory memory) => memory.ToSpan();

	public Span<byte> ToSpan() => new(Pointer, (int)Length);
	public byte* ToPointer() => Pointer;

	public void Dispose() {
		if (Pointer == null) {
			Warning("WARNING: ATTEPTED TO DISPOSE OF NO LONGER VALID UNMANAGED HEAP MEMORY\n");
			return;
		}
		NativeMemory.Free(Pointer);
		Pointer = null;
		Len = 0;
	}

	public bool IsValid() => Pointer != null;

	public nuint Length => Len;
	public nuint Handle => (nuint)Pointer;
}
public unsafe class UnmanagedHeapMemory<T> : IDisposable where T : unmanaged
{
	T* Pointer;
	nuint Len;

	public UnmanagedHeapMemory(nuint elements) {
		Pointer = (T*)NativeMemory.Alloc(elements * (nuint)sizeof(T));
		Len = elements;
	}
	public UnmanagedHeapMemory(int elements) : this((nuint)elements) { }

	public void Memset(in T value, int pos, int length) => memset(new Span<T>(Pointer, (int)Length).Slice(pos, length), value);
	public void Memset(in T value, int length) => memset(new Span<T>(Pointer, (int)Length)[..length], value);
	public void Memset(in T value) => memset(new Span<T>(Pointer, (int)Length), value);

	public static implicit operator Span<T>(UnmanagedHeapMemory<T> memory) => memory.ToSpan();

	public Span<T> ToSpan() => new(Pointer, (int)Length);
	public T* ToPointer() => Pointer;

	public void Dispose() {
		if (Pointer == null) {
			Warning("WARNING: ATTEPTED TO DISPOSE OF NO LONGER VALID UNMANAGED HEAP MEMORY\n");
			return;
		}
		NativeMemory.Free(Pointer);
		Pointer = null;
		Len = 0;
	}

	public bool IsValid() => Pointer != null;

	public nuint Length => Len;
	public nuint Handle => (nuint)Pointer;
}
