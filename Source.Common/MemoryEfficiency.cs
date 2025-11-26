using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Source.Common;

/// <summary>
/// An atteept at resolving issue #37. This exposes a state from IDisposable.
/// </summary>
public interface IExtDisposable : IDisposable {
	/// <summary>
	/// Should return true if the object has been disposed or is in the process of disposing.
	/// </summary>
	bool Disposed();
	public bool TryDispose() {
		if (Disposed())
			return false;
		Dispose();
		return true;
	}
}

public static class IExtDisposableGlobalFns {
	/// <summary>
	/// This method allows checking if an <see cref="IExtDisposable"/> is valid or not. If the object is null, this returns false.
	/// If the object isn't null, this returns !Disposed().
	/// </summary>
	/// <param name="extDisposable"></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsValid([NotNullWhen(true)] this IExtDisposable? extDisposable) => extDisposable == null ? false : !extDisposable.Disposed();
	public static bool Delete([MaybeNull] this IExtDisposable? extDisposable) => extDisposable == null ? false : extDisposable.TryDispose();
}

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
