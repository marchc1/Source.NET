using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common.Utilities;

public class UtlMemory<T>
{
	const nint EXTERNAL_BUFFER_MARKER = -1;
	const nint EXTERNAL_CONST_BUFFER_MARKER = -2;
	public UtlMemory(nint growSize = 0, nint initSize = 0) {
		GrowSize = growSize;
		ValidateGrowSize();

		if (initSize != 0) {
			Memory = new T[initSize];
		}
	}
	public UtlMemory(T[] memory, bool isReadonly = false) {
		Memory = memory;
		GrowSize = isReadonly ? EXTERNAL_CONST_BUFFER_MARKER : EXTERNAL_BUFFER_MARKER;
	}

	public T[] Base() => Memory ?? []; 
	public nuint Count() => (nuint)(Memory?.LongLength ?? 0); 

	private void ValidateGrowSize() {}


	protected T[]? Memory;
	protected nint GrowSize;

	public void EnsureCapacity(nint num) {
		if (Memory?.LongLength >= num)
			return;

		if (IsExternallyAllocated()) {
			Assert(false);
			return;
		}

		if(Memory != null) {
			// Reallocate
			T[] old = Memory;
			Memory = new T[num];
			Array.Copy(old, Memory, old.LongLength);
		}
		else {
			Memory = new T[num];
		}
	}

	public ref T this[nuint index] => ref Memory![index];
	public bool IsExternallyAllocated() => GrowSize < 0;
	public bool IsReadOnly() => GrowSize == EXTERNAL_CONST_BUFFER_MARKER;
	public void SetGrowSize(nint size) {
		GrowSize = size;
		ValidateGrowSize();
	}
	public bool IsIdxValid(nuint index) => index < (nuint)(Memory?.Length ?? 0);
}
