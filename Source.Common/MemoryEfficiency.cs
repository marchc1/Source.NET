using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common;

public enum StructInitializationMode {
	None,
	Default,
	New
}
public class ReusableBox<T> where T : struct {
	// Instance fields
	public T Struct;

	// Instance methods
	public ref T Ref() => ref Struct;

	// Static members
	static readonly ClassMemoryPool<ReusableBox<T>> pool = new();
	// Static methods
	public static ReusableBox<T> Rent(StructInitializationMode initializeMode = StructInitializationMode.New) {
		ReusableBox<T> box = pool.Alloc();

		if (initializeMode == StructInitializationMode.New)
			box.Struct = new();
		else if (initializeMode == StructInitializationMode.Default)
			box.Struct = default;

		return box;
	}

	public static void Return(ReusableBox<T> box) {
		pool.Free(box);
	}
}
