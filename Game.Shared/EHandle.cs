using Source;
using Source.Common;

using System.Runtime.CompilerServices;

namespace Game.Shared;

public static class HandleExts {
	static IClientEntityList? entityList;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IHandleEntity? Get(this BaseHandle handle) {
		return (entityList ??= Singleton<IClientEntityList>()).LookupEntity(handle);
	}
	public static T? Get<T>(this Handle<T> handle) where T : IHandleEntity {
		return (T?)(entityList ??= Singleton<IClientEntityList>()).LookupEntity(handle);
	}

	/// <summary>
	/// Because C# doesn't have set operators, you use this to set an EHANDLE's value to another value.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="handle"></param>
	/// <param name="entity"></param>
	public static void Set<T>(this Handle<T> handle, Handle<T> entity) where T : IHandleEntity {
		handle.Index = entity.Index;
	}
}

public struct Handle<T> : IEquatable<Handle<T>>, IEquatable<BaseHandle>, IBaseHandle {
	public uint Unpack() => Index;

	public bool Equals(Handle<T> otherHandle) => Index == otherHandle.Index;
	public bool Equals(BaseHandle otherHandle) => Index == otherHandle.Index;
	public uint Index;

	public void Invalidate() => Index = (uint)Constants.INVALID_EHANDLE_INDEX;
	public Handle() => Invalidate();
	public Handle(uint value) => Index = value;
	public Handle(in BaseHandle handle) => Index = handle.Index;
	public Handle(int entry, int serial) => Init(entry, serial);

	public void Init(in BaseHandle otherHandle) => Index = otherHandle.Index;
	public void Init(uint entindex) => Index = entindex;
	public void Init(ulong entindex) => Index = (uint)entindex;
	public void Init(int entry, int serial) => Index = (uint)(entry | (serial << Constants.NUM_ENT_ENTRY_BITS));

	public int GetEntryIndex() => (int)(Index & Constants.ENT_ENTRY_MASK);
	public int GetSerialNumber() => (int)(Index >> Constants.NUM_ENT_ENTRY_BITS);

	public static bool operator ==(BaseHandle a, Handle<T> b) => a.Index == b.Index;
	public static bool operator !=(BaseHandle a, Handle<T> b) => a.Index != b.Index;
	public static bool operator <(BaseHandle a, Handle<T> b) => a.Index < b.Index;
	public static bool operator >(BaseHandle a, Handle<T> b) => a.Index > b.Index;

	public static bool operator ==(Handle<T> a, BaseHandle b) => a.Index == b.Index;
	public static bool operator !=(Handle<T> a, BaseHandle b) => a.Index != b.Index;
	public static bool operator <(Handle<T> a, BaseHandle b) => a.Index < b.Index;
	public static bool operator >(Handle<T> a, BaseHandle b) => a.Index > b.Index;

	public static bool operator ==(Handle<T> a, Handle<T> b) => a.Index == b.Index;
	public static bool operator !=(Handle<T> a, Handle<T> b) => a.Index != b.Index;
	public static bool operator <(Handle<T> a, Handle<T> b) => a.Index < b.Index;
	public static bool operator >(Handle<T> a, Handle<T> b) => a.Index > b.Index;

	public override bool Equals(object? obj) {
		return obj switch {
			BaseHandle b => Index == b.Index,
			Handle<T> b => Index == b.Index,
			IBaseHandle b => Index == b.Unpack(),
			_ => false
		};
	}

	public static implicit operator Handle<T>(BaseHandle handle) => new(handle.Index);
	public static implicit operator BaseHandle(Handle<T> handle) => new(handle.Index);

	public override int GetHashCode() => (int)Index;
	public bool IsValid() => Index != Constants.INVALID_EHANDLE_INDEX;

	public Handle<T> Set(IHandleEntity? entity) {
		if (entity != null)
			this.Index = entity.GetRefEHandle().Index;
		else
			Index = Constants.INVALID_EHANDLE_INDEX;
		return this;
	}
}
