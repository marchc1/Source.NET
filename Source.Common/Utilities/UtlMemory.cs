using CommunityToolkit.HighPerformance;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

using SpanIDX = int;

namespace Source.Common;

public class UtlMemory<T, I> where I : unmanaged, IEquatable<I>, IComparable, IComparable<I>, ISpanFormattable, IBinaryInteger<I>, IMinMaxValue<I>, IUtf8SpanFormattable
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)] static I FromSpanIDX(SpanIDX value) => I.CreateTruncating(value);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] static SpanIDX ToSpanIDX(I value) => SpanIDX.CreateTruncating(value);

	static readonly I NUM_2 = I.CreateTruncating(2);
	static readonly I NUM_31 = I.CreateTruncating(31);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static I CalcNewAllocationCount(I nAllocationCount, I nGrowSize, I nNewSize, I nBytesItem) {
		if (nGrowSize != I.Zero) {
			nAllocationCount = ((I.One + ((nNewSize - I.One) / nGrowSize)) * nGrowSize);
		}
		else {
			if (nAllocationCount == I.Zero)
				// Compute an allocation which is at least as big as a cache line...
				nAllocationCount = (NUM_31 + nBytesItem) / nBytesItem;

			while (nAllocationCount < nNewSize)
				nAllocationCount *= NUM_2;
		}

		return nAllocationCount;
	}

	T[]? Memory;
	I AllocationCount;
	I GrowSize;


	public UtlMemory(I growSize = default, I initSize = default) {
		AllocationCount = initSize;
		GrowSize = growSize;
		ValidateGrowSize();
		Assert(growSize >= I.Zero);
		if (AllocationCount != I.Zero)
			Memory = new T[ToSpanIDX(AllocationCount)];
	}

	public UtlMemory(T[] memory, bool isReadonly = false) {
		Memory = memory;
		AllocationCount = FromSpanIDX(memory.Length);
		GrowSize = isReadonly ? EXTERNAL_CONST_BUFFER_MARKER : EXTERNAL_BUFFER_MARKER;
	}

	public void Init(I growSize = default, I initSize = default) {
		Purge();

		GrowSize = growSize;
		AllocationCount = initSize;
		ValidateGrowSize();
		Assert(growSize >= I.Zero);
		if (AllocationCount != I.Zero)
			Memory = new T[ToSpanIDX(AllocationCount)];
	}

	public record struct Iterator
	{
		public Iterator(I i) => Index = i;
		public I Index;
	}

	public Iterator First() => new(IsIdxValid(I.Zero) ? I.Zero : InvalidIndex());
	public Iterator Next(in Iterator it) => new(IsIdxValid(it.Index + I.One) ? it.Index + I.One : InvalidIndex());
	public I GetIndex(in Iterator it) => it.Index;
	public bool IsIdxAfter(I i, in Iterator it) => i > it.Index;
	public bool IsValidIterator(in Iterator it) => IsIdxValid(it.Index);
	public Iterator InvalidIterator(I i, in Iterator it) => new(InvalidIndex());

	public ref T this[I i] {
		get {
			Assert(i < AllocationCount);
			return ref Memory.AsSpan()[ToSpanIDX(i)];
		}
	}
	public ref T Element(I i) {
		Assert(i < AllocationCount);
		return ref Memory.AsSpan()[ToSpanIDX(i)];
	}

	public bool IsIdxValid(I i) => i < AllocationCount;

	public static readonly I INVALID_INDEX = unchecked(-I.One);
	public static I InvalidIndex() => InvalidIndex();

	public T[] Base() => Memory!;

	public void SetExternalBuffer(T[] memory, bool isReadonly = false) {
		Purge();
		Memory = memory;
		AllocationCount = FromSpanIDX(memory.Length);
		GrowSize = isReadonly ? EXTERNAL_CONST_BUFFER_MARKER : EXTERNAL_BUFFER_MARKER;
	}
	public void AssumeMemory(T[] memory) {
		Purge();
		Memory = memory;
		AllocationCount = FromSpanIDX(memory.Length);
	}

	public void Swap(UtlMemory<T, I> memory) {
		(GrowSize, memory.GrowSize) = (memory.GrowSize, GrowSize);
		(Memory, memory.Memory) = (memory.Memory, Memory);
		(AllocationCount, memory.AllocationCount) = (memory.AllocationCount, AllocationCount);
	}

	public void ConvertToGrowableMemory(I growSize) {
		if (!IsExternallyAllocated())
			return;

		GrowSize = growSize;
		if (AllocationCount != I.Zero) {
			T[] memory = new T[ToSpanIDX(AllocationCount)];
			SpanIDX minSize = Math.Min(memory.Length, Memory!.Length);
			memcpy(memory.AsSpan()[..minSize], Memory.AsSpan()[..minSize]);
			Memory = memory;
		}
		else {
			Memory = default;
		}
	}

	public I NumAllocated() {
		return AllocationCount;
	}
	public I Count() {
		return AllocationCount;
	}
	public void Grow() => Grow(I.One);
	public void Grow(I num) {
		Assert(num > I.Zero);

		if (IsExternallyAllocated()) {
			// Can't grow a buffer whose memory was externally allocated 
			Assert(false);
			return;
		}

		// Make sure we have at least numallocated + num allocations.
		// Use the grow rules specified for this memory (in m_nGrowSize)
		I nAllocationRequested = AllocationCount + num;

		I nNewAllocationCount = CalcNewAllocationCount(AllocationCount, GrowSize, nAllocationRequested, FromSpanIDX(Unsafe.SizeOf<T>()));

		// if m_nAllocationRequested wraps index type I, recalculate
		if (nNewAllocationCount < nAllocationRequested) {
			if (nNewAllocationCount == I.Zero && (nNewAllocationCount - I.One) >= nAllocationRequested) {
				--nNewAllocationCount; // deal w/ the common case of m_nAllocationCount == MAX_USHORT + 1
			}
			else {
				while (nNewAllocationCount < nAllocationRequested)
					nNewAllocationCount = (nNewAllocationCount + nAllocationRequested) / NUM_2;
			}
		}

		AllocationCount = nNewAllocationCount;

		if (Memory != null) {
			T[] reallocated = new T[ToSpanIDX(AllocationCount)];
			SpanIDX minSize = Math.Min(reallocated.Length, Memory.Length);
			memcpy(reallocated.AsSpan()[..minSize], Memory.AsSpan()[..minSize]);
			Memory = reallocated;
		}
		else
			Memory = new T[ToSpanIDX(AllocationCount)];
	}
	public void EnsureCapacity(I num) {
		if (AllocationCount >= num)
			return;

		if (IsExternallyAllocated()) {
			// Can't grow a buffer whose memory was externally allocated 
			Assert(false);
			return;
		}

		AllocationCount = num;

		if (Memory != null) {
			T[] reallocated = new T[ToSpanIDX(AllocationCount)];
			SpanIDX minSize = Math.Min(reallocated.Length, Memory.Length);
			memcpy(reallocated.AsSpan()[..minSize], Memory.AsSpan()[..minSize]);
			Memory = reallocated;
		}
		else
			Memory = new T[ToSpanIDX(AllocationCount)];
	}
	public void Purge() {
		if (!IsExternallyAllocated()) {
			if (Memory != null)
				Memory = default;
			AllocationCount = I.Zero;
		}
	}
	public void Purge(I numElements) {
		Assert(numElements >= I.Zero);

		if (numElements > AllocationCount) {
			// Ensure this isn't a grow request in disguise.
			Assert(numElements <= AllocationCount); //-V547
			return;
		}

		// If we have zero elements, simply do a purge:
		if (numElements == I.Zero) {
			Purge();
			return;
		}

		if (IsExternallyAllocated())
			// Can't shrink a buffer whose memory was externally allocated, fail silently like purge 
			return;

		// If the number of elements is the same as the allocation count, we are done.
		if (numElements == AllocationCount)
			return;


		if (Memory == null) {
			// Allocation count is non zero, but memory is null.
			Assert(false);
			return;
		}

		AllocationCount = numElements;

		// Allocation count > 0, shrink it down.
		T[] reallocated = new T[ToSpanIDX(AllocationCount)];
		SpanIDX minSize = Math.Min(reallocated.Length, Memory.Length);
		memcpy(reallocated.AsSpan()[..minSize], Memory.AsSpan()[..minSize]);
		Memory = reallocated;
	}

	public bool IsExternallyAllocated() => GrowSize < I.Zero;
	public bool IsReadOnly() => GrowSize == EXTERNAL_CONST_BUFFER_MARKER;
	public void SetGrowSize(I size) {
		Assert(!IsExternallyAllocated());
		Assert(size >= I.Zero);
		GrowSize = size;
		ValidateGrowSize();
	}

	protected void ValidateGrowSize() { }

	protected static readonly I EXTERNAL_BUFFER_MARKER = unchecked(-(I.One));
	protected static readonly I EXTERNAL_CONST_BUFFER_MARKER = unchecked(-(I.One + I.One));
}

// Default memory type of nint
public class UtlMemory<T> : UtlMemory<T, nint>{
	public UtlMemory(nint growSize = default, nint initSize = default) : base(growSize, initSize) { }
	public UtlMemory(T[] memory) : base(memory) { }
}
